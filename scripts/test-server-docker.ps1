param(
    [string]$RepoRoot = (Resolve-Path (Join-Path $PSScriptRoot "..")).Path,
    [switch]$KeepRunning,
    [switch]$SkipBuild
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

function Write-Step {
    param([string]$Message)
    Write-Host "`n==> $Message" -ForegroundColor Cyan
}

function New-RandomSecret {
    param([int]$Length = 64)
    $chars = ('abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789!@#$%^&*()-_=+')
    -join (1..$Length | ForEach-Object { $chars[(Get-Random -Minimum 0 -Maximum $chars.Length)] })
}

$serverPath = Join-Path $RepoRoot 'src\SharpCoreDB.Server'
$certsPath = Join-Path $serverPath 'certs'
$secretsPath = Join-Path $serverPath 'secrets'
$certPath = Join-Path $certsPath 'server.pfx'
$composePath = Join-Path $serverPath 'docker-compose.yml'
$overridePath = Join-Path $serverPath 'docker-compose.override.local.yml'
$pfxPassword = 'TestServerPfxPass123!'
$jwtSecret = 'LocalDockerJwtSecretKeyForSharpCoreDB_AtLeast32Chars!'

$results = [System.Collections.Generic.List[object]]::new()

function Add-Result {
    param(
        [string]$Check,
        [bool]$Passed,
        [string]$Details
    )

    $results.Add([pscustomobject]@{
        Check   = $Check
        Passed  = $Passed
        Details = $Details
    }) | Out-Null
}

function Invoke-Compose {
    param([string]$Arguments)
    Push-Location $serverPath
    try {
        & docker compose -f $composePath -f $overridePath @($Arguments -split ' ')
    }
    finally {
        Pop-Location
    }
}

try {
    Write-Step 'Preparing folders and local secrets'
    New-Item -ItemType Directory -Force $certsPath | Out-Null
    New-Item -ItemType Directory -Force $secretsPath | Out-Null

    foreach ($name in @('master', 'model', 'msdb', 'appdb')) {
        Set-Content -Path (Join-Path $secretsPath "$name.key") -Value (New-RandomSecret) -NoNewline
    }

    Write-Step 'Creating development TLS certificate'
    & dotnet dev-certs https -ep $certPath -p $pfxPassword | Out-Null

    Write-Step 'Writing local compose override'
    $overrideContent = @"
services:
  sharpcoredb:
    environment:
      - Server__Security__TlsPrivateKeyPath=$pfxPassword
      - Server__Security__JwtSecretKey=$jwtSecret
      - Server__SystemDatabases__Enabled=false
"@
    Set-Content -Path $overridePath -Value $overrideContent

    Write-Step 'Starting Docker stack'
    if ($SkipBuild) {
        Invoke-Compose 'up -d'
    }
    else {
        Invoke-Compose 'up -d --build'
    }

    Write-Step 'Waiting for healthy container (max 120s)'
    $deadline = (Get-Date).AddSeconds(120)
    $health = 'starting'
    while ((Get-Date) -lt $deadline) {
        $state = & docker inspect sharpcoredb-server --format "{{.State.Status}}|{{if .State.Health}}{{.State.Health.Status}}{{else}}none{{end}}"
        if ($state) {
            $parts = $state.Trim() -split '\|'
            if ($parts.Count -ge 2) {
                $health = $parts[1]
                if ($parts[0] -eq 'running' -and $health -eq 'healthy') {
                    break
                }
            }
        }
        Start-Sleep -Seconds 3
    }

    $isHealthy = $health -eq 'healthy'
    Add-Result -Check 'Container healthy within 2 minutes' -Passed $isHealthy -Details "health=$health"

    Write-Step 'Running HTTPS and protocol checks'

    try {
        $healthStatus = (Invoke-WebRequest -Uri 'https://localhost:8443/health' -SkipCertificateCheck -UseBasicParsing -TimeoutSec 10).StatusCode
        Add-Result -Check 'HTTPS /health responds' -Passed ($healthStatus -eq 200) -Details "status=$healthStatus"
    }
    catch {
        Add-Result -Check 'HTTPS /health responds' -Passed $false -Details $_.Exception.Message
    }

    try {
        $rootContent = (Invoke-WebRequest -Uri 'https://localhost:8443/' -SkipCertificateCheck -UseBasicParsing -TimeoutSec 10).Content
        Add-Result -Check 'HTTPS root endpoint responds' -Passed ($rootContent -like '*SharpCoreDB Server*') -Details $rootContent
    }
    catch {
        Add-Result -Check 'HTTPS root endpoint responds' -Passed $false -Details $_.Exception.Message
    }

    try {
        $tcp = [System.Net.Sockets.TcpClient]::new()
        $tcp.Connect('localhost', 5001)
        $connected = $tcp.Connected
        $tcp.Dispose()
        Add-Result -Check 'gRPC port 5001 reachable' -Passed $connected -Details ($connected ? 'TCP connect ok' : 'TCP connect failed')
    }
    catch {
        Add-Result -Check 'gRPC port 5001 reachable' -Passed $false -Details $_.Exception.Message
    }

    try {
        Invoke-WebRequest -Uri 'http://localhost:8443/health' -UseBasicParsing -TimeoutSec 8 | Out-Null
        Add-Result -Check 'No plain HTTP endpoint exposure' -Passed $false -Details 'HTTP unexpectedly responded on 8443'
    }
    catch {
        Add-Result -Check 'No plain HTTP endpoint exposure' -Passed $true -Details 'HTTP request failed as expected'
    }

    Write-Step 'Checking startup logs for configured databases'
    $logs = & docker logs sharpcoredb-server --tail 300 2>&1 | Out-String
    $dbNames = @('master', 'model', 'msdb', 'tempdb', 'appdb')
    $missing = @($dbNames | Where-Object { $logs -notmatch [Regex]::Escape($_) })
    Add-Result -Check 'Configured databases initialized' -Passed ($missing.Count -eq 0) -Details (($missing.Count -eq 0) ? 'all found' : ("missing: $($missing -join ', ')"))

    Write-Step 'Test summary'
    $results | Format-Table -AutoSize

    if ($results.Exists({ param($r) -not $r.Passed })) {
        Write-Host "`nServer-mode Docker test: FAILED" -ForegroundColor Red
        exit 1
    }

    Write-Host "`nServer-mode Docker test: PASSED" -ForegroundColor Green
}
finally {
    if (-not $KeepRunning) {
        Write-Step 'Teardown: docker compose down -v'
        try {
            Invoke-Compose 'down -v'
        }
        catch {
            Write-Warning "Teardown failed: $($_.Exception.Message)"
        }
    }
    else {
        Write-Host "Keeping container running because -KeepRunning was specified." -ForegroundColor Yellow
    }
}
