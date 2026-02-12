@echo off
REM Umi Health POS - Kill All Running Processes (Batch Version)
REM This script stops all services and kills related processes

setlocal enabledelayedexpansion

echo === Umi Health POS - Killing All Running Processes ===
echo.

REM Check if running as Administrator
net session >nul 2>&1
if %errorlevel% neq 0 (
    echo WARNING: Not running as Administrator
    echo Some processes may not be able to be stopped
    echo For full functionality, run as Administrator
    echo.
)

REM Stop Docker containers
echo === Stopping Docker Containers ===
echo.
docker-compose down >nul 2>&1
if %errorlevel% equ 0 (
    echo ✓ Docker containers stopped successfully
) else (
    echo ⚠ Docker containers may not be running or accessible
)
echo.

REM Kill application processes
echo === Killing Application Processes ===
echo.

REM Kill .NET processes
echo Checking for .NET processes...
tasklist /FI "IMAGENAME eq dotnet.exe" 2>nul | findstr dotnet.exe >nul
if %errorlevel% equ 0 (
    echo Found .NET processes, killing...
    taskkill /F /IM dotnet.exe >nul 2>&1
    if %errorlevel% equ 0 (
        echo ✓ .NET processes killed
    ) else (
        echo ⚠ Failed to kill some .NET processes
    )
) else (
    echo No .NET processes found
)
echo.

REM Kill Node.js processes
echo Checking for Node.js processes...
tasklist /FI "IMAGENAME eq node.exe" 2>nul | findstr node.exe >nul
if %errorlevel% equ 0 (
    echo Found Node.js processes, killing...
    taskkill /F /IM node.exe >nul 2>&1
    if %errorlevel% equ 0 (
        echo ✓ Node.js processes killed
    ) else (
        echo ⚠ Failed to kill some Node.js processes
    )
) else (
    echo No Node.js processes found
)
echo.

REM Kill Nginx processes
echo Checking for Nginx processes...
tasklist /FI "IMAGENAME eq nginx.exe" 2>nul | findstr nginx.exe >nul
if %errorlevel% equ 0 (
    echo Found Nginx processes, killing...
    taskkill /F /IM nginx.exe >nul 2>&1
    if %errorlevel% equ 0 (
        echo ✓ Nginx processes killed
    ) else (
        echo ⚠ Failed to kill some Nginx processes
    )
) else (
    echo No Nginx processes found
)
echo.

REM Stop PostgreSQL services
echo === Stopping PostgreSQL Services ===
echo.

REM Try different PostgreSQL service names
set "pg_services=postgresql-x64-18 postgresql-x64-15 postgresql-x64-14 postgresql-x64-13"

for %%s in (%pg_services%) do (
    echo Checking for PostgreSQL service: %%s
    sc query %%s >nul 2>&1
    if !errorlevel! equ 0 (
        echo Found PostgreSQL service: %%s
        sc query %%s | findstr "RUNNING" >nul
        if !errorlevel! equ 0 (
            echo Stopping %%s...
            net stop %%s >nul 2>&1
            if !errorlevel! equ 0 (
                echo ✓ %%s stopped successfully
            ) else (
                echo ⚠ Failed to stop %%s (may need Administrator)
            )
        ) else (
            echo %%s is not running
        )
    ) else (
        echo Service %%s not found
    )
)
echo.

REM Kill processes by port
echo === Killing Processes by Port ===
echo.

REM Check port 5432 (PostgreSQL)
echo Checking for processes on port 5432...
for /f "tokens=5" %%a in ('netstat -ano ^| findstr ":5432" ^| findstr "LISTENING"') do (
    echo Found process %%a on port 5432
    taskkill /F /PID %%a >nul 2>&1
    if !errorlevel! equ 0 (
        echo ✓ Killed process %%a on port 5432
    ) else (
        echo ⚠ Failed to kill process %%a on port 5432
    )
)
if not defined %%a (
    echo No processes found on port 5432
)
echo.

REM Check port 5000 (Backend API)
echo Checking for processes on port 5000...
for /f "tokens=5" %%a in ('netstat -ano ^| findstr ":5000" ^| findstr "LISTENING"') do (
    echo Found process %%a on port 5000
    taskkill /F /PID %%a >nul 2>&1
    if !errorlevel! equ 0 (
        echo ✓ Killed process %%a on port 5000
    ) else (
        echo ⚠ Failed to kill process %%a on port 5000
    )
)
if not defined %%a (
    echo No processes found on port 5000
)
echo.

REM Check port 80 (Frontend)
echo Checking for processes on port 80...
for /f "tokens=5" %%a in ('netstat -ano ^| findstr ":80" ^| findstr "LISTENING"') do (
    echo Found process %%a on port 80
    taskkill /F /PID %%a >nul 2>&1
    if !errorlevel! equ 0 (
        echo ✓ Killed process %%a on port 80
    ) else (
        echo ⚠ Failed to kill process %%a on port 80
    )
)
if not defined %%a (
    echo No processes found on port 80
)
echo.

REM Final status check
echo === Final Status Check ===
echo.

set "all_clear=true"

REM Check ports
echo Checking port status...
netstat -ano | findstr ":5432" | findstr "LISTENING" >nul
if %errorlevel% equ 0 (
    echo ⚠ Port 5432 is still in use
    set "all_clear=false"
) else (
    echo ✓ Port 5432 is clear
)

netstat -ano | findstr ":5000" | findstr "LISTENING" >nul
if %errorlevel% equ 0 (
    echo ⚠ Port 5000 is still in use
    set "all_clear=false"
) else (
    echo ✓ Port 5000 is clear
)

netstat -ano | findstr ":80" | findstr "LISTENING" >nul
if %errorlevel% equ 0 (
    echo ⚠ Port 80 is still in use
    set "all_clear=false"
) else (
    echo ✓ Port 80 is clear
)

echo.

REM Check Docker containers
echo Checking Docker container status...
docker-compose ps -q >nul 2>&1
if %errorlevel% equ 0 (
    echo ⚠ Some Docker containers may still be running
    set "all_clear=false"
) else (
    echo ✓ All Docker containers stopped
)

echo.

if "%all_clear%"=="true" (
    echo 🎉 SUCCESS: All Umi Health POS processes have been killed!
) else (
    echo ⚠ Some processes may still be running
    echo 💡 Try running this script as Administrator for full cleanup
)

echo.
echo === Process Cleanup Complete ===
echo.
pause
