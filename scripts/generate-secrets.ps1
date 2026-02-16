# Generate Production Secrets for Umi Health POS
# Run this script to generate secure values for your GitHub secrets

Write-Host "=== Umi Health POS - Production Secrets Generator ===" -ForegroundColor Green
Write-Host ""

# Generate JWT Secret Key
Write-Host "1. Generating JWT Secret Key..." -ForegroundColor Yellow
Add-Type -AssemblyName System.Web
$jwtKey = [System.Web.Security.Membership]::GeneratePassword(32, 4)
Write-Host "JWT_SECRET_KEY: $jwtKey" -ForegroundColor Cyan
Write-Host ""

# Generate Database Connection String Template
Write-Host "2. Database Connection String Template:" -ForegroundColor Yellow
Write-Host "Replace the placeholders with your actual database values:" -ForegroundColor Gray
Write-Host "DATABASE_CONNECTION_STRING: Host=your-postgres-host;Database=umi_db;Username=your-username;Password=your-password;Port=5432;SslMode=Require;" -ForegroundColor Cyan
Write-Host ""

# Generate Sentry DSN Template
Write-Host "3. Sentry DSN Template:" -ForegroundColor Yellow
Write-Host "Get your DSN from: https://sentry.io" -ForegroundColor Gray
Write-Host "SENTRY_DSN: https://your-public-key@your-org.ingest.sentry.io/your-project-id" -ForegroundColor Cyan
Write-Host ""

Write-Host "=== Next Steps ===" -ForegroundColor Green
Write-Host "1. Go to your GitHub repository"
Write-Host "2. Navigate to Settings → Secrets and variables → Actions"
Write-Host "3. Click 'New repository secret' for each of the following:"
Write-Host "   - DATABASE_CONNECTION_STRING"
Write-Host "   - JWT_SECRET_KEY"
Write-Host "   - SENTRY_DSN"
Write-Host ""
Write-Host "4. Copy and paste the values above (replace database placeholders)"
Write-Host ""
Write-Host "=== Security Notes ===" -ForegroundColor Red
Write-Host "- Never commit secrets to your repository"
Write-Host "- Use different secrets for staging and production"
Write-Host "- Rotate secrets regularly"
Write-Host "- Use strong, unique passwords for database"
