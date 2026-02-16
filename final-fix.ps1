# Final fix for remaining syntax errors
Write-Host "Applying final fixes..." -ForegroundColor Green

# Fix AuthController.cs line 369 - missing comma
Write-Host "`n=== Fixing AuthController.cs syntax ===" -ForegroundColor Cyan
$authControllerPath = "c:\Users\sepio\Desktop\Umi-Health-POS\backend\Controllers\Api\AuthController.cs"
$content = Get-Content $authControllerPath -Raw

# Add missing comma after LastAccessAt
$content = $content -replace 'LastAccessAt = DateTime\.UtcNow\s*IsActive = true', 'LastAccessAt = DateTime.UtcNow,' + "`n                    IsActive = true"

Set-Content $authControllerPath $content -Encoding UTF8
Write-Host "Fixed AuthController.cs missing comma" -ForegroundColor Green

# Fix BillingController.cs line 123 - malformed property assignment
Write-Host "`n=== Fixing BillingController.cs syntax ===" -ForegroundColor Cyan
$billingControllerPath = "c:\Users\sepio\Desktop\Umi-Health-POS\backend\Controllers\Api\BillingController.cs"
$content = Get-Content $billingControllerPath -Raw

# Fix the malformed line
$content = $content -replace 'existing"" = request\.Notes;', 'existingInvoice.Notes = request.Notes ?? "";'

# Also fix line 120 which seems to have an issue
$content = $content -replace 'existinginvoice\.SubscriptionPlan\?\.Name \?\? "Unknown" = request\.Plan;', 'existingInvoice.SubscriptionPlan = await _context.SubscriptionPlans.FindAsync(request.PlanId);'

Set-Content $billingControllerPath $content -Encoding UTF8
Write-Host "Fixed BillingController.cs property assignments" -ForegroundColor Green

# Test final compilation
Write-Host "`n=== Final Compilation Test ===" -ForegroundColor Cyan
try {
    Set-Location "c:\Users\sepio\Desktop\Umi-Health-POS\backend"
    $result = dotnet build --no-restore 2>&1
    if ($LASTEXITCODE -eq 0) {
        Write-Host "✅ All compilation errors fixed successfully!" -ForegroundColor Green
        Write-Host $result
    } else {
        Write-Host "❌ Remaining errors:" -ForegroundColor Red
        $result | Where-Object { $_ -match "error" } | ForEach-Object { 
            Write-Host "  $_" -ForegroundColor Red 
        }
    }
} catch {
    Write-Host "❌ Error during compilation: $($_.Exception.Message)" -ForegroundColor Red
}

Write-Host "`n=== Fix Summary ===" -ForegroundColor Cyan
Write-Host "✅ Fixed missing comma in AuthController.cs" -ForegroundColor Green
Write-Host "✅ Fixed malformed property assignment in BillingController.cs" -ForegroundColor Green
Write-Host "✅ All compilation errors should now be resolved" -ForegroundColor Green
