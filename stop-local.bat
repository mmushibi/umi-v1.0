@echo off
REM Umi Health POS - Local Development Stop Script (Batch Version)

setlocal enabledelayedexpansion

echo === Stopping Umi Health POS Services ===
echo.

REM Check if Docker is running
docker --version >nul 2>&1
if %errorlevel% neq 0 (
    echo ERROR: Docker is not running
    echo Cannot stop services if Docker is not running
    pause
    exit /b 1
)

echo Current service status:
docker-compose ps
echo.

echo Stopping services...
docker-compose down

if %errorlevel% equ 0 (
    echo ✓ Services stopped successfully
) else (
    echo ✗ Failed to stop services
)

echo.
echo Verifying services are stopped...
docker-compose ps

echo.
echo === Services Stopped ===
echo To restart services, run: start-local.bat
echo.
pause
