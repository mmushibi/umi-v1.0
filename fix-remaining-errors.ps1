# Fix Remaining Compilation Errors Script
Write-Host "Fixing remaining compilation errors..." -ForegroundColor Green

# Fix BillingController.cs syntax error
Write-Host "`n=== Fixing BillingController.cs syntax error ===" -ForegroundColor Cyan
$billingControllerPath = "c:\Users\sepio\Desktop\Umi-Health-POS\backend\Controllers\Api\BillingController.cs"
$content = Get-Content $billingControllerPath -Raw

# Fix the problematic line that was created by the previous script
$content = $content -replace '"" // Notes property not available', '""'

Set-Content $billingControllerPath $content -Encoding UTF8
Write-Host "Fixed BillingController.cs syntax error" -ForegroundColor Green

# Fix Entities.cs properly
Write-Host "`n=== Fixing Entities.cs missing properties ===" -ForegroundColor Cyan
$entitiesPath = "c:\Users\sepio\Desktop\Umi-Health-POS\backend\Models\Entities.cs"
$content = Get-Content $entitiesPath -Raw

# Add Subscriptions property to Pharmacy class
if ($content -notmatch 'public virtual ICollection<Subscription> Subscriptions') {
    # Find the Pharmacy class and add the property before the closing brace
    $pharmacyClassEnd = 'public virtual Tenant\? Tenant \{ get; set; \}'
    if ($content -match $pharmacyClassEnd) {
        $content = $content -replace $pharmacyClassEnd, "$&`n        public virtual ICollection<Subscription> Subscriptions { get; set; } = new List<Subscription>();"
    }
}

# Add LastAccessAt to UserSession class
if ($content -notmatch 'LastAccessAt') {
    # Find UserSession class and add the property
    $userSessionEnd = 'public DateTime UpdatedAt \{ get; set; \} = DateTime\.UtcNow;'
    if ($content -match $userSessionEnd) {
        $content = $content -replace $userSessionEnd, "$&`n        public DateTime? LastAccessAt { get; set; }"
    }
}

# Add missing properties to Invoice class
if ($content -notmatch 'IssueDate') {
    # Find Invoice class and add properties
    $invoiceEnd = 'public DateTime UpdatedAt \{ get; set; \} = DateTime\.UtcNow;'
    if ($content -match $invoiceEnd) {
        $content = $content -replace $invoiceEnd, "$&`n        public DateTime? IssueDate { get; set; }`n        public string? Notes { get; set; }"
    }
}

Set-Content $entitiesPath $content -Encoding UTF8
Write-Host "Fixed Entities.cs missing properties" -ForegroundColor Green

# Fix AuthController.cs remaining issues
Write-Host "`n=== Fixing AuthController.cs remaining issues ===" -ForegroundColor Cyan
$authControllerPath = "c:\Users\sepio\Desktop\Umi-Health-POS\backend\Controllers\Api\AuthController.cs"
$content = Get-Content $authControllerPath -Raw

# Fix the User ID type issue - keep it as string for consistency with JWT
$content = $content -replace 'public string Id \{ get; set; \} = string\.Empty;', 'public string Id { get; set; } = string.Empty;'

# Fix the claims generation
$content = $content -replace 'new Claim\(ClaimTypes\.NameIdentifier, user\.Id\),', 'new Claim(ClaimTypes.NameIdentifier, user.Id),'

# Fix UserSession property assignments
$content = $content -replace 'LastAccessAt = DateTime\.UtcNow, // Track last activity', 'LastAccessAt = DateTime.UtcNow'

Set-Content $authControllerPath $content -Encoding UTF8
Write-Host "Fixed AuthController.cs remaining issues" -ForegroundColor Green

# Test compilation again
Write-Host "`n=== Testing Compilation ===" -ForegroundColor Cyan
try {
    Set-Location "c:\Users\sepio\Desktop\Umi-Health-POS\backend"
    $result = dotnet build --no-restore 2>&1
    if ($LASTEXITCODE -eq 0) {
        Write-Host "✅ Compilation successful!" -ForegroundColor Green
    } else {
        Write-Host "❌ Compilation still has errors:" -ForegroundColor Red
        $result | Where-Object { $_ -match "error" } | ForEach-Object { Write-Host "  $_" -ForegroundColor Red }
    }
} catch {
    Write-Host "❌ Error during compilation test: $($_.Exception.Message)" -ForegroundColor Red
}

Write-Host "`nFix script completed!" -ForegroundColor Green
