# SharpCoreDB Benchmark Runner (PowerShell)
# Runs all benchmark scenarios and generates reports

param(
    [string]$Configuration = "Release",
    [string]$OutputDir = "results",
    [switch]$SkipBuild = $false
)

Write-Host "========================================" -ForegroundColor Cyan
Write-Host "  SharpCoreDB Benchmark Suite" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan
Write-Host ""

# Get script directory
$ScriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path
$ProjectDir = Join-Path $ScriptDir "SharpCoreDB.Benchmarks"
$OutputPath = Join-Path $ScriptDir $OutputDir
$Timestamp = Get-Date -Format "yyyy-MM-dd-HHmmss"
$RunOutputDir = Join-Path $OutputPath $Timestamp

Write-Host "[Runner] Configuration: $Configuration" -ForegroundColor Green
Write-Host "[Runner] Output directory: $RunOutputDir" -ForegroundColor Green
Write-Host ""

# Build project
if (-not $SkipBuild) {
    Write-Host "[Runner] Building project..." -ForegroundColor Yellow
    Push-Location $ProjectDir
    dotnet build --configuration $Configuration
    if ($LASTEXITCODE -ne 0) {
        Write-Host "[Runner] Build failed!" -ForegroundColor Red
        Pop-Location
        exit 1
    }
    Pop-Location
    Write-Host "[Runner] Build successful!" -ForegroundColor Green
    Write-Host ""
} else {
    Write-Host "[Runner] Skipping build (--SkipBuild specified)" -ForegroundColor Yellow
    Write-Host ""
}

# Create output directory
New-Item -ItemType Directory -Force -Path $RunOutputDir | Out-Null
Write-Host "[Runner] Created output directory: $RunOutputDir" -ForegroundColor Green
Write-Host ""

# Run benchmarks
Write-Host "[Runner] Starting benchmark execution..." -ForegroundColor Cyan
Write-Host ""

Push-Location $ProjectDir
$StartTime = Get-Date

# Execute benchmark
dotnet run --configuration $Configuration --no-build

$EndTime = Get-Date
$Duration = $EndTime - $StartTime

Pop-Location

Write-Host ""
Write-Host "========================================" -ForegroundColor Cyan
Write-Host "  Benchmark Run Complete" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan
Write-Host "Duration: $($Duration.TotalMinutes.ToString('F2')) minutes" -ForegroundColor Green
Write-Host "Results saved to: $RunOutputDir" -ForegroundColor Green
Write-Host ""

# List generated files
Write-Host "Generated files:" -ForegroundColor Yellow
Get-ChildItem $RunOutputDir -Recurse | ForEach-Object {
    Write-Host "  - $($_.Name)" -ForegroundColor Gray
}

Write-Host ""
Write-Host "[Runner] Done!" -ForegroundColor Cyan
