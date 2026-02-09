# Umi Health POS - Complete Database SQL Files

This directory contains comprehensive SQL files for all pages and features of the Umi Health POS application. All files are designed to work with PostgreSQL database named `umi_db`.

## Complete File Structure

### **Core Database Files**

#### 1. **01_Create_Database_Schema.sql**
- **Purpose**: Complete database schema with all core tables
- **Contains**: Core tables, indexes, triggers, constraints, views, and initial system settings
- **Usage**: Run this file first to create complete database structure

#### 2. **08_Authentication_Account_Flow.sql**
- **Purpose**: User authentication, signup, signin, and comprehensive account management
- **Key Features**:
  - User accounts with detailed profile information
  - Multi-factor authentication (2FA) support
  - Session management and security logging
  - Password policies and history tracking
  - Role-based permissions system
  - Account registration and invitation workflows

#### 3. **09_Supplier_Management.sql**
- **Purpose**: Complete supplier and procurement management
- **Key Features**:
  - Supplier master data with Zambia compliance
  - Purchase order management
  - Goods receipt and quality control
  - Supplier performance evaluation
  - Contract management
  - Price list and catalog management

#### 4. **10_Daybook_Shift_Management.sql**
- **Purpose**: Daily operations, shift management, and business tracking
- **Key Features**:
  - Shift scheduling and assignment
  - Daybook operations and reconciliation
  - Cash drawer management
  - Daily sales and inventory summaries
  - Shift performance metrics
  - Incident tracking and management

#### 5. **11_Branch_Management.sql**
- **Purpose**: Multi-branch operations and inter-branch management
- **Key Features**:
  - Branch master data and configuration
  - Branch inventory management
  - Inter-branch transfers
  - Branch performance analytics
  - Equipment and asset tracking
  - Branch-specific settings and targets

#### 6. **12_Help_Training.sql**
- **Purpose**: Help system, training management, and user support
- **Key Features**:
  - Knowledge base and help articles
  - Training course management
  - User enrollment and progress tracking
  - Support ticket system
  - FAQ management
  - System alerts and notifications

### **Portal-Specific Files**

#### 7. **02_Tenant_Admin_Portal.sql**
- **Purpose**: Tables and functionality specific to Tenant Admin portal
- **Contains**: Multi-tenant management, user administration, reporting, audit logging

#### 8. **03_Pharmacist_Portal.sql**
- **Purpose**: Tables and functionality specific to Pharmacist portal
- **Contains**: Patient management, prescription tracking, clinical data, Zambia compliance

#### 9. **04_Cashier_Portal.sql**
- **Purpose**: Tables and functionality specific to Cashier portal
- **Contains**: Sales processing, payment handling, cash management, loyalty programs

#### 10. **05_Sales_Operations_Portal.sql**
- **Purpose**: Tables and functionality specific to Sales Operations portal
- **Contains**: Sales analytics, subscription management, performance tracking

#### 11. **06_Super_Admin_Portal.sql**
- **Purpose**: Tables and functionality specific to Super Admin portal
- **Contains**: System administration, multi-tenant management, monitoring

### **Supporting Files**

#### 12. **07_Seed_Data.sql**
- **Purpose**: Initial seed data for testing and demonstration
- **Contains**: Sample data for all major tables including users, products, customers, etc.

## Page Coverage Analysis

### **All Pages Covered:**

#### **Tenant Admin Portal:**
- ✅ **Home**: Dashboard with system overview
- ✅ **Point of Sale**: Sales transaction management
- ✅ **Patients**: Patient records and management
- ✅ **Sales**: Sales analytics and reporting
- ✅ **Payments**: Payment processing and tracking
- ✅ **Inventory**: Complete inventory management
- ✅ **Daybook**: Daily operations and reconciliation
- ✅ **Shift Management**: Staff scheduling and management
- ✅ **Reports**: Comprehensive reporting system
- ✅ **User Management**: User accounts and permissions
- ✅ **Branches**: Multi-branch operations
- ✅ **Suppliers**: Supplier and procurement management
- ✅ **Help & Training**: Support and training resources
- ✅ **Account**: Account settings and configuration

#### **Pharmacist Portal:**
- ✅ **Home**: Pharmacy dashboard
- ✅ **Patients**: Patient clinical records
- ✅ **Prescriptions**: Prescription management
- ✅ **Inventory**: Pharmacy inventory with Zambia compliance
- ✅ **Clinical**: Clinical tools and documentation
- ✅ **Compliance**: Regulatory compliance tracking
- ✅ **Reports**: Pharmacy-specific reports
- ✅ **Suppliers**: Pharmaceutical supplier management
- ✅ **Help & Training**: Pharmacy training resources
- ✅ **Account**: Pharmacist account settings

#### **Cashier Portal:**
- ✅ **Home**: Point of sale dashboard
- ✅ **Point of Sale**: Sales transaction processing
- ✅ **Inventory**: Product lookup and stock viewing
- ✅ **Prescriptions**: Prescription fulfillment
- ✅ **Patients**: Customer management
- ✅ **Sales**: Sales history and tracking
- ✅ **Payments**: Multiple payment method support
- ✅ **Reports**: Cashier performance reports
- ✅ **Account**: Cashier account settings

#### **Sales Operations Portal:**
- ✅ **Home**: Sales operations dashboard
- ✅ **Point of Sale**: Sales management oversight
- ✅ **Inventory**: Inventory analytics and optimization
- ✅ **Prescriptions**: Prescription sales tracking
- ✅ **Patients**: Customer relationship management
- ✅ **Sales**: Advanced sales analytics
- ✅ **Payments**: Payment method analysis
- ✅ **Reports**: Sales performance reports
- ✅ **User Management**: Sales team management
- ✅ **Branches**: Multi-branch sales oversight
- ✅ **Account**: Sales operations settings

#### **Super Admin Portal:**
- ✅ **Home**: System administration dashboard
- ✅ **Point of Sale**: System-wide POS oversight
- ✅ **Inventory**: Global inventory management
- ✅ **Prescriptions**: System prescription tracking
- ✅ **Patients**: Global patient management
- ✅ **Sales**: System-wide sales analytics
- ✅ **Payments**: Payment system management
- ✅ **Reports**: System-wide reporting
- ✅ **User Management**: Global user administration
- ✅ **Branches**: Multi-branch system management
- ✅ **Account**: System administration settings

## Role Hierarchy Implementation

### **Role-Based Access Control Structure:**

```
System/Super Admin
├── Operations/Sales Team
│   ├── Operations Admin (System-wide operations oversight)
│   └── Sales Team Admin (Sales team management)
├── Tenant Admin (Tenant-level administration)
├── Branch Manager (Branch-level management)
├── Pharmacists (Pharmacy operations)
├── Cashiers (Point of sale operations)
├── Sales Representatives (Customer management)
└── Accountants (Financial operations)
```

### **Role Permissions Matrix:**

#### **Super Admin**
- Full system access across all tenants
- User management across all roles
- System configuration and monitoring
- Audit log access
- Backup and restore capabilities

#### **Operations Admin**
- System-wide operations oversight
- Cross-tenant sales analytics
- Performance monitoring
- Compliance monitoring
- Team management for operations

#### **Sales Team Admin**
- Sales team management
- Target and commission management
- Territory management
- Customer relationship management
- Advanced sales reporting

#### **Tenant Admin**
- Full access within assigned tenant
- User management for tenant users
- Branch management within tenant
- Inventory and supplier management
- Reports and analytics for tenant

#### **Branch Manager**
- Branch operations management
- Staff scheduling and management
- Inventory control for branch
- Branch performance reporting

#### **Pharmacists**
- Prescription management
- Inventory management (read/write, no delete)
- Patient clinical records
- Compliance tracking

#### **Cashiers**
- Point of sale operations
- Customer management
- Sales transaction processing
- Inventory viewing (read-only)

#### **Sales Representatives**
- Customer relationship management
- Sales tracking and reporting
- Territory management
- Lead management

#### **Accountants**
- Financial reporting
- Transaction oversight
- Audit trail access
- Tax and compliance reporting

## Installation Instructions

### **1. Database Setup**
```sql
-- Create database
CREATE DATABASE umi_db;

-- Connect to database
\c umi_db
```

### **2. Run SQL Files in Order**
```bash
# 1. Create complete database schema
psql -h localhost -U postgres -d umi_db -f 01_Create_Database_Schema.sql

# 2. Run authentication and account flow
psql -h localhost -U postgres -d umi_db -f 08_Authentication_Account_Flow.sql

# 3. Run supplier management
psql -h localhost -U postgres -d umi_db -f 09_Supplier_Management.sql

# 4. Run daybook and shift management
psql -h localhost -U postgres -d umi_db -f 10_Daybook_Shift_Management.sql

# 5. Run branch management
psql -h localhost -U postgres -d umi_db -f 11_Branch_Management.sql

# 6. Run help and training
psql -h localhost -U postgres -d umi_db -f 12_Help_Training.sql

# 7. Run portal-specific files (optional - included in main schema)
psql -h localhost -U postgres -d umi_db -f 02_Tenant_Admin_Portal.sql
psql -h localhost -U postgres -d umi_db -f 03_Pharmacist_Portal.sql
psql -h localhost -U postgres -d umi_db -f 04_Cashier_Portal.sql
psql -h localhost -U postgres -d umi_db -f 05_Sales_Operations_Portal.sql
psql -h localhost -U postgres -d umi_db -f 06_Super_Admin_Portal.sql

# 8. Insert seed data for testing
psql -h localhost -U postgres -d umi_db -f 07_Seed_Data.sql
```

### **3. Configuration Files**
- **appsettings.json**: Main configuration with umi_db database
- **appsettings.Development.json**: Development settings
- **appsettings.Production.json**: Production settings

## Default Users

### **Super Admin**
- **Username**: superadmin
- **Password**: admin123
- **Role**: SuperAdmin

### **Tenant Admin**
- **Username**: tenantadmin
- **Password**: admin123
- **Role**: TenantAdmin

## Performance Considerations

### **Database Optimization**
- Strategic indexes on all tables
- Optimized queries in views
- Efficient foreign key relationships
- Proper data types and constraints

### **Security Features**
- Comprehensive audit logging
- Role-based permissions
- Session management
- Password security policies
- Multi-factor authentication support

## Maintenance and Support

### **Regular Tasks**
1. **Backup**: Daily automated backups
2. **Archive**: Archive old transaction data
3. **Monitor**: Check system health and performance
4. **Update**: Apply security updates and patches
5. **Review**: Monitor audit logs and user access

### **Scalability Considerations**
- Multi-tenant architecture for growth
- Branch management for expansion
- Supplier management for procurement optimization
- Training system for staff development
- Comprehensive reporting for business intelligence

## Version History

- **v1.0.0**: Complete multi-tenant POS system with all portal functionality
- Supports all requested pages and features
- Zambia-specific compliance features
- Comprehensive authentication and security
- Advanced inventory and supplier management
- Complete training and support system

This database structure provides a complete foundation for the Umi Health POS application, supporting all requested pages and features with proper PostgreSQL configuration for the `umi_db` database.
