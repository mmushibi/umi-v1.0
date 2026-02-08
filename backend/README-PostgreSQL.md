# Umi Health POS - PostgreSQL Database Setup

## Overview
This project uses PostgreSQL as the primary database for the Umi Health Point of Sale system.

## Prerequisites
- PostgreSQL 14 or later
- .NET 8.0 SDK
- Visual Studio 2022 or VS Code

## Database Setup

### 1. Install PostgreSQL
```bash
# Windows (using Chocolatey)
choco install postgresql

# macOS (using Homebrew)
brew install postgresql

# Ubuntu/Debian
sudo apt-get install postgresql postgresql-contrib
```

### 2. Start PostgreSQL Service
```bash
# Windows
Start-Service postgresql-x64-14

# macOS
brew services start postgresql

# Linux
sudo systemctl start postgresql
```

### 3. Create Database
```bash
# Connect to PostgreSQL
psql -U postgres

# Create database
CREATE DATABASE umihealth;

# Create user (optional)
CREATE USER umi_user WITH PASSWORD 'your_secure_password';

# Grant permissions
GRANT ALL PRIVILEGES ON DATABASE umihealth TO umi_user;
```

### 4. Update Connection String
Update `appsettings.json` with your PostgreSQL connection details:

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Host=localhost;Database=umihealth;Username=umi_user;Password=your_secure_password"
  }
}
```

## Running the Application

### 1. Restore Dependencies
```bash
cd backend
dotnet restore
```

### 2. Run Database Migrations
```bash
dotnet ef database update
```

### 3. Run the Application
```bash
dotnet run
```

## Database Schema
The database schema is automatically created by Entity Framework Core. Key tables:

- **Products** - Inventory items with stock tracking
- **Customers** - Customer information
- **Sales** - Sales transactions
- **SaleItems** - Individual sale line items
- **StockTransactions** - Stock movement history

## Environment Variables
You can also use environment variables instead of `appsettings.json`:

```bash
# Windows
set ConnectionStrings__DefaultConnection="Host=localhost;Database=umihealth;Username=umi_user;Password=your_secure_password"

# Linux/macOS
export ConnectionStrings__DefaultConnection="Host=localhost;Database=umihealth;Username=umi_user;Password=your_secure_password"
```

## Docker Setup (Optional)
If you prefer using Docker:

```dockerfile
FROM mcr.microsoft.com/dotnet/aspnet:8.0
WORKDIR /app
COPY . .
RUN dotnet restore
RUN dotnet ef database update
EXPOSE 80
ENTRYPOINT ["dotnet", "run"]
```

```bash
# Build and run with Docker
docker build -t umi-health-pos .
docker run -e ConnectionStrings__DefaultConnection="Host=host.docker.internal;Database=umihealth;Username=umi_user;Password=your_secure_password" -p 5432:80 umi-health-pos
```

## Testing the Connection
To verify the database connection:

1. Start the application
2. Open browser to `http://localhost:5000/swagger`
3. Test the API endpoints
4. Check logs for any database connection issues

## Troubleshooting

### Common Issues:
- **Connection refused**: Ensure PostgreSQL is running
- **Authentication failed**: Verify username/password in connection string
- **Database doesn't exist**: Run the schema creation script
- **Port conflicts**: Ensure port 5432 is available

### Connection String Format:
```
Host=hostname;Database=database_name;Username=username;Password=password;Port=5432
```

## Production Considerations
- Use strong passwords
- Enable SSL connections in production
- Set up regular database backups
- Configure connection pooling for performance
- Monitor database performance and logs
