# Simple final fix for compilation errors
Write-Host "Applying simple final fix..." -ForegroundColor Green

# Fix UserResponse class directly
Write-Host "`n=== Fixing UserResponse class directly ===" -ForegroundColor Cyan
$userModelPath = "c:\Users\sepio\Desktop\Umi-Health-POS\backend\Models\User.cs"
$content = Get-Content $userModelPath -Raw

# Remove and recreate UserResponse class
$content = $content -replace 'public class UserResponse\s*\{[^}]*?\}', 'public class UserResponse { public string Id { get; set; } = string.Empty; public string Name { get; set; } = string.Empty; public string Email { get; set; } = string.Empty; public string Phone { get; set; } = string.Empty; public string Address { get; set; } = string.Empty; public string Role { get; set; } = string.Empty; public string PharmacyId { get; set; } = string.Empty; public string PharmacyName { get; set; } = string.Empty; public string? Branch { get; set; } = string.Empty; public string Status { get; set; } = string.Empty; public DateTime? LastLogin { get; set; } public DateTime CreatedAt { get; set; } }'

Set-Content $userModelPath $content -Encoding UTF8
Write-Host "Fixed UserResponse class" -ForegroundColor Green

# Test compilation
Write-Host "`n=== Testing Compilation ===" -ForegroundColor Cyan
try {
    Set-Location "c:\Users\sepio\Desktop\Umi-Health-POS\backend"
    $result = dotnet build --no-restore 2>&1
    $errors = $result | Where-Object { $_ -match "error CS" }
    if ($errors) {
        Write-Host "Remaining errors: $($errors.Count)" -ForegroundColor Red
        $errors | Select-Object -First 3 | ForEach-Object { Write-Host "  $_" -ForegroundColor Red }
    } else {
        Write-Host "🎉 SUCCESS! All compilation errors resolved!" -ForegroundColor Green
    }
} catch {
    Write-Host "Error: $($_.Exception.Message)" -ForegroundColor Red
}

Write-Host "Simple final fix completed!" -ForegroundColor Green
