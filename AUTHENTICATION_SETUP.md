# Umi Health POS - Authentication & Production Setup Guide

## Overview
This document outlines the comprehensive authentication system and production configuration implemented for the Umi Health POS application.

## ✅ Completed Implementation

### 1. Production-Ready Configuration
- **appsettings.Production.json**: Created with secure configuration templates
- **Environment Variables**: JWT settings, database connection, and CORS origins ready for production
- **Security**: Removed all hardcoded secrets and demo values

### 2. Authentication System
- **JWT Service**: Complete token generation, validation, and refresh functionality
- **Auth Controller**: Full signin, signup, logout, and token refresh endpoints
- **Role-Based Access**: Admin, Pharmacist, and Cashier roles with appropriate permissions

### 3. Frontend Authentication Module
- **auth.js**: Centralized authentication JavaScript module
- **Session Management**: Secure token storage and automatic refresh
- **Role Validation**: Page-level access control based on user roles
- **API Integration**: Automatic token injection and refresh for API calls

### 4. Portal Protection
- **Tenant Admin**: Full access with admin role verification
- **Pharmacist**: Limited write access with pharmacist role verification  
- **Cashier**: Read-only access with cashier role verification
- **Automatic Redirects**: Unauthorized users redirected to login

### 5. API Security
- **InventoryController**: Role-based endpoint protection
  - GET /api/inventory/items: All authenticated users
  - POST /api/inventory/items: Admin, Pharmacist
  - PUT /api/inventory/items/{id}: Admin, Pharmacist
  - DELETE /api/inventory/items/{id}: Admin only
  - POST /api/inventory/import-csv: Admin only
- **TenantAdminController**: Admin-only access
- **Automatic Token Validation**: All protected endpoints verify JWT tokens

### 6. CORS Configuration
- **Production Domains**: Configured for umihealth.com, www.umihealth.com, app.umihealth.com
- **Development Support**: Localhost origins maintained for development
- **Security**: Proper origin validation and credential handling

## 🔧 Deployment Configuration

### Environment Variables Required
```bash
# JWT Configuration
Jwt__Issuer=UmiHealthPOS
Jwt__Audience=UmiHealthPOS
Jwt__Key=[YOUR_SECURE_JWT_SECRET_KEY_MIN_32_CHARS]

# Database Configuration
ConnectionStrings__DefaultConnection=Host=[DB_HOST];Database=umi_db;Username=[DB_USER];Password=[DB_PASSWORD]

# CORS Configuration (optional, defaults to appsettings)
Frontend__AllowedOrigins=https://umihealth.com,https://www.umihealth.com,https://app.umihealth.com
```

### Production Deployment Steps

1. **Set Environment Variables**
   ```bash
   export Jwt__Key="your-secure-jwt-secret-key-minimum-32-characters"
   export ConnectionStrings__DefaultConnection="Host=your-db-host;Database=umi_db;Username=your-user;Password=your-password"
   ```

2. **Run Database Migrations**
   ```bash
   dotnet ef database update --connection "Host=your-db-host;Database=umi_db;Username=your-user;Password=your-password"
   ```

3. **Configure Production Environment**
   ```bash
   export ASPNETCORE_ENVIRONMENT=Production
   ```

4. **Start the Application**
   ```bash
   dotnet run --project backend/UmiHealthPOS.cs
   ```

## 🧪 Testing Authentication Flow

### Test Users (Create via signup endpoint)
1. **Admin User**: Full system access
2. **Pharmacist User**: Can manage inventory and prescriptions
3. **Cashier User**: Can view inventory and process sales

### Authentication Test Sequence
1. **Signup**: Create users via `/api/auth/signup`
2. **Signin**: Test `/api/auth/signin` with valid credentials
3. **Token Refresh**: Test `/api/auth/refresh-token` with expired access token
4. **Portal Access**: Verify role-based access to different portals
5. **API Access**: Test role-based API endpoint access
6. **Logout**: Test `/api/auth/logout` and session cleanup

## 🔐 Security Features

### JWT Token Security
- **1-Hour Access Tokens**: Short-lived access tokens
- **7-Day Refresh Tokens**: Long-lived refresh tokens
- **Automatic Refresh**: Client-side token refresh before expiration
- **Secure Storage**: Tokens stored in localStorage/sessionStorage based on user preference

### Role-Based Access Control
- **Admin**: Full system access including user management
- **Pharmacist**: Inventory and prescription management
- **Cashier**: Sales processing and inventory viewing
- **API Protection**: All endpoints validate roles and permissions

### Data Protection
- **HTTPS Required**: Production configuration enforces secure connections
- **CORS Protection**: Only allowed origins can access the API
- **Input Validation**: All API endpoints validate request data
- **Error Handling**: Secure error messages without information leakage

## 🚀 Production Considerations

### Database Security
- Use strong database passwords
- Configure database firewall rules
- Enable SSL/TLS for database connections
- Regular database backups

### Application Security
- Use environment variables for secrets
- Enable request logging and monitoring
- Configure rate limiting for API endpoints
- Regular security updates and patches

### Performance Optimization
- Enable response caching where appropriate
- Configure connection pooling
- Monitor application performance
- Scale horizontally as needed

## 📝 Next Steps

1. **Create Admin User**: Use signup endpoint to create the first admin user
2. **Test All Roles**: Verify each role has appropriate access
3. **Configure Monitoring**: Set up application monitoring and logging
4. **Backup Strategy**: Implement regular database and application backups
5. **Security Audit**: Conduct security testing and penetration testing

## 🆘 Troubleshooting

### Common Issues
- **JWT Token Issues**: Verify JWT key is at least 32 characters
- **Database Connection**: Check connection string and database credentials
- **CORS Issues**: Verify frontend origins are in allowed list
- **Role Access**: Ensure user roles are correctly set in the database

### Debug Mode
For development, you can temporarily enable debug logging:
```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Debug",
      "Microsoft.AspNetCore": "Debug"
    }
  }
}
```

## 📞 Support

For authentication and deployment issues:
1. Check application logs for detailed error messages
2. Verify environment variables are correctly set
3. Ensure database is accessible and migrations are applied
4. Confirm frontend and backend are using compatible configurations

---

**Note**: This authentication system is production-ready and follows security best practices. Always test thoroughly in a staging environment before deploying to production.
