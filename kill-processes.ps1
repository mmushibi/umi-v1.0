# Umi Health POS - Kill All Running Processes
# This script stops all services and kills related processes
# NOTE: Run this script as Administrator for full functionality

param(
    [switch]$Force,
    [switch]$Help
)

# Display help information
if ($Help) {
    Write-Host "=== Umi Health POS - Kill Processes Script ===" -ForegroundColor Green
    Write-Host ""
    Write-Host "Usage: .\kill-processes.ps1 [options]" -ForegroundColor Yellow
    Write-Host ""
    Write-Host "Options:" -ForegroundColor Cyan
    Write-Host "  -Force    Force kill all processes without confirmation"
    Write-Host "  -Help     Show this help message"
    Write-Host ""
    Write-Host "NOTE: Run this script as Administrator for full functionality" -ForegroundColor Red
    Write-Host ""
    exit 0
}

Write-Host "=== Umi Health POS - Killing All Running Processes ===" -ForegroundColor Green
Write-Host ""

# Check if running as Administrator
$isAdmin = ([Security.Principal.WindowsPrincipal][Security.Principal.WindowsIdentity]::GetCurrent()).IsInRole([Security.Principal.WindowsBuiltInRole]::Administrator)

if (-not $isAdmin) {
    Write-Host "⚠ WARNING: Not running as Administrator" -ForegroundColor Yellow
    Write-Host "Some processes may not be able to be stopped" -ForegroundColor Yellow
    Write-Host "For full functionality, run as Administrator" -ForegroundColor Yellow
    Write-Host ""
}

# Function to kill process by name
function Kill-ProcessByName {
    param(
        [string]$ProcessName,
        [string]$DisplayName = $ProcessName
    )
    
    Write-Host "Checking for $DisplayName processes..." -ForegroundColor Yellow
    
    $processes = Get-Process -Name $ProcessName -ErrorAction SilentlyContinue
    if ($processes) {
        foreach ($proc in $processes) {
            Write-Host "  Found $($proc.ProcessName) (PID: $($proc.Id))" -ForegroundColor Gray
            
            if ($Force -or $isAdmin) {
                try {
                    $proc.Kill()
                    Write-Host "  ✓ Killed $($proc.ProcessName) (PID: $($proc.Id))" -ForegroundColor Green
                } catch {
                    Write-Host "  ✗ Failed to kill $($proc.ProcessName) (PID: $($proc.Id)): $($_.Exception.Message)" -ForegroundColor Red
                }
            } else {
                Write-Host "  ⚠ Skipping $($proc.ProcessName) (use -Force or run as Administrator)" -ForegroundColor Yellow
            }
        }
    } else {
        Write-Host "  No $DisplayName processes found" -ForegroundColor Gray
    }
    Write-Host ""
}

# Function to stop Windows service
function Stop-ServiceSafely {
    param(
        [string]$ServiceName,
        [string]$DisplayName = $ServiceName
    )
    
    Write-Host "Checking for $DisplayName service..." -ForegroundColor Yellow
    
    $service = Get-Service -Name $ServiceName -ErrorAction SilentlyContinue
    if ($service) {
        Write-Host "  Found $DisplayName service (Status: $($service.Status))" -ForegroundColor Gray
        
        if ($service.Status -eq "Running") {
            if ($Force -or $isAdmin) {
                try {
                    Stop-Service -Name $ServiceName -Force -ErrorAction Stop
                    Write-Host "  ✓ Stopped $DisplayName service" -ForegroundColor Green
                } catch {
                    Write-Host "  ✗ Failed to stop $DisplayName service: $($_.Exception.Message)" -ForegroundColor Red
                }
            } else {
                Write-Host "  ⚠ Skipping $DisplayName service (use -Force or run as Administrator)" -ForegroundColor Yellow
            }
        } else {
            Write-Host "  $DisplayName service is not running" -ForegroundColor Gray
        }
    } else {
        Write-Host "  No $DisplayName service found" -ForegroundColor Gray
    }
    Write-Host ""
}

# Function to kill process by port
function Kill-ProcessByPort {
    param(
        [int]$Port,
        [string]$DisplayName = "Port $Port"
    )
    
    Write-Host "Checking for processes using $DisplayName..." -ForegroundColor Yellow
    
    try {
        $connections = netstat -ano | findstr ":$Port"
        if ($connections) {
            Write-Host "  Found processes using $DisplayName:" -ForegroundColor Gray
            
            foreach ($line in $connections) {
                if ($line -match 'LISTENING\s+(\d+)$') {
                    $pid = $matches[1]
                    $process = Get-Process -Id $pid -ErrorAction SilentlyContinue
                    
                    if ($process) {
                        Write-Host "    Found $($process.ProcessName) (PID: $pid) on $DisplayName" -ForegroundColor Gray
                        
                        if ($Force -or $isAdmin) {
                            try {
                                $process.Kill()
                                Write-Host "    ✓ Killed $($process.ProcessName) (PID: $pid) on $DisplayName" -ForegroundColor Green
                            } catch {
                                Write-Host "    ✗ Failed to kill $($process.ProcessName) (PID: $pid): $($_.Exception.Message)" -ForegroundColor Red
                            }
                        } else {
                            Write-Host "    ⚠ Skipping $($process.ProcessName) on $DisplayName (use -Force or run as Administrator)" -ForegroundColor Yellow
                        }
                    }
                }
            }
        } else {
            Write-Host "  No processes found using $DisplayName" -ForegroundColor Gray
        }
    } catch {
        Write-Host "  Error checking $DisplayName: $($_.Exception.Message)" -ForegroundColor Red
    }
    Write-Host ""
}

# Stop Docker containers first
Write-Host "=== Stopping Docker Containers ===" -ForegroundColor Cyan
Write-Host ""

try {
    $dockerResult = docker-compose down 2>$null
    if ($LASTEXITCODE -eq 0) {
        Write-Host "✓ Docker containers stopped successfully" -ForegroundColor Green
    } else {
        Write-Host "⚠ Docker containers may not be running or accessible" -ForegroundColor Yellow
    }
} catch {
    Write-Host "⚠ Docker not available or not running" -ForegroundColor Yellow
}
Write-Host ""

# Kill application processes
Write-Host "=== Killing Application Processes ===" -ForegroundColor Cyan
Write-Host ""

Kill-ProcessByName -ProcessName "dotnet" -DisplayName ".NET"
Kill-ProcessByName -ProcessName "node" -DisplayName "Node.js"
Kill-ProcessByName -ProcessName "nginx" -DisplayName "Nginx"
Kill-ProcessByName -ProcessName "postgres" -DisplayName "PostgreSQL"

# Stop Windows services
Write-Host "=== Stopping Windows Services ===" -ForegroundColor Cyan
Write-Host ""

Stop-ServiceSafely -ServiceName "postgresql-x64-18" -DisplayName "PostgreSQL Server 18"
Stop-ServiceSafely -ServiceName "postgresql-x64-15" -DisplayName "PostgreSQL Server 15"
Stop-ServiceSafely -ServiceName "postgresql-x64-14" -DisplayName "PostgreSQL Server 14"

# Kill processes by port
Write-Host "=== Killing Processes by Port ===" -ForegroundColor Cyan
Write-Host ""

Kill-ProcessByPort -Port 5432 -DisplayName "PostgreSQL Port"
Kill-ProcessByPort -Port 5000 -DisplayName "Backend API Port"
Kill-ProcessByPort -Port 80 -DisplayName "Frontend Port"

# Final status check
Write-Host "=== Final Status Check ===" -ForegroundColor Green
Write-Host ""

$ports = @(5432, 5000, 80)
$allClear = $true

foreach ($port in $ports) {
    $connections = netstat -ano | findstr ":$port" | findstr "LISTENING"
    if ($connections) {
        Write-Host "⚠ Port $port is still in use:" -ForegroundColor Yellow
        $connections | ForEach-Object { Write-Host "  $_" -ForegroundColor Gray }
        $allClear = $false
    } else {
        Write-Host "✓ Port $port is clear" -ForegroundColor Green
    }
}

Write-Host ""

# Check Docker containers
try {
    $containers = docker-compose ps -q 2>$null
    if ($containers) {
        Write-Host "⚠ Some Docker containers are still running:" -ForegroundColor Yellow
        docker-compose ps 2>$null | ForEach-Object { Write-Host "  $_" -ForegroundColor Gray }
        $allClear = $false
    } else {
        Write-Host "✓ All Docker containers stopped" -ForegroundColor Green
    }
} catch {
    Write-Host "ℹ Docker status check skipped" -ForegroundColor Gray
}

Write-Host ""

if ($allClear) {
    Write-Host "🎉 SUCCESS: All Umi Health POS processes have been killed!" -ForegroundColor Green
} else {
    Write-Host "⚠ Some processes may still be running" -ForegroundColor Yellow
    if (-not $isAdmin) {
        Write-Host "💡 Try running this script as Administrator for full cleanup" -ForegroundColor Cyan
    }
    if (-not $Force) {
        Write-Host "💡 Try running with -Force flag for more aggressive cleanup" -ForegroundColor Cyan
    }
}

Write-Host ""
Write-Host "=== Process Cleanup Complete ===" -ForegroundColor Green
