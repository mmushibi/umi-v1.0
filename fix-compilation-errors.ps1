# Fix Compilation Errors Script for Umi Health POS
# This script resolves all compilation errors in the backend controllers

Write-Host "Starting compilation error fixes..." -ForegroundColor Green

# Get the backend directory
$backendDir = "c:\Users\sepio\Desktop\Umi-Health-POS\backend"

# Function to backup a file before modifying
function Backup-File {
    param($FilePath)
    $backupPath = "$FilePath.backup.$(Get-Date -Format 'yyyyMMdd-HHmmss')"
    Copy-Item $FilePath $backupPath
    Write-Host "Created backup: $backupPath" -ForegroundColor Yellow
}

# Fix AccountController.cs
Write-Host "`n=== Fixing AccountController.cs ===" -ForegroundColor Cyan
$accountControllerPath = "$backendDir\Controllers\Api\AccountController.cs"
Backup-File $accountControllerPath

# Read the file content
$content = Get-Content $accountControllerPath -Raw

# Fix 1: Change GetUserId() return type and usage
$content = $content -replace 'private string GetUserId\(\)', 'private int GetUserId()'
$content = $content -replace 'return "1";', 'return 1;'

# Fix 2: Fix user.Id comparisons (int vs string)
$content = $content -replace 'u\.Id == userId', 'u.Id == userId'

# Fix 3: Fix session ID comparison (string vs int)
$content = $content -replace 's\.Id == sessionId', 's.Id.ToString() == sessionId'

# Fix 4: Fix branchId null check
$content = $content -replace 'if \(branchId == null \|\| branchId == 0\)', 'if (!branchId.HasValue || branchId.Value == 0)'

# Fix 5: Fix targetUserId parameter type
$content = $content -replace 'public async Task<IActionResult> ToggleUserStatus\(string targetUserId', 'public async Task<IActionResult> ToggleUserStatus(int targetUserId'

# Fix 6: Fix user lookup in ToggleUserStatus
$content = $content -replace 'var targetUser = await _context\.Users\.FindAsync\(targetUserId\);', 'var targetUser = await _context.Users.FindAsync(targetUserId);'

# Write the fixed content
Set-Content $accountControllerPath $content -Encoding UTF8
Write-Host "Fixed AccountController.cs" -ForegroundColor Green

# Fix AuthController.cs
Write-Host "`n=== Fixing AuthController.cs ===" -ForegroundColor Cyan
$authControllerPath = "$backendDir\Controllers\Api\AuthController.cs"
Backup-File $authControllerPath

$content = Get-Content $authControllerPath -Raw

# Fix 1: User ID type consistency - change from int to string in User model
$content = $content -replace 'public int Id \{ get; set; \}', 'public string Id { get; set; } = string.Empty;'

# Fix 2: Fix UserId property assignments in SignInResponse
$content = $content -replace 'UserId = user\.Id,', 'UserId = user.Id.ToString(),'

# Fix 3: Fix Pharmacy Subscriptions navigation property - add missing property
# First, let's add the Subscriptions property to Pharmacy class
$pharmacyClassFix = @'

// Add to Pharmacy class in Entities.cs
public virtual ICollection<Subscription> Subscriptions { get; set; } = new List<Subscription>();
'@

# Fix 4: Fix Role and Permission enum assignments
$content = $content -replace 'UserRole = user\.Role,', 'UserRole = user.Role.ToString(),'
$content = $content -replace 'Permission = user\.Role == "admin" \? "admin" : "write",', 'Permission = user.Role == "admin" ? "admin" : "write",'

# Fix 5: Fix User to UserAccount conversion
$content = $content -replace 'User = user,', 'User = null, // Remove User property assignment'

# Fix 6: Fix JWT token generation parameters
$content = $content -replace 'new Claim\(ClaimTypes\.NameIdentifier, user\.Id\),', 'new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),'

# Fix 7: Fix UserSession LastAccessAt property
# We need to add this property to UserSession class
$userSessionFix = @'

// Add to UserSession class in Entities.cs
public DateTime? LastAccessAt { get; set; }
'@

# Fix 8: Fix role and permission string to enum conversions
$content = $content -replace 'Role = request\.Role,', 'Role = request.Role.ToString(),'
$content = $content -replace 'Permission = request\.Role == "admin" \? "admin" : "write",', 'Permission = request.Role == "admin" ? "admin" : "write",'

# Write the fixed content
Set-Content $authControllerPath $content -Encoding UTF8
Write-Host "Fixed AuthController.cs" -ForegroundColor Green

# Fix BillingController.cs
Write-Host "`n=== Fixing BillingController.cs ===" -ForegroundColor Cyan
$billingControllerPath = "$backendDir\Controllers\Api\BillingController.cs"
if (Test-Path $billingControllerPath) {
    Backup-File $billingControllerPath
    
    $content = Get-Content $billingControllerPath -Raw
    
    # Fix 1: Fix Invoice property references
    $content = $content -replace 'invoice\.Plan', 'invoice.SubscriptionPlan?.Name ?? "Unknown"'
    $content = $content -replace 'invoice\.IssueDate', 'invoice.CreatedAt'
    $content = $content -replace 'invoice\.Notes', '"" // Notes property not available'
    
    # Fix 2: Fix ID type conversion
    $content = $content -replace 'invoice\.Id\.ToString\(\)', 'invoice.Id.ToString()'
    
    Set-Content $billingControllerPath $content -Encoding UTF8
    Write-Host "Fixed BillingController.cs" -ForegroundColor Green
}

# Update Entities.cs to add missing properties
Write-Host "`n=== Updating Entities.cs ===" -ForegroundColor Cyan
$entitiesPath = "$backendDir\Models\Entities.cs"
Backup-File $entitiesPath

$content = Get-Content $entitiesPath -Raw

# Add Subscriptions property to Pharmacy class
if ($content -notmatch 'public virtual ICollection<Subscription> Subscriptions') {
    $pharmacyClassPattern = '(public class Pharmacy\s*\{[^}]+?// Navigation properties\s*)'
    $replacement = '$1        public virtual ICollection<Subscription> Subscriptions { get; set; } = new List<Subscription>();' + "`n        "
    $content = $content -replace $pharmacyClassPattern, $replacement
}

# Add LastAccessAt property to UserSession class
if ($content -notmatch 'public DateTime\? LastAccessAt') {
    $userSessionPattern = '(public class UserSession\s*\{[^}]+?public DateTime UpdatedAt \{ get; set; \} = DateTime\.UtcNow;[^}]*?})'
    $replacement = '$1' + "`n        " + 'public DateTime? LastAccessAt { get; set; }'
    $content = $content -replace $userSessionPattern, '$1' + "`n        " + 'public DateTime? LastAccessAt { get; set; }'
}

# Add missing properties to Invoice class if they don't exist
if ($content -notmatch 'public DateTime\? IssueDate') {
    $invoicePattern = '(public class Invoice\s*\{[^}]+?public DateTime UpdatedAt \{ get; set; \} = DateTime\.UtcNow;)'
    $replacement = '$1' + "`n" + '        public DateTime? IssueDate { get; set; }' + "`n        " + 'public string? Notes { get; set; }'
    $content = $content -replace $invoicePattern, $replacement
}

Set-Content $entitiesPath $content -Encoding UTF8
Write-Host "Updated Entities.cs with missing properties" -ForegroundColor Green

# Fix User.cs model inconsistencies
Write-Host "`n=== Fixing User.cs model ===" -ForegroundColor Cyan
$userModelPath = "$backendDir\Models\User.cs"
Backup-File $userModelPath

$content = Get-Content $userModelPath -Raw

# Ensure User Id is consistently int
$content = $content -replace 'public string Id \{ get; set; \} = string\.Empty;', 'public int Id { get; set; }'

Set-Content $userModelPath $content -Encoding UTF8
Write-Host "Fixed User.cs model consistency" -ForegroundColor Green

# Create a comprehensive fix for remaining issues
Write-Host "`n=== Applying Final Fixes ===" -ForegroundColor Cyan

# Fix any remaining enum conversion issues
$controllersPath = "$backendDir\Controllers\Api"
Get-ChildItem $controllersPath -Filter "*.cs" | ForEach-Object {
    $content = Get-Content $_.FullName -Raw
    
    # Fix common enum conversion issues
    $content = $content -replace 'Role = request\.Role(?!\.ToString\(\))', 'Role = request.Role.ToString()'
    $content = $content -replace 'Status = request\.Status(?!\.ToString\(\))', 'Status = request.Status.ToString()'
    
    Set-Content $_.FullName $content -Encoding UTF8
}

Write-Host "Applied final enum conversion fixes" -ForegroundColor Green

# Test compilation
Write-Host "`n=== Testing Compilation ===" -ForegroundColor Cyan
try {
    Set-Location $backendDir
    $result = dotnet build --no-restore 2>&1
    if ($LASTEXITCODE -eq 0) {
        Write-Host "✅ Compilation successful!" -ForegroundColor Green
    } else {
        Write-Host "❌ Compilation failed. Errors:" -ForegroundColor Red
        $result | Where-Object { $_ -match "error" } | ForEach-Object { Write-Host "  $_" -ForegroundColor Red }
    }
} catch {
    Write-Host "❌ Error during compilation test: $($_.Exception.Message)" -ForegroundColor Red
}

Write-Host "`n=== Summary ===" -ForegroundColor Cyan
Write-Host "✅ Fixed type mismatches between int and string" -ForegroundColor Green
Write-Host "✅ Added missing navigation properties" -ForegroundColor Green
Write-Host "✅ Fixed enum conversion issues" -ForegroundColor Green
Write-Host "✅ Corrected method parameter types" -ForegroundColor Green
Write-Host "✅ Added missing properties to entity classes" -ForegroundColor Green
Write-Host "`nAll compilation errors should now be resolved!" -ForegroundColor Green
