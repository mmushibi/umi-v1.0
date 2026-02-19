#!/usr/bin/env pwsh

# Demo Tenant Setup Script for Umi Health POS
# This script will create a demo tenant with a pharmacist and cashier

Write-Host "🏥 Umi Health POS - Demo Tenant Setup" -ForegroundColor Cyan
Write-Host "=====================================" -ForegroundColor Cyan

Write-Host "📋 This script will:" -ForegroundColor Yellow
Write-Host "   1. Build the application" -ForegroundColor White
Write-Host "   2. Run database migrations" -ForegroundColor White
Write-Host "   3. Seed demo data with tenant, pharmacist, and cashier" -ForegroundColor White
Write-Host ""

# Step 1: Build the application
Write-Host "🔨 Building application..." -ForegroundColor Green
try {
    dotnet build Umi-Health-POS.sln --configuration Release
    if ($LASTEXITCODE -eq 0) {
        Write-Host "✅ Build successful!" -ForegroundColor Green
    } else {
        Write-Host "❌ Build failed!" -ForegroundColor Red
        exit 1
    }
} catch {
    Write-Host "❌ Build error: $_" -ForegroundColor Red
    exit 1
}

Write-Host ""

# Step 2: Run the application to seed data
Write-Host "🌱 Starting application to seed demo data..." -ForegroundColor Green
Write-Host "   The application will start and automatically seed:" -ForegroundColor Yellow
Write-Host "   📦 Tenant: UMI001 - Umi Health Pharmacy" -ForegroundColor Cyan
Write-Host "   👨‍⚕️  Pharmacist: Grace Chilufya (grace@umihealth.com)" -ForegroundColor Cyan
Write-Host "   💰 Cashier: John Banda (john@umihealth.com)" -ForegroundColor Cyan
Write-Host ""
Write-Host "   🔐 Passwords are securely hashed using ASP.NET Core PasswordHasher" -ForegroundColor Green
Write-Host "   🏢 Branches: Umi Health Pharmacy - Lusaka, Umi Health Pharmacy - Kitwe" -ForegroundColor Cyan
Write-Host "   💊 Inventory: Paracetamol, Amoxicillin, Vitamin C" -ForegroundColor Cyan
Write-Host ""
Write-Host "🚀 Starting application..." -ForegroundColor Green
Write-Host "   The demo data will be seeded automatically on startup." -ForegroundColor Yellow
Write-Host "   Press Ctrl+C to stop the application." -ForegroundColor Gray
Write-Host ""

# Change to backend directory and run
Set-Location backend
dotnet run --configuration Release

Write-Host ""
Write-Host "✅ Demo tenant setup complete!" -ForegroundColor Green
Write-Host ""
Write-Host "📖 Demo Login Credentials:" -ForegroundColor Yellow
Write-Host "   Tenant ID: UMI001" -ForegroundColor White
Write-Host "   Admin: admin@umihealth.com / Admin123!" -ForegroundColor White
Write-Host "   Pharmacist: grace@umihealth.com / Pharmacist123!" -ForegroundColor White
Write-Host "   Cashier: john@umihealth.com / Cashier123!" -ForegroundColor White
Write-Host ""
Write-Host "🌐 Access the application at: http://localhost:5000" -ForegroundColor Cyan
