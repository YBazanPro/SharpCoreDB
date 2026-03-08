# SharpCoreDB Server - Windows Service Install Script
# Usage: Run as Administrator in PowerShell
#   .\install-service.ps1
# Requires: .NET 10 runtime

#Requires -RunAsAdministrator

$ServiceName = "SharpCoreDB"
$DisplayName = "SharpCoreDB Database Server"
$Description = "SharpCoreDB network database server with gRPC and HTTPS support"
$InstallDir = "C:\Program Files\SharpCoreDB Server"
$DataDir = "$InstallDir\data"
$LogDir = "$InstallDir\logs"
$CertDir = "$InstallDir\certs"
$SecretDir = "$InstallDir\secrets"
$ExePath = "$InstallDir\sharpcoredb-server.exe"

Write-Host ""
Write-Host "======================================" -ForegroundColor Cyan
Write-Host "  SharpCoreDB Server v1.5.0 Installer" -ForegroundColor Cyan
Write-Host "  Windows Service Setup" -ForegroundColor Cyan
Write-Host "======================================" -ForegroundColor Cyan
Write-Host ""

# Check .NET
$dotnetVersion = dotnet --version 2>$null
if (-not $dotnetVersion) {
    Write-Host "ERROR: .NET runtime not found." -ForegroundColor Red
    Write-Host "Install .NET 10: https://dotnet.microsoft.com/download/dotnet/10.0"
    exit 1
}
Write-Host "[OK] .NET version: $dotnetVersion" -ForegroundColor Green

# Create directories
Write-Host "[..] Creating directories" -ForegroundColor Yellow
$dirs = @($InstallDir, "$DataDir\system", "$DataDir\user", $LogDir, $CertDir, $SecretDir)
foreach ($dir in $dirs) {
    if (-not (Test-Path $dir)) {
        New-Item -ItemType Directory -Path $dir -Force | Out-Null
    }
}
Write-Host "[OK] Directories created" -ForegroundColor Green

# Copy application files
$ScriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path
$PublishDir = Join-Path $ScriptDir "publish"

if (Test-Path $PublishDir) {
    Write-Host "[..] Copying application files" -ForegroundColor Yellow
    Copy-Item -Path "$PublishDir\*" -Destination $InstallDir -Recurse -Force
    Write-Host "[OK] Files copied to $InstallDir" -ForegroundColor Green
} else {
    Write-Host "[!!] No publish directory found. Run first:" -ForegroundColor Red
    Write-Host "  dotnet publish src\SharpCoreDB.Server -c Release -o installers\windows\publish"
    Write-Host "Then re-run this script."
    exit 1
}

# Stop existing service if running
$existingService = Get-Service -Name $ServiceName -ErrorAction SilentlyContinue
if ($existingService) {
    Write-Host "[..] Stopping existing service" -ForegroundColor Yellow
    Stop-Service -Name $ServiceName -Force -ErrorAction SilentlyContinue
    sc.exe delete $ServiceName | Out-Null
    Start-Sleep -Seconds 2
    Write-Host "[OK] Existing service removed" -ForegroundColor Green
}

# Register Windows Service
Write-Host "[..] Registering Windows Service" -ForegroundColor Yellow

sc.exe create $ServiceName `
    binPath= "`"$ExePath`"" `
    start= delayed-auto `
    DisplayName= "`"$DisplayName`"" | Out-Null

sc.exe description $ServiceName "`"$Description`"" | Out-Null
sc.exe failure $ServiceName reset= 86400 actions= restart/5000/restart/10000/restart/30000 | Out-Null

Write-Host "[OK] Service registered: $ServiceName" -ForegroundColor Green

# Set environment variables for the service
$regPath = "HKLM:\SYSTEM\CurrentControlSet\Services\$ServiceName"
$env = @(
    "ASPNETCORE_URLS=https://+:5001;https://+:8443",
    "ASPNETCORE_ENVIRONMENT=Production",
    "DOTNET_gcServer=1"
)
Set-ItemProperty -Path $regPath -Name "Environment" -Value $env -Type MultiString

# Firewall rules
Write-Host "[..] Configuring firewall" -ForegroundColor Yellow
$rules = @(
    @{ Name = "SharpCoreDB gRPC"; Port = 5001 },
    @{ Name = "SharpCoreDB HTTPS API"; Port = 8443 }
)
foreach ($rule in $rules) {
    $existing = Get-NetFirewallRule -DisplayName $rule.Name -ErrorAction SilentlyContinue
    if (-not $existing) {
        New-NetFirewallRule -DisplayName $rule.Name `
            -Direction Inbound -Protocol TCP `
            -LocalPort $rule.Port -Action Allow | Out-Null
    }
}
Write-Host "[OK] Firewall rules configured" -ForegroundColor Green

Write-Host ""
Write-Host "======================================" -ForegroundColor Green
Write-Host "  Installation complete!" -ForegroundColor Green
Write-Host "======================================" -ForegroundColor Green
Write-Host ""
Write-Host "  Next steps:" -ForegroundColor White
Write-Host "  1. Place TLS cert:  $CertDir\server.pfx"
Write-Host "  2. Edit config:     $InstallDir\appsettings.json"
Write-Host "  3. Set JWT secret in config or environment"
Write-Host "  4. Start service:   Start-Service $ServiceName"
Write-Host "  5. Check status:    Get-Service $ServiceName"
Write-Host "  6. View logs:       Get-Content $LogDir\sharpcoredb-server*.log -Tail 50"
Write-Host ""
Write-Host "  Endpoints:" -ForegroundColor White
Write-Host "    gRPC:      https://localhost:5001"
Write-Host "    REST API:  https://localhost:8443"
Write-Host "    Health:    https://localhost:8443/health"
Write-Host ""
