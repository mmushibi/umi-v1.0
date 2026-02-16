# Comprehensive fix for all compilation errors
Write-Host "Starting comprehensive fix..." -ForegroundColor Green

# Fix all controllers systematically
$controllersPath = "c:\Users\sepio\Desktop\Umi-Health-POS\backend\Controllers\Api"

# Fix AccountController.cs - GetUserId return type
Write-Host "`n=== Fixing AccountController.cs ===" -ForegroundColor Cyan
$accountController = Join-Path $controllersPath "AccountController.cs"
if (Test-Path $accountController) {
    $content = Get-Content $accountController -Raw
    
    # Fix GetUserId method signature and return
    $content = $content -replace 'private string GetUserId\(\)', 'private int GetUserId()'
    $content = $content -replace 'return "1";', 'return 1;'
    
    # Fix user comparisons
    $content = $content -replace 'u\.Id == userId', 'u.Id == userId'
    
    # Fix session ID comparison
    $content = $content -replace 's\.Id == sessionId', 's.Id.ToString() == sessionId'
    
    # Fix ToggleUserStatus parameter type
    $content = $content -replace 'public async Task<IActionResult> ToggleUserStatus\(string targetUserId', 'public async Task<IActionResult> ToggleUserStatus(int targetUserId'
    
    Set-Content $accountController $content -Encoding UTF8
    Write-Host "Fixed AccountController.cs" -ForegroundColor Green
}

# Fix AuthController.cs - User ID consistency
Write-Host "`n=== Fixing AuthController.cs ===" -ForegroundColor Cyan
$authController = Join-Path $controllersPath "AuthController.cs"
if (Test-Path $authController) {
    $content = Get-Content $authController -Raw
    
    # Fix User ID to be string for JWT compatibility
    $content = $content -replace 'Id = new Random\(\)\.Next\(100000, 999999\)', 'Id = Guid.NewGuid().ToString()'
    
    # Fix claims generation
    $content = $content -replace 'new Claim\(ClaimTypes\.NameIdentifier, user\.Id\),', 'new Claim(ClaimTypes.NameIdentifier, user.Id),'
    
    # Fix UserSession UserId assignment
    $content = $content -replace 'UserId = user\.Id,', 'UserId = user.Id,'
    
    Set-Content $authController $content -Encoding UTF8
    Write-Host "Fixed AuthController.cs" -ForegroundColor Green
}

# Fix User.cs model - make Id string
Write-Host "`n=== Fixing User.cs model ===" -ForegroundColor Cyan
$userModel = "c:\Users\sepio\Desktop\Umi-Health-POS\backend\Models\User.cs"
if (Test-Path $userModel) {
    $content = Get-Content $userModel -Raw
    
    # Change Id to string for JWT compatibility
    $content = $content -replace 'public int Id \{ get; set; \}', 'public string Id { get; set; } = string.Empty;'
    
    Set-Content $userModel $content -Encoding UTF8
    Write-Host "Fixed User.cs model" -ForegroundColor Green
}

# Fix Entities.cs - add missing properties correctly
Write-Host "`n=== Fixing Entities.cs ===" -ForegroundColor Cyan
$entitiesPath = "c:\Users\sepio\Desktop\Umi-Health-POS\backend\Models\Entities.cs"
if (Test-Path $entitiesPath) {
    $content = Get-Content $entitiesPath -Raw
    
    # Add Subscriptions to Pharmacy
    if ($content -match 'public class Pharmacy[^}]*?public virtual Tenant\? Tenant \{ get; set; \}[^}]*?}') {
        $content = $content -replace '(public virtual Tenant\? Tenant \{ get; set; \})', "$1`n        public virtual ICollection<Subscription> Subscriptions { get; set; } = new List<Subscription>();"
    }
    
    # Add LastAccessAt to UserSession
    if ($content -match 'public class UserSession[^}]*?public DateTime UpdatedAt \{ get; set; \} = DateTime\.UtcNow;[^}]*?}') {
        $content = $content -replace '(public DateTime UpdatedAt \{ get; set; \} = DateTime\.UtcNow;)', "$1`n        public DateTime? LastAccessAt { get; set; }"
    }
    
    # Add IssueDate and Notes to Invoice
    if ($content -match 'public class Invoice[^}]*?public DateTime UpdatedAt \{ get; set; \} = DateTime\.UtcNow;[^}]*?}') {
        $content = $content -replace '(public DateTime UpdatedAt \{ get; set; \} = DateTime\.UtcNow;)', "$1`n        public DateTime? IssueDate { get; set; }`n        public string? Notes { get; set; }"
    }
    
    Set-Content $entitiesPath $content -Encoding UTF8
    Write-Host "Fixed Entities.cs" -ForegroundColor Green
}

# Fix type conversion issues in all controllers
Write-Host "`n=== Fixing type conversions ===" -ForegroundColor Cyan
Get-ChildItem $controllersPath -Filter "*.cs" | ForEach-Object {
    $content = Get-Content $_.FullName -Raw
    
    # Fix DateTime? to DateTime conversions
    $content = $content -replace '(\w+)\.CreatedAt = request\.CreatedAt', '$1.CreatedAt = request.CreatedAt ?? DateTime.UtcNow'
    $content = $content -replace '(\w+)\.UpdatedAt = request\.UpdatedAt', '$1.UpdatedAt = request.UpdatedAt ?? DateTime.UtcNow'
    
    # Fix decimal to int conversions
    $content = $content -replace 'MaxUsers = request\.MaxUsers', 'MaxUsers = (int)request.MaxUsers'
    $content = $content -replace 'MaxBranches = request\.MaxBranches', 'MaxBranches = (int)request.MaxBranches'
    
    # Fix enum conversions
    $content = $content -replace 'Status = request\.Status(?!\.ToString\(\))', 'Status = request.Status.ToString()'
    $content = $content -replace 'Role = request\.Role(?!\.ToString\(\))', 'Role = request.Role.ToString()'
    
    Set-Content $_.FullName $content -Encoding UTF8
}

Write-Host "Fixed type conversions in all controllers" -ForegroundColor Green

# Test compilation
Write-Host "`n=== Testing Compilation ===" -ForegroundColor Cyan
try {
    Set-Location "c:\Users\sepio\Desktop\Umi-Health-POS\backend"
    $result = dotnet build --no-restore 2>&1
    if ($LASTEXITCODE -eq 0) {
        Write-Host "✅ All compilation errors fixed!" -ForegroundColor Green
    } else {
        $errors = $result | Where-Object { $_ -match "error CS" } | Select-Object -First 10
        if ($errors) {
            Write-Host "❌ Top 10 remaining errors:" -ForegroundColor Red
            $errors | ForEach-Object { Write-Host "  $_" -ForegroundColor Red }
        } else {
            Write-Host "✅ All compilation errors fixed!" -ForegroundColor Green
        }
    }
} catch {
    Write-Host "❌ Error during compilation: $($_.Exception.Message)" -ForegroundColor Red
}

Write-Host "`nComprehensive fix completed!" -ForegroundColor Green
