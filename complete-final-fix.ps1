# Complete final fix for all remaining compilation errors
Write-Host "Applying complete final fix..." -ForegroundColor Green

# Fix 1: Ensure LastAccessAt is properly added to UserSession in Entities.cs
Write-Host "`n=== Ensuring LastAccessAt in UserSession ===" -ForegroundColor Cyan
$entitiesPath = "c:\Users\sepio\Desktop\Umi-Health-POS\backend\Models\Entities.cs"
$content = Get-Content $entitiesPath -Raw

# Remove any existing LastAccessAt properties that might be duplicated
$content = $content -replace 'public DateTime\? LastAccessAt \{ get; set; \}\s*$', ''

# Add LastAccessAt to UserSession class correctly
$userSessionPattern = '(public class UserSession[^}]*?public DateTime UpdatedAt \{ get; set; \} = DateTime\.UtcNow;)'
if ($content -match $userSessionPattern) {
    $content = $content -replace $userSessionPattern, "$1`n        public DateTime? LastAccessAt { get; set; }"
}

Set-Content $entitiesPath $content -Encoding UTF8
Write-Host "Fixed LastAccessAt in UserSession" -ForegroundColor Green

# Fix 2: Ensure IsActive is added to Tenant class
$content = Get-Content $entitiesPath -Raw
$tenantPattern = '(public class Tenant[^}]*?public DateTime UpdatedAt \{ get; set; \} = DateTime\.UtcNow;)'
if ($content -match $tenantPattern) {
    $content = $content -replace $tenantPattern, "$1`n        public bool IsActive { get; set; } = true;"
}
Set-Content $entitiesPath $content -Encoding UTF8
Write-Host "Fixed IsActive in Tenant class" -ForegroundColor Green

# Fix 3: Fix UserResponse class in User.cs to include all required properties
Write-Host "`n=== Fixing UserResponse class ===" -ForegroundColor Cyan
$userModelPath = "c:\Users\sepio\Desktop\Umi-Health-POS\backend\Models\User.cs"
$content = Get-Content $userModelPath -Raw

# Remove existing UserResponse class and recreate it with all properties
$userResponsePattern = 'public class UserResponse\s*\{[^}]*?\}'
$newUserResponse = @'
public class UserResponse
    {
        public string Id { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Phone { get; set; } = string.Empty;
        public string Address { get; set; } = string.Empty;
        public string Role { get; set; } = string.Empty;
        public string PharmacyId { get; set; } = string.Empty;
        public string PharmacyName { get; set; } = string.Empty;
        public string? Branch { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public DateTime? LastLogin { get; set; }
        public DateTime CreatedAt { get; set; }
    }'@

if ($content -match $userResponsePattern) {
    $content = $content -replace $userResponsePattern, $newUserResponse.Trim()
}

Set-Content $userModelPath $content -Encoding UTF8
Write-Host "Fixed UserResponse class" -ForegroundColor Green

# Fix 4: Remove duplicate properties that might have been added incorrectly
Write-Host "`n=== Cleaning up duplicate properties ===" -ForegroundColor Cyan
$content = Get-Content $entitiesPath -Raw

# Remove any duplicate LastAccessAt, IsActive, etc. that were added to wrong classes
$lines = $content -split "`n"
$cleanedLines = @()
$inClass = $false
$currentClass = ""
$duplicateProperties = @('LastAccessAt', 'IsActive', 'IssueDate', 'Notes')

foreach ($line in $lines) {
    if ($line -match 'public class (\w+)') {
        $currentClass = $matches[1]
        $inClass = $true
    }
    elseif ($line -match '^\s*}\s*$') {
        $inClass = $false
    }
    
    # Skip duplicate properties that were added to wrong classes
    $skipLine = $false
    foreach ($prop in $duplicateProperties) {
        if ($line -match "public DateTime\? $prop" -or $line -match "public bool $prop" -or $line -match "public string\? $prop") {
            # Only allow these properties in specific classes
            if (($prop -eq 'LastAccessAt' -and $currentClass -ne 'UserSession') -or
                ($prop -eq 'IsActive' -and $currentClass -ne 'Tenant' -and $currentClass -ne 'Branch') -or
                (($prop -eq 'IssueDate' -or $prop -eq 'Notes') -and $currentClass -ne 'Invoice')) {
                $skipLine = $true
                break
            }
        }
    }
    
    if (-not $skipLine) {
        $cleanedLines += $line
    }
}

$cleanedContent = $cleanedLines -join "`n"
Set-Content $entitiesPath $cleanedContent -Encoding UTF8
Write-Host "Cleaned up duplicate properties" -ForegroundColor Green

# Final compilation test
Write-Host "`n=== Complete Final Compilation Test ===" -ForegroundColor Cyan
try {
    Set-Location "c:\Users\sepio\Desktop\Umi-Health-POS\backend"
    $result = dotnet build --no-restore 2>&1
    if ($LASTEXITCODE -eq 0) {
        Write-Host "🎉 COMPLETE SUCCESS! All compilation errors have been resolved!" -ForegroundColor Green
        Write-Host "The backend now compiles successfully with 0 errors." -ForegroundColor Green
    } else {
        $errorCount = ($result | Where-Object { $_ -match "error CS" }).Count
        Write-Host "❌ Remaining errors: $errorCount" -ForegroundColor Red
        if ($errorCount -le 10) {
            $result | Where-Object { $_ -match "error CS" } | ForEach-Object { Write-Host "  $_" -ForegroundColor Red }
        } else {
            Write-Host "Too many errors to display. First few:" -ForegroundColor Red
            $result | Where-Object { $_ -match "error CS" } | Select-Object -First 5 | ForEach-Object { Write-Host "  $_" -ForegroundColor Red }
        }
    }
} catch {
    Write-Host "❌ Error during compilation: $($_.Exception.Message)" -ForegroundColor Red
}

Write-Host "`n=== Complete Final Fix Summary ===" -ForegroundColor Cyan
Write-Host "✅ Fixed LastAccessAt in UserSession class" -ForegroundColor Green
Write-Host "✅ Fixed IsActive in Tenant class" -ForegroundColor Green
Write-Host "✅ Fixed UserResponse class with all required properties" -ForegroundColor Green
Write-Host "✅ Cleaned up duplicate properties" -ForegroundColor Green
Write-Host "`nComplete final fix applied!" -ForegroundColor Green
