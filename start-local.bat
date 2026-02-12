@echo off
REM Umi Health POS - Local Development Startup Script (Batch Version)
REM This script starts the database, backend, and frontend services locally

setlocal enabledelayedexpansion

echo === Umi Health POS - Local Development Startup ===
echo.

REM Check if Docker is installed
docker --version >nul 2>&1
if %errorlevel% neq 0 (
    echo ERROR: Docker is not installed or not running
    echo Please install Docker Desktop and ensure it's running
    pause
    exit /b 1
)
echo ✓ Docker is installed

REM Check if Docker Compose is available
docker-compose --version >nul 2>&1
if %errorlevel% neq 0 (
    echo ERROR: Docker Compose is not available
    echo Please ensure Docker Compose is installed
    pause
    exit /b 1
)
echo ✓ Docker Compose is available

REM Check if .NET SDK is installed
dotnet --version >nul 2>&1
if %errorlevel% neq 0 (
    echo WARNING: .NET SDK is not installed
    echo Please install .NET SDK for backend development
) else (
    echo ✓ .NET SDK is installed
)

REM Check if Node.js is installed
node --version >nul 2>&1
if %errorlevel% neq 0 (
    echo WARNING: Node.js is not installed
    echo Please install Node.js for frontend development
) else (
    echo ✓ Node.js is installed
)

echo.
echo Starting services...
echo.

REM Start PostgreSQL database
echo === Starting PostgreSQL Database ===
docker-compose up -d postgres
if %errorlevel% equ 0 (
    echo ✓ PostgreSQL startup initiated
) else (
    echo ✗ Failed to start PostgreSQL
)
echo.

REM Start Backend API
echo === Starting .NET Backend API ===
docker-compose up -d backend
if %errorlevel% equ 0 (
    echo ✓ Backend API startup initiated
) else (
    echo ✗ Failed to start Backend API
)
echo.

REM Start Frontend
echo === Starting Frontend ===
docker-compose up -d frontend
if %errorlevel% equ 0 (
    echo ✓ Frontend startup initiated
) else (
    echo ✗ Failed to start Frontend
)
echo.

REM Wait a moment for services to start
echo Waiting for services to initialize...
timeout /t 10 /nobreak >nul

REM Display service status
echo === Service Status ===
echo.

docker-compose ps

echo.
echo === Access URLs ===
echo.
echo 🌐 Frontend Application: http://localhost
echo 🔧 Backend API: http://localhost:5000
echo 🗄️  Database: localhost:5432 (umi_health_pos/umi_admin)
echo.
echo === Useful Commands ===
echo.
echo View logs:
echo   docker-compose logs -f [service-name]
echo.
echo Stop all services:
echo   docker-compose down
echo.
echo Restart services:
echo   docker-compose restart [service-name]
echo.
echo Check container status:
echo   docker-compose ps
echo.
echo === Startup Complete ===
echo Your Umi Health POS application is starting up!
echo Please wait a few moments for all services to fully initialize.
echo.
pause
