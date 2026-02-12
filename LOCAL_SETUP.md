# Umi Health POS - Local Development Setup

This guide will help you set up and run the Umi Health POS application on your local machine for development purposes.

## Prerequisites

Before you begin, ensure you have the following software installed:

### Required Software
- **Docker Desktop** - For containerized database and services
  - Download from: https://www.docker.com/products/docker-desktop
- **Git** - For version control
  - Download from: https://git-scm.com/download/win

### Recommended for Development
- **.NET 8 SDK** - For backend development
  - Download from: https://dotnet.microsoft.com/download/dotnet/8.0
- **Node.js** (v18 or higher) - For frontend development
  - Download from: https://nodejs.org/
- **Visual Studio 2022** or **VS Code** - For code editing
  - VS Code: https://code.visualstudio.com/

## Quick Start

### Option 1: Using the Startup Scripts (Recommended)

The easiest way to start the application is using one of the provided startup scripts:

#### PowerShell Script (Windows PowerShell)
```powershell
# Run from the project root directory
.\start-local.ps1

# Or with options
.\start-local.ps1 -Rebuild  # Rebuild containers before starting
.\start-local.ps1 -Help     # Show all available options
```

#### Batch Script (Windows CMD)
```batch
# Run from the project root directory
start-local.bat
```

### Option 2: Manual Docker Compose

```bash
# Start all services
docker-compose up -d

# View logs
docker-compose logs -f

# Stop all services
docker-compose down
```

## Application Architecture

The Umi Health POS application consists of three main services:

### 🗄️ PostgreSQL Database (Port 5432)
- **Container**: `umi-postgres`
- **Database**: `umi_health_pos`
- **Username**: `umi_admin`
- **Password**: `umi_secure_password_2024`
- **Purpose**: Stores all application data including inventory, users, and transactions

### 🔧 .NET Backend API (Port 5000)
- **Container**: `umi-backend-api`
- **Framework**: ASP.NET Core 8
- **Purpose**: RESTful API for all business logic and data operations
- **Features**: JWT Authentication, Entity Framework Core, SignalR for real-time updates

### 🌐 Frontend Web Application (Port 80)
- **Container**: `umi-frontend`
- **Technology**: Static HTML/CSS/JavaScript with Tailwind CSS
- **Purpose**: User interface for all application modules
- **Features**: Responsive design, multi-portal system (Tenant Admin, Pharmacist, Cashier)

## Access URLs

Once the application is running, you can access:

- **Frontend Application**: http://localhost
- **Backend API**: http://localhost:5000
- **Database**: localhost:5432 (for direct database connections)

## Application Modules

The application provides three user portals:

### 1. Tenant Admin Portal
- **URL**: http://localhost/modules/Tenant-Admin/
- **Access**: Full inventory management capabilities
- **Features**: Add/edit/delete products, CSV import/export, user management

### 2. Pharmacist Portal  
- **URL**: http://localhost/modules/Pharmacist/
- **Access**: Limited write access
- **Features**: View inventory, add/edit products, export data

### 3. Cashier Portal
- **URL**: http://localhost/modules/Cashier/
- **Access**: Read-only access
- **Features**: View inventory, search products, check stock levels

## Development Workflow

### 1. Making Changes to Frontend
```bash
# Build CSS with Tailwind (watch mode)
npm run build-css

# Build CSS for production
npm run build-css-prod

# Restart frontend container to see changes
docker-compose restart frontend
```

### 2. Making Changes to Backend
```bash
# Rebuild and restart backend container
docker-compose up -d --build backend

# View backend logs
docker-compose logs -f backend
```

### 3. Database Operations
```bash
# Access database container
docker exec -it umi-postgres psql -U umi_admin -d umi_health_pos

# View database logs
docker-compose logs -f postgres
```

## Useful Commands

### Docker Compose Commands
```bash
# Start all services
docker-compose up -d

# Start specific service
docker-compose up -d postgres

# Stop all services
docker-compose down

# View running containers
docker-compose ps

# View logs for all services
docker-compose logs -f

# View logs for specific service
docker-compose logs -f backend

# Rebuild all containers
docker-compose up -d --build

# Remove all containers and volumes
docker-compose down -v
```

### Database Commands
```bash
# Connect to PostgreSQL database
docker exec -it umi-postgres psql -U umi_admin -d umi_health_pos

# Backup database
docker exec umi-postgres pg_dump -U umi_admin umi_health_pos > backup.sql

# Restore database
docker exec -i umi-postgres psql -U umi_admin umi_health_pos < backup.sql
```

## Troubleshooting

### Common Issues

#### 1. Port Already in Use
If you see "port already in use" errors:
```bash
# Check what's using the port
netstat -ano | findstr :5432  # PostgreSQL
netstat -ano | findstr :5000  # Backend
netstat -ano | findstr :80    # Frontend

# Stop conflicting services or use different ports
```

#### 2. Docker Issues
If Docker commands fail:
- Ensure Docker Desktop is running
- Restart Docker Desktop
- Check Docker logs for errors

#### 3. Backend Startup Issues
If the backend fails to start:
```bash
# Check backend logs
docker-compose logs backend

# Common issues:
# - Database connection failed (check PostgreSQL is running)
# - Missing dependencies (rebuild with --build flag)
# - Configuration errors (check appsettings.json)
```

#### 4. Frontend Not Loading
If the frontend doesn't load:
```bash
# Check frontend container is running
docker-compose ps

# Check nginx logs
docker-compose logs frontend

# Ensure CSS is built
npm run build-css-prod
```

### Performance Tips

#### 1. Development Mode
For better development experience:
```bash
# Run backend locally instead of in Docker
cd backend
dotnet run

# Run frontend with live reload
# (Set up a local web server like Live Server extension in VS Code)
```

#### 2. Resource Usage
To reduce resource usage:
```bash
# Stop unused services
docker-compose stop postgres  # If using local database
docker-compose stop frontend   # If developing backend only
```

## Configuration

### Environment Variables
Key configuration options in `docker-compose.yml`:

```yaml
# Database Configuration
POSTGRES_DB: umi_health_pos
POSTGRES_USER: umi_admin
POSTGRES_PASSWORD: umi_secure_password_2024

# Backend Configuration
ASPNETCORE_ENVIRONMENT: Production
ConnectionStrings__DefaultConnection: Host=postgres;Database=umi_health_pos;Username=umi_admin;Password=umi_secure_password_2024

# JWT Configuration
Jwt__Issuer: UmiHealthPOS
Jwt__Audience: UmiHealthPOSUsers
Jwt__Key: [Your-Secret-Key]
```

### Custom Configuration
To modify configuration:
1. Edit `docker-compose.yml` for container settings
2. Edit `backend/appsettings.json` for application settings
3. Restart containers after changes

## Security Notes

### Development Environment
- Default passwords are for development only
- JWT key should be changed in production
- Database should be secured in production

### Production Deployment
- Use environment-specific configuration
- Change all default passwords
- Enable HTTPS
- Use proper secrets management
- Set up proper CORS policies

## Getting Help

If you encounter issues:

1. **Check the logs**: `docker-compose logs -f [service-name]`
2. **Verify prerequisites**: Ensure all required software is installed
3. **Check port availability**: Ensure ports 5432, 5000, and 80 are available
4. **Restart services**: `docker-compose down && docker-compose up -d`
5. **Rebuild containers**: `docker-compose up -d --build`

For additional help:
- Check the project documentation
- Review the Docker Compose configuration
- Examine the application logs for specific error messages

## Next Steps

Once the application is running:

1. **Explore the frontend**: Navigate to http://localhost
2. **Test the APIs**: Visit http://localhost:5000
3. **Examine the database**: Connect to PostgreSQL and explore the schema
4. **Start development**: Begin making changes to the codebase
5. **Review the documentation**: Read through other project documentation files

Happy coding! 🚀
