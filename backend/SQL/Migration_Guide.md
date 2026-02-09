# Umi Health POS - PostgreSQL Migration Guide

## Database Migration Instructions

### **Prerequisites**
- PostgreSQL server installed and running
- Database user with root privileges
- Database name: `umi_db`

### **Migration Execution**

#### **Option 1: Direct Migration (Recommended)**

```bash
# 1. Connect to PostgreSQL as root user
psql -U root -d postgres

# 2. Create database if it doesn't exist
CREATE DATABASE umi_db;

# 3. Connect to the umi_db database
\c umi_db

# 4. Execute the migration script
psql -U root -d umi_db -f /path/to/SQL/14_Migration_PostgreSQL.sql

# 5. Verify migration completion
SELECT 'Migration completed successfully' as status;
```

#### **Option 2: Step-by-Step Migration**

```bash
# 1. Create database
psql -U root -d postgres -c "CREATE DATABASE umi_db;"

# 2. Execute core schema
psql -U root -d umi_db -f /path/to/SQL/01_Create_Database_Schema.sql

# 3. Execute authentication system
psql -U root -d umi_db -f /path/to/SQL/08_Authentication_Account_Flow.sql

# 4. Execute role-based access control
psql -U root -d umi_db -f /path/to/SQL/13_Role_Based_Access_Control.sql

# 5. Execute remaining modules in order
psql -U root -d umi_db -f /path/to/SQL/09_Supplier_Management.sql
psql -U root -d umi_db -f /path/to/SQL/10_Daybook_Shift_Management.sql
psql -U root -d umi_db -f /path/to/SQL/11_Branch_Management.sql
psql -U root -d umi_db -f /path/to/SQL/12_Help_Training.sql

# 6. Execute seed data
psql -U root -d umi_db -f /path/to/SQL/07_Seed_Data.sql
```

### **Connection String Configuration**

Update your `appsettings.json`:

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Host=localhost;Database=umi_db;Username=postgres;Password=your_root_password"
  }
}
```

### **Migration Verification**

```sql
-- Verify tables were created
SELECT 
    schemaname,
    tablename,
    tableowner
FROM pg_tables 
WHERE schemaname = 'public'
ORDER BY tablename;

-- Verify row counts
SELECT 
    'useraccounts' as table_name,
    COUNT(*) as row_count
FROM useraccounts
UNION ALL
SELECT 
    'tenants' as table_name,
    COUNT(*) as row_count
FROM tenants
UNION ALL
SELECT 
    'products' as table_name,
    COUNT(*) as row_count
FROM products
UNION ALL
SELECT 
    'inventoryitems' as table_name,
    COUNT(*) as row_count
FROM inventoryitems;
```

### **Default Users After Migration**

The migration creates these default users:

| Username | Password | Role | Description |
|----------|----------|------|-------------|
| superadmin | admin123 | SuperAdmin | System administrator |
| operationsadmin | admin123 | OperationsAdmin | Operations administrator |
| salesteamadmin | admin123 | SalesTeamAdmin | Sales team administrator |
| tenantadmin | admin123 | TenantAdmin | Tenant administrator |

### **PostgreSQL Configuration**

For optimal performance with umi_db, consider these PostgreSQL settings in `postgresql.conf`:

```ini
# Memory settings
shared_buffers = 256MB
effective_cache_size = 1GB
work_mem = 4MB
maintenance_work_mem = 64MB

# Connection settings
max_connections = 100
shared_preload_libraries = 'pg_stat_statements'
listen_addresses = '*'

# Logging
log_destination = 'stderr'
logging_collector = 'stderr'
log_line_prefix = 'umi_db'
log_min_duration_statement = 1000
log_checkpoints = on
log_connections = on
log_disconnections = on
log_lock_waits = on

# Performance
checkpoint_completion_target = 0.9
random_page_cost = 1.1
effective_io_concurrency = 200
```

### **Backup Strategy**

```bash
# Create backup user
psql -U root -d postgres -c "CREATE USER umi_backup WITH PASSWORD 'backup_password';"

# Grant necessary permissions
psql -U root -d postgres -c "GRANT CONNECT ON DATABASE umi_db TO umi_backup;"

# Backup script
pg_dump -U umi_backup -h localhost -d umi_db > umi_db_backup_$(date +%Y%m%d_%H%M%S).sql

# Restore script (if needed)
psql -U umi_backup -h localhost -d umi_db < umi_db_backup_latest.sql
```

### **Troubleshooting**

**Common Issues and Solutions:**

1. **Connection refused**
   - Check if PostgreSQL is running: `sudo systemctl status postgresql`
   - Verify connection string in appsettings.json
   - Ensure database user has privileges

2. **Permission denied**
   - Run as root user or grant appropriate privileges to your user
   - Check PostgreSQL pg_hba.conf file for authentication settings

3. **Database already exists**
   - The migration script handles this automatically
   - Tables will be created/updated as needed

4. **Migration fails partway through**
   - Check PostgreSQL logs: `tail -f /var/log/postgresql/postgresql-*.log`
   - Run migration in smaller chunks to identify the issue
   - Ensure all dependencies are installed

### **Migration Rollback**

If needed, you can rollback by:

```sql
-- Drop all tables (WARNING: This will delete all data)
DROP SCHEMA IF EXISTS public CASCADE;
CREATE SCHEMA public;

-- Re-run migration from beginning
```

### **Next Steps After Migration**

1. **Test Application Connection**
2. **Verify All Tables Created**
3. **Run Application and Test All Features**
4. **Monitor Performance and Optimize as Needed**

### **Support**

If you encounter any issues during migration:
1. Check PostgreSQL logs: `/var/log/postgresql/`
2. Verify database connection string
3. Ensure PostgreSQL user has sufficient privileges
4. Run migration in smaller chunks to isolate issues

The migration script includes comprehensive error handling and verification to ensure successful database setup.
