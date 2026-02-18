# Database verification script for Umi Health POS
# This script checks if PostgreSQL is available and can connect

Write-Host "Testing PostgreSQL connection..."

# Try to find PostgreSQL
$psqlPaths = @(
    "C:\Program Files\PostgreSQL\16\bin\psql.exe",
    "C:\Program Files (x86)\PostgreSQL\16\bin\psql.exe",
    "psql.exe"
)

$psqlFound = $false
foreach ($path in $psqlPaths) {
    if (Test-Path $path) {
        Write-Host "Found PostgreSQL at: $path"
        $psqlFound = $true
        $psqlPath = $path
        break
    }
}

if (-not $psqlFound) {
    Write-Host "PostgreSQL not found. Please install PostgreSQL or add to PATH."
    exit 1
}

# Test connection
try {
    $result = & $psqlPath -h localhost -U umi_admin -d umi_health_pos -c "SELECT version();" -t
    if ($LASTEXITCODE -eq 0) {
        Write-Host "✅ Database connection successful!"
        Write-Host "PostgreSQL version: $result"
        
        # Check if tables exist
        $tables = & $psqlPath -h localhost -U umi_admin -d umi_health_pos -c "SELECT tablename FROM pg_tables WHERE schemaname = 'public';" -t
        Write-Host "Existing tables: $tables"
        
        # Check if our tables exist
        $ourTables = @("Tenants", "Products", "Customers", "Sales", "SaleItems", "InventoryItems")
        foreach ($table in $ourTables) {
            if ($tables -match $table) {
                Write-Host "✅ Table '$table' exists"
            } else {
                Write-Host "❌ Table '$table' missing"
            }
        }
    } else {
        Write-Host "❌ Database connection failed!"
        Write-Host "Please check:"
        Write-Host "1. PostgreSQL service is running"
        Write-Host "2. Database 'umi_health_pos' exists"
        Write-Host "3. User 'umi_admin' has correct permissions"
        Write-Host "4. Connection string in appsettings.json is correct"
    }
} catch {
    Write-Host "❌ Error: $_"
}
