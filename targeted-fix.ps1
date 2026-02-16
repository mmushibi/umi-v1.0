# Targeted fix for only the missing properties
Write-Host "Applying targeted fixes..." -ForegroundColor Green

$entitiesPath = "c:\Users\sepio\Desktop\Umi-Health-POS\backend\Models\Entities.cs"
$content = Get-Content $entitiesPath -Raw

# Add Subscriptions property to Pharmacy class only
if ($content -match '(public class Pharmacy\s*\{[^}]+?public virtual Tenant\? Tenant \{ get; set; \})') {
    $content = $content -replace '(public virtual Tenant\? Tenant \{ get; set; \})', "$1`n        public virtual ICollection<Subscription> Subscriptions { get; set; } = new List<Subscription>();"
    Write-Host "Added Subscriptions property to Pharmacy class" -ForegroundColor Green
}

# Add LastAccessAt property to UserSession class only
if ($content -match '(public class UserSession[^}]+?public DateTime UpdatedAt \{ get; set; \} = DateTime\.UtcNow;)') {
    $content = $content -replace '(public DateTime UpdatedAt \{ get; set; \} = DateTime\.UtcNow;)', "$1`n        public DateTime? LastAccessAt { get; set; }"
    Write-Host "Added LastAccessAt property to UserSession class" -ForegroundColor Green
}

# Add IssueDate and Notes properties to Invoice class only
if ($content -match '(public class Invoice[^}]+?public DateTime UpdatedAt \{ get; set; \} = DateTime\.UtcNow;)') {
    $content = $content -replace '(public DateTime UpdatedAt \{ get; set; \} = DateTime\.UtcNow;)', "$1`n        public DateTime? IssueDate { get; set; }`n        public string? Notes { get; set; }"
    Write-Host "Added IssueDate and Notes properties to Invoice class" -ForegroundColor Green
}

Set-Content $entitiesPath $content -Encoding UTF8

# Test compilation
Write-Host "`n=== Testing Compilation ===" -ForegroundColor Cyan
try {
    Set-Location "c:\Users\sepio\Desktop\Umi-Health-POS\backend"
    $result = dotnet build --no-restore 2>&1
    if ($LASTEXITCODE -eq 0) {
        Write-Host "✅ All compilation errors fixed!" -ForegroundColor Green
    } else {
        Write-Host "❌ Remaining errors:" -ForegroundColor Red
        $result | Where-Object { $_ -match "error" } | ForEach-Object { 
            Write-Host "  $_" -ForegroundColor Red 
        }
    }
} catch {
    Write-Host "❌ Error: $($_.Exception.Message)" -ForegroundColor Red
}

Write-Host "Targeted fix completed!" -ForegroundColor Green
