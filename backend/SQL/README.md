# Umi Health POS Database SQL Files

This directory contains comprehensive SQL files for setting up and managing the Umi Health POS database. All files are designed to work with PostgreSQL database named `umi_db`.

## File Structure

### 1. **01_Create_Database_Schema.sql**
- **Purpose**: Complete database schema with all tables for all portals
- **Contains**: Core tables, indexes, triggers, constraints, views, and initial system settings
- **Usage**: Run this file first to create the complete database structure
- **Tables Included**:
  - Products, Customers, Sales, SaleItems, StockTransactions
  - InventoryItems, Prescriptions, Patients, PrescriptionItems
  - Users, Tenants, DashboardSettings, AuditLogs, SystemSettings
  - Notifications, and all supporting tables

### 2. **02_Tenant_Admin_Portal.sql**
- **Purpose**: Tables and functionality specific to Tenant Admin portal
- **Contains**: Multi-tenant management, user administration, reporting, audit logging
- **Key Features**:
  - Tenant management with subscription tracking
  - User role and permission management
  - Sales and inventory reporting
  - Audit logging and backup tracking
  - Dashboard analytics

### 3. **03_Pharmacist_Portal.sql**
- **Purpose**: Tables and functionality specific to Pharmacist portal
- **Contains**: Patient management, prescription tracking, clinical data
- **Key Features**:
  - Patient records with medical history and allergies
  - Prescription management with status tracking
  - Drug interaction checking
  - Regulatory compliance for Zambia
  - Clinical documentation

### 4. **04_Cashier_Portal.sql**
- **Purpose**: Tables and functionality specific to Cashier portal
- **Contains**: Sales processing, payment handling, cash management
- **Key Features**:
  - Point of sale transactions
  - Multiple payment methods (Cash, Card, Mobile Money)
  - Cash drawer and shift management
  - Customer loyalty programs
  - Refunds and returns processing

### 5. **05_Sales_Operations_Portal.sql**
- **Purpose**: Tables and functionality specific to Sales Operations portal
- **Contains**: Sales analytics, subscription management, performance tracking
- **Key Features**:
  - Sales analytics and reporting
  - Subscription plan management
  - Sales targets and commission tracking
  - Territory management
  - Performance metrics

### 6. **06_Super_Admin_Portal.sql**
- **Purpose**: Tables and functionality specific to Super Admin portal
- **Contains**: System administration, multi-tenant management, monitoring
- **Key Features**:
  - System configuration and monitoring
  - Multi-tenant administration
  - User management and security
  - System health monitoring
  - Backup and disaster recovery
  - Compliance and audit management

### 7. **07_Seed_Data.sql**
- **Purpose**: Initial seed data for testing and demonstration
- **Contains**: Sample data for all major tables
- **Key Features**:
  - Super admin and tenant admin users
  - Sample products and inventory items
  - Customer and patient records
  - Sample prescriptions and sales
  - System configuration settings
  - Promotions and loyalty programs

## Database Configuration

### Connection String
```
Host=localhost;Database=umi_db;Username=postgres;Password=password
```

### Environment-Specific Files
- **appsettings.json**: Main configuration with umi_db database
- **appsettings.Development.json**: Development settings with umi_db_dev database
- **appsettings.Production.json**: Production settings with security placeholders

## Key Features

### Zambia-Specific Compliance
- Zambia Registration Numbers for pharmaceutical products
- License number tracking
- Regulatory compliance tables
- Controlled substances logging
- Local currency support (ZMW)

### Multi-Tenant Architecture
- Complete tenant isolation
- Subscription-based access control
- Resource usage tracking
- Tenant-specific configurations

### Role-Based Access Control
- Super Admin: Full system access
- Tenant Admin: Tenant management
- Pharmacist: Clinical and prescription management
- Cashier: Sales and customer service

### Comprehensive Audit Trail
- User activity logging
- Data change tracking
- System event logging
- Compliance reporting

### Performance Optimization
- Strategic indexes on all tables
- Optimized queries in views
- Efficient foreign key relationships
- Proper data types and constraints

## Installation Instructions

### 1. Create Database
```sql
CREATE DATABASE umi_db;
```

### 2. Run Schema Files in Order
```bash
# 1. Create complete database schema
psql -h localhost -U postgres -d umi_db -f 01_Create_Database_Schema.sql

# 2. (Optional) Run portal-specific files if you want to separate concerns
psql -h localhost -U postgres -d umi_db -f 02_Tenant_Admin_Portal.sql
psql -h localhost -U postgres -d umi_db -f 03_Pharmacist_Portal.sql
psql -h localhost -U postgres -d umi_db -f 04_Cashier_Portal.sql
psql -h localhost -U postgres -d umi_db -f 05_Sales_Operations_Portal.sql
psql -h localhost -U postgres -d umi_db -f 06_Super_Admin_Portal.sql

# 3. Insert seed data for testing
psql -h localhost -U postgres -d umi_db -f 07_Seed_Data.sql
```

### 3. Verify Installation
```sql
-- Check table count
SELECT COUNT(*) FROM information_schema.tables WHERE table_schema = 'public';

-- Verify seed data
SELECT COUNT(*) FROM Users;
SELECT COUNT(*) FROM Tenants;
SELECT COUNT(*) FROM Products;
SELECT COUNT(*) FROM InventoryItems;
```

## Default Users

### Super Admin
- **Username**: superadmin
- **Password**: admin123
- **Role**: SuperAdmin

### Tenant Admin
- **Username**: tenantadmin
- **Password**: admin123
- **Role**: TenantAdmin
- **Tenant**: Umi Health Pharmacy Ltd

## Security Notes

1. **Change Default Passwords**: Immediately change default passwords after installation
2. **Environment Variables**: Use environment variables for sensitive configuration
3. **Database Security**: Configure proper database user permissions
4. **Backup Strategy**: Implement regular database backups
5. **Audit Monitoring**: Monitor audit logs for suspicious activity

## Maintenance

### Regular Tasks
1. **Backup**: Daily automated backups
2. **Archive**: Archive old transaction data based on retention policies
3. **Monitor**: Check system health and performance metrics
4. **Update**: Apply security updates and patches
5. **Review**: Review audit logs and user access

### Performance Tuning
1. **Index Analysis**: Regularly analyze and optimize indexes
2. **Query Optimization**: Monitor slow queries
3. **Statistics**: Update table statistics for query planner
4. **Vacuum**: Regular vacuum and analyze operations

## Support

For technical support or questions about the database schema:
- Check the Entity Framework models in `backend/Models/Entities.cs`
- Review the application context in `backend/Data/ApplicationDbContext.cs`
- Consult the API controllers in `backend/Controllers/Api/`

## Version History

- **v1.0.0**: Initial release with complete multi-tenant POS system
- Supports all portal functionalities
- Zambia-specific compliance features
- Comprehensive audit and security features
