# Umi Health POS - Local Development Startup Script
# This script starts the database, backend, and frontend services locally

param(
    [switch]$SkipDatabase,
    [switch]$SkipBackend,
    [switch]$SkipFrontend,
    [switch]$Rebuild,
    [switch]$Help
)

# Display help information
if ($Help) {
    Write-Host "=== Umi Health POS - Local Development Startup Script ===" -ForegroundColor Green
    Write-Host ""
    Write-Host "Usage: .\start-local.ps1 [options]" -ForegroundColor Yellow
    Write-Host ""
    Write-Host "Options:" -ForegroundColor Cyan
    Write-Host "  -SkipDatabase    Skip starting the PostgreSQL database"
    Write-Host "  -SkipBackend     Skip starting the .NET backend API"
    Write-Host "  -SkipFrontend    Skip starting the frontend development server"
    Write-Host "  -Rebuild         Rebuild Docker containers before starting"
    Write-Host "  -Help            Show this help message"
    Write-Host ""
    Write-Host "Examples:" -ForegroundColor Yellow
    Write-Host "  .\start-local.ps1                    # Start all services"
    Write-Host "  .\start-local.ps1 -SkipDatabase      # Start backend and frontend only"
    Write-Host "  .\start-local.ps1 -Rebuild           # Rebuild and start all services"
    Write-Host ""
    exit 0
}

# Function to check if a port is in use
function Test-PortInUse {
    param([int]$Port)
    $connection = New-Object System.Net.Sockets.TcpClient
    try {
        $connection.Connect("localhost", $Port)
        $connection.Close()
        return $true
    }
    catch {
        return $false
    }
}

# Function to wait for service to be ready
function Wait-ForService {
    param(
        [string]$ServiceName,
        [int]$Port,
        [string]$Path = ""
    )
    
    Write-Host "Waiting for $ServiceName to be ready..." -ForegroundColor Yellow
    $maxAttempts = 30
    $attempt = 0
    
    while ($attempt -lt $maxAttempts) {
        if (Test-PortInUse -Port $Port) {
            Write-Host "✓ $ServiceName is ready!" -ForegroundColor Green
            return $true
        }
        
        $attempt++
        Write-Host "Attempt $attempt/$maxAttempts..." -ForegroundColor Gray
        Start-Sleep -Seconds 2
    }
    
    Write-Host "✗ $ServiceName failed to start within expected time" -ForegroundColor Red
    return $false
}

# Main execution
Write-Host "=== Umi Health POS - Local Development Startup ===" -ForegroundColor Green
Write-Host ""

# Check prerequisites
Write-Host "Checking prerequisites..." -ForegroundColor Yellow

# Check if Docker is installed and running
try {
    $dockerVersion = docker --version 2>$null
    if ($LASTEXITCODE -eq 0) {
        Write-Host "✓ Docker: $dockerVersion" -ForegroundColor Green
    } else {
        throw "Docker not found"
    }
} catch {
    Write-Host "✗ Docker is not installed or not running" -ForegroundColor Red
    Write-Host "Please install Docker Desktop and ensure it's running" -ForegroundColor Yellow
    exit 1
}

# Check if Docker Compose is available
try {
    $composeVersion = docker-compose --version 2>$null
    if ($LASTEXITCODE -eq 0) {
        Write-Host "✓ Docker Compose: $composeVersion" -ForegroundColor Green
    } else {
        throw "Docker Compose not found"
    }
} catch {
    Write-Host "✗ Docker Compose is not available" -ForegroundColor Red
    Write-Host "Please ensure Docker Compose is installed" -ForegroundColor Yellow
    exit 1
}

# Check if .NET SDK is installed (for backend development)
try {
    $dotnetVersion = dotnet --version 2>$null
    if ($LASTEXITCODE -eq 0) {
        Write-Host "✓ .NET SDK: $dotnetVersion" -ForegroundColor Green
    } else {
        throw ".NET SDK not found"
    }
} catch {
    Write-Host "✗ .NET SDK is not installed" -ForegroundColor Red
    Write-Host "Please install .NET SDK for backend development" -ForegroundColor Yellow
}

# Check if Node.js is installed (for frontend development)
try {
    $nodeVersion = node --version 2>$null
    if ($LASTEXITCODE -eq 0) {
        Write-Host "✓ Node.js: $nodeVersion" -ForegroundColor Green
    } else {
        throw "Node.js not found"
    }
} catch {
    Write-Host "✗ Node.js is not installed" -ForegroundColor Red
    Write-Host "Please install Node.js for frontend development" -ForegroundColor Yellow
}

Write-Host ""

# Check if ports are available
Write-Host "Checking port availability..." -ForegroundColor Yellow

$ports = @{
    "PostgreSQL" = 5432
    "Backend API" = 5000
    "Frontend" = 80
}

$portsBlocked = @()

foreach ($serviceName in $ports.Keys) {
    $port = $ports[$serviceName]
    if (Test-PortInUse -Port $port) {
        Write-Host "⚠ Port $port ($serviceName) is already in use" -ForegroundColor Yellow
        $portsBlocked += $serviceName
    } else {
        Write-Host "✓ Port $port ($serviceName) is available" -ForegroundColor Green
    }
}

if ($portsBlocked.Count -gt 0) {
    Write-Host ""
    Write-Host "Warning: The following ports are already in use:" -ForegroundColor Yellow
    $portsBlocked | ForEach-Object { Write-Host "  - $_" -ForegroundColor Gray }
    Write-Host "You may need to stop existing services or use different ports" -ForegroundColor Yellow
    Write-Host ""
}

# Start services
Write-Host "Starting services..." -ForegroundColor Yellow
Write-Host ""

# Start PostgreSQL database
if (-not $SkipDatabase) {
    Write-Host "=== Starting PostgreSQL Database ===" -ForegroundColor Cyan
    
    if ($Rebuild) {
        Write-Host "Rebuilding PostgreSQL container..." -ForegroundColor Yellow
        docker-compose down postgres 2>$null
        docker-compose up -d --build postgres
    } else {
        docker-compose up -d postgres
    }
    
    if ($LASTEXITCODE -eq 0) {
        if (Wait-ForService -ServiceName "PostgreSQL" -Port 5432) {
            Write-Host "✓ PostgreSQL started successfully" -ForegroundColor Green
        } else {
            Write-Host "✗ PostgreSQL failed to start" -ForegroundColor Red
            if (-not $SkipBackend) {
                Write-Host "Skipping backend startup due to database failure" -ForegroundColor Yellow
                $SkipBackend = $true
            }
        }
    } else {
        Write-Host "✗ Failed to start PostgreSQL" -ForegroundColor Red
        $SkipBackend = $true
    }
    Write-Host ""
}

# Start Backend API
if (-not $SkipBackend) {
    Write-Host "=== Starting .NET Backend API ===" -ForegroundColor Cyan
    
    if ($Rebuild) {
        Write-Host "Rebuilding backend container..." -ForegroundColor Yellow
        docker-compose down backend 2>$null
        docker-compose up -d --build backend
    } else {
        docker-compose up -d backend
    }
    
    if ($LASTEXITCODE -eq 0) {
        if (Wait-ForService -ServiceName "Backend API" -Port 5000) {
            Write-Host "✓ Backend API started successfully" -ForegroundColor Green
        } else {
            Write-Host "✗ Backend API failed to start" -ForegroundColor Red
        }
    } else {
        Write-Host "✗ Failed to start Backend API" -ForegroundColor Red
    }
    Write-Host ""
}

# Start Frontend
if (-not $SkipFrontend) {
    Write-Host "=== Starting Frontend ===" -ForegroundColor Cyan
    
    # Check if we need to build CSS first
    if (Test-Path "src/input.css") {
        Write-Host "Building CSS with Tailwind..." -ForegroundColor Yellow
        npm run build-css-prod 2>$null
        if ($LASTEXITCODE -eq 0) {
            Write-Host "✓ CSS built successfully" -ForegroundColor Green
        } else {
            Write-Host "⚠ CSS build failed, using existing styles" -ForegroundColor Yellow
        }
    }
    
    if ($Rebuild) {
        Write-Host "Rebuilding frontend container..." -ForegroundColor Yellow
        docker-compose down frontend 2>$null
        docker-compose up -d --build frontend
    } else {
        docker-compose up -d frontend
    }
    
    if ($LASTEXITCODE -eq 0) {
        if (Wait-ForService -ServiceName "Frontend" -Port 80) {
            Write-Host "✓ Frontend started successfully" -ForegroundColor Green
        } else {
            Write-Host "✗ Frontend failed to start" -ForegroundColor Red
        }
    } else {
        Write-Host "✗ Failed to start Frontend" -ForegroundColor Red
    }
    Write-Host ""
}

# Display service status
Write-Host "=== Service Status ===" -ForegroundColor Green
Write-Host ""

$services = @(
    @{ Name = "PostgreSQL"; Port = 5432; Skipped = $SkipDatabase },
    @{ Name = "Backend API"; Port = 5000; Skipped = $SkipBackend },
    @{ Name = "Frontend"; Port = 80; Skipped = $SkipFrontend }
)

foreach ($service in $services) {
    if ($service.Skipped) {
        Write-Host "$($service.Name): Skipped" -ForegroundColor Gray
    } elseif (Test-PortInUse -Port $service.Port) {
        Write-Host "$($service.Name): Running (Port $($service.Port))" -ForegroundColor Green
    } else {
        Write-Host "$($service.Name): Not running" -ForegroundColor Red
    }
}

Write-Host ""

# Display access URLs
Write-Host "=== Access URLs ===" -ForegroundColor Green
Write-Host ""

if (-not $SkipFrontend -and (Test-PortInUse -Port 80)) {
    Write-Host "🌐 Frontend Application: http://localhost" -ForegroundColor Cyan
}

if (-not $SkipBackend -and (Test-PortInUse -Port 5000)) {
    Write-Host "🔧 Backend API: http://localhost:5000" -ForegroundColor Cyan
    Write-Host "📚 API Documentation: http://localhost:5000/swagger" -ForegroundColor Cyan
}

if (-not $SkipDatabase -and (Test-PortInUse -Port 5432)) {
    Write-Host "🗄️  Database: localhost:5432 (umi_health_pos/umi_admin)" -ForegroundColor Cyan
}

Write-Host ""

# Display useful commands
Write-Host "=== Useful Commands ===" -ForegroundColor Green
Write-Host ""
Write-Host "View logs:" -ForegroundColor Yellow
Write-Host "  docker-compose logs -f [service-name]" -ForegroundColor Gray
Write-Host ""
Write-Host "Stop all services:" -ForegroundColor Yellow
Write-Host "  docker-compose down" -ForegroundColor Gray
Write-Host ""
Write-Host "Restart services:" -ForegroundColor Yellow
Write-Host "  docker-compose restart [service-name]" -ForegroundColor Gray
Write-Host ""
Write-Host "Check container status:" -ForegroundColor Yellow
Write-Host "  docker-compose ps" -ForegroundColor Gray
Write-Host ""

Write-Host "=== Startup Complete ===" -ForegroundColor Green
Write-Host "Your Umi Health POS application is starting up!" -ForegroundColor Cyan
Write-Host "Please wait a few moments for all services to fully initialize." -ForegroundColor Yellow
