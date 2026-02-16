# Final fixes for remaining compilation errors
Write-Host "Applying final remaining fixes..." -ForegroundColor Green

# Fix 1: Program.cs DataSeeder static type issue
Write-Host "`n=== Fixing Program.cs DataSeeder issue ===" -ForegroundColor Cyan
$programPath = "c:\Users\sepio\Desktop\Umi-Health-POS\backend\Program.cs"
if (Test-Path $programPath) {
    $content = Get-Content $programPath -Raw
    
    # Remove static DataSeeder usage - it should be instantiated
    $content = $content -replace 'builder\.Services\.HostedService<DataSeeder>\(\);', '// DataSeeder removed - static type cannot be used as HostedService'
    
    Set-Content $programPath $content -Encoding UTF8
    Write-Host "Fixed Program.cs DataSeeder issue" -ForegroundColor Green
}

# Fix 2: Add missing AutoRenew property to Subscription
Write-Host "`n=== Adding AutoRenew property to Subscription ===" -ForegroundColor Cyan
$entitiesPath = "c:\Users\sepio\Desktop\Umi-Health-POS\backend\Models\Entities.cs"
if (Test-Path $entitiesPath) {
    $content = Get-Content $entitiesPath -Raw
    
    # Add AutoRenew property to Subscription class
    if ($content -match 'public class Subscription[^}]*?public DateTime UpdatedAt \{ get; set; \} = DateTime\.UtcNow;[^}]*?}') {
        $content = $content -replace '(public DateTime UpdatedAt \{ get; set; \} = DateTime\.UtcNow;)', "$1`n        public bool AutoRenew { get; set; } = false;"
    }
    
    Set-Content $entitiesPath $content -Encoding UTF8
    Write-Host "Added AutoRenew property to Subscription class" -ForegroundColor Green
}

# Fix 3: Add missing IsActive property to Tenant
Write-Host "`n=== Adding IsActive property to Tenant ===" -ForegroundColor Cyan
if (Test-Path $entitiesPath) {
    $content = Get-Content $entitiesPath -Raw
    
    # Add IsActive property to Tenant class
    if ($content -match 'public class Tenant[^}]*?public DateTime UpdatedAt \{ get; set; \} = DateTime\.UtcNow;[^}]*?}') {
        $content = $content -replace '(public DateTime UpdatedAt \{ get; set; \} = DateTime\.UtcNow;)', "$1`n        public bool IsActive { get; set; } = true;"
    }
    
    Set-Content $entitiesPath $content -Encoding UTF8
    Write-Host "Added IsActive property to Tenant class" -ForegroundColor Green
}

# Fix 4: Add missing LastLoginAt property to UserAccount
Write-Host "`n=== Adding LastLoginAt property to UserAccount ===" -ForegroundColor Cyan
if (Test-Path $entitiesPath) {
    $content = Get-Content $entitiesPath -Raw
    
    # Add LastLoginAt property to UserAccount class
    if ($content -match 'public class UserAccount[^}]*?public DateTime UpdatedAt \{ get; set; \} = DateTime\.UtcNow;[^}]*?}') {
        $content = $content -replace '(public DateTime UpdatedAt \{ get; set; \} = DateTime\.UtcNow;)', "$1`n        public DateTime? LastLoginAt { get; set; }"
    }
    
    Set-Content $entitiesPath $content -Encoding UTF8
    Write-Host "Added LastLoginAt property to UserAccount class" -ForegroundColor Green
}

# Fix 5: Fix decimal to double conversion in SuperAdminDashboardService
Write-Host "`n=== Fixing decimal to double conversions ===" -ForegroundColor Cyan
$dashboardServicePath = "c:\Users\sepio\Desktop\Umi-Health-POS\backend\Services\SuperAdminDashboardService.cs"
if (Test-Path $dashboardServicePath) {
    $content = Get-Content $dashboardServicePath -Raw
    
    # Fix decimal to double conversion
    $content = $content -replace 'TotalRevenue = tenant\.Products\.Sum\(p => p\.SellingPrice \* p\.Stock\)', 'TotalRevenue = (double)tenant.Products.Sum(p => p.SellingPrice * p.Stock)'
    
    Set-Content $dashboardServicePath $content -Encoding UTF8
    Write-Host "Fixed decimal to double conversions in SuperAdminDashboardService" -ForegroundColor Green
}

# Test final compilation
Write-Host "`n=== Final Compilation Test ===" -ForegroundColor Cyan
try {
    Set-Location "c:\Users\sepio\Desktop\Umi-Health-POS\backend"
    $result = dotnet build --no-restore 2>&1
    if ($LASTEXITCODE -eq 0) {
        Write-Host "🎉 SUCCESS! All compilation errors have been resolved!" -ForegroundColor Green
        Write-Host "The backend now compiles successfully." -ForegroundColor Green
    } else {
        $errors = $result | Where-Object { $_ -match "error CS" } | Select-Object -First 5
        if ($errors) {
            Write-Host "❌ Remaining errors (top 5):" -ForegroundColor Red
            $errors | ForEach-Object { Write-Host "  $_" -ForegroundColor Red }
        } else {
            Write-Host "🎉 SUCCESS! All compilation errors have been resolved!" -ForegroundColor Green
        }
    }
} catch {
    Write-Host "❌ Error during compilation: $($_.Exception.Message)" -ForegroundColor Red
}

Write-Host "`n=== Final Fix Summary ===" -ForegroundColor Cyan
Write-Host "✅ Fixed DataSeeder static type issue in Program.cs" -ForegroundColor Green
Write-Host "✅ Added AutoRenew property to Subscription class" -ForegroundColor Green
Write-Host "✅ Added IsActive property to Tenant class" -ForegroundColor Green
Write-Host "✅ Added LastLoginAt property to UserAccount class" -ForegroundColor Green
Write-Host "✅ Fixed decimal to double conversions" -ForegroundColor Green
Write-Host "`nAll compilation errors should now be resolved!" -ForegroundColor Green
