# Umi Health POS - Docker Setup

This directory contains Docker configuration files for the Umi Health POS application.

## Quick Start

1. **Prerequisites**
   - Docker Desktop installed and running
   - Git (to clone the repository)

2. **Running the Application**

   ```bash
   # Clone and navigate to the project
   git clone <repository-url>
   cd Umi-Health-POS
   
   # Start all services
   docker-compose up -d
   
   # View logs
   docker-compose logs -f
   ```

3. **Access the Application**
   - Frontend: http://localhost:80
   - Backend API: http://localhost:5000
   - PostgreSQL: localhost:5432

## Services

### PostgreSQL Database
- **Container**: `umi-postgres`
- **Database**: `umi_health_pos`
- **Username**: `umi_admin`
- **Password**: `umi_secure_password_2024`
- **Port**: 5432

### Backend API
- **Container**: `umi-backend-api`
- **Framework**: .NET 8.0
- **Port**: 5000 (HTTP), 5001 (HTTPS)
- **Environment**: Production

### Frontend
- **Container**: `umi-frontend`
- **Server**: Nginx
- **Port**: 80 (HTTP), 443 (HTTPS)

## Development

### Building Images

```bash
# Build backend image
docker build -t umi-backend ./backend

# Build with docker-compose
docker-compose build
```

### Running Services

```bash
# Start all services
docker-compose up -d

# Start specific service
docker-compose up -d postgres

# Stop all services
docker-compose down

# Stop and remove volumes
docker-compose down -v
```

### Database Management

```bash
# Connect to PostgreSQL
docker exec -it umi-postgres psql -U umi_admin -d umi_health_pos

# View database logs
docker-compose logs postgres
```

### Environment Variables

The backend uses the following environment variables:

```yaml
ASPNETCORE_ENVIRONMENT: Production
ConnectionStrings__DefaultConnection: Host=postgres;Database=umi_health_pos;Username=umi_admin;Password=umi_secure_password_2024
Jwt__Issuer: UmiHealthPOS
Jwt__Audience: UmiHealthPOSUsers
Jwt__Key: ThisIsASecretKeyForJWTTokenGenerationThatShouldBeAtLeast32CharactersLong!
Frontend__AllowedOrigins: http://localhost:3000,http://localhost:8080
```

## Production Deployment

For production deployment:

1. **Update Environment Variables**
   - Change database passwords
   - Update JWT keys
   - Configure proper CORS origins

2. **SSL Configuration**
   - Add SSL certificates to nginx
   - Update HTTPS configuration

3. **Security**
   - Remove debug endpoints
   - Configure proper firewall rules
   - Set up proper logging

## Troubleshooting

### Common Issues

1. **Port Conflicts**
   ```bash
   # Check what's using ports
   netstat -tulpn | grep :80
   netstat -tulpn | grep :5432
   ```

2. **Database Connection Issues**
   ```bash
   # Check database logs
   docker-compose logs postgres
   
   # Test connection
   docker exec -it umi-postgres psql -U umi_admin -d umi_health_pos
   ```

3. **Backend Build Issues**
   ```bash
   # Rebuild backend image
   docker-compose build --no-cache backend
   
   # View build logs
   docker-compose logs backend
   ```

### Logs

```bash
# View all logs
docker-compose logs

# Follow logs for specific service
docker-compose logs -f backend

# View last 100 lines
docker-compose logs --tail=100
```

## File Structure

```
Umi-Health-POS/
├── backend/
│   ├── Dockerfile          # Backend container configuration
│   ├── .dockerignore       # Files to exclude from Docker context
│   └── ...                 # Backend source code
├── wwwroot/                # Frontend static files
├── docker-compose.yml      # Multi-container orchestration
├── nginx.conf             # Nginx configuration (if needed)
└── README.md              # This file
```

## Support

For issues related to:
- **Docker setup**: Check the troubleshooting section above
- **Application functionality**: Refer to the main project documentation
- **Database issues**: Check PostgreSQL logs and connection strings
