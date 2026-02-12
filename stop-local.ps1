# Umi Health POS - Local Development Stop Script
# This script stops all running services

param(
    [switch]$RemoveVolumes,
    [switch]$Help
)

# Display help information
if ($Help) {
    Write-Host "=== Umi Health POS - Local Development Stop Script ===" -ForegroundColor Green
    Write-Host ""
    Write-Host "Usage: .\stop-local.ps1 [options]" -ForegroundColor Yellow
    Write-Host ""
    Write-Host "Options:" -ForegroundColor Cyan
    Write-Host "  -RemoveVolumes    Remove Docker volumes (WARNING: This will delete database data)"
    Write-Host "  -Help            Show this help message"
    Write-Host ""
    Write-Host "Examples:" -ForegroundColor Yellow
    Write-Host "  .\stop-local.ps1              # Stop all services"
    Write-Host "  .\stop-local.ps1 -RemoveVolumes # Stop services and delete data"
    Write-Host ""
    exit 0
}

Write-Host "=== Stopping Umi Health POS Services ===" -ForegroundColor Green
Write-Host ""

# Check if Docker is running
try {
    $dockerVersion = docker --version 2>$null
    if ($LASTEXITCODE -eq 0) {
        Write-Host "✓ Docker is running" -ForegroundColor Green
    } else {
        throw "Docker not found"
    }
} catch {
    Write-Host "✗ Docker is not running" -ForegroundColor Red
    Write-Host "Cannot stop services if Docker is not running" -ForegroundColor Yellow
    exit 1
}

# Show current status
Write-Host "Current service status:" -ForegroundColor Yellow
docker-compose ps
Write-Host ""

# Stop services
if ($RemoveVolumes) {
    Write-Host "Stopping services and removing volumes..." -ForegroundColor Red
    Write-Host "WARNING: This will delete all database data!" -ForegroundColor Red
    Write-Host ""
    
    $confirm = Read-Host "Are you sure you want to delete all data? (y/N)"
    if ($confirm -eq 'y' -or $confirm -eq 'Y') {
        docker-compose down -v
        Write-Host "✓ Services stopped and volumes removed" -ForegroundColor Green
    } else {
        Write-Host "Operation cancelled" -ForegroundColor Yellow
        exit 0
    }
} else {
    Write-Host "Stopping services..." -ForegroundColor Yellow
    docker-compose down
    Write-Host "✓ Services stopped" -ForegroundColor Green
}

Write-Host ""

# Verify services are stopped
Write-Host "Verifying services are stopped..." -ForegroundColor Yellow
$runningContainers = docker-compose ps -q
if ($runningContainers) {
    Write-Host "⚠ Some containers may still be running" -ForegroundColor Yellow
    docker-compose ps
} else {
    Write-Host "✓ All services stopped successfully" -ForegroundColor Green
}

Write-Host ""
Write-Host "=== Services Stopped ===" -ForegroundColor Green
Write-Host "To restart services, run: .\start-local.ps1" -ForegroundColor Cyan
