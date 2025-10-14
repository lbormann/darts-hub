# DartsHub CLI Test Script
# This script tests the CLI functionality of DartsHub

Write-Host "Testing DartsHub CLI functionality..." -ForegroundColor Green
Write-Host ""

$exePath = Join-Path $PSScriptRoot "darts-hub.exe"

if (Test-Path $exePath) {
    Write-Host "Found DartsHub executable: $exePath" -ForegroundColor Green
    
    # Test version command
    Write-Host ""
    Write-Host "Testing --version command:" -ForegroundColor Yellow
    & $exePath --version
    
    # Test help command
    Write-Host ""
    Write-Host "Testing --help command:" -ForegroundColor Yellow
    & $exePath --help
    
    Write-Host ""
    Write-Host "CLI test completed!" -ForegroundColor Green
} else {
    Write-Host "Error: darts-hub.exe not found in current directory" -ForegroundColor Red
    Write-Host "Please run this script from the DartsHub binary directory" -ForegroundColor Red
}