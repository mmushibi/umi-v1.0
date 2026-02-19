# Umi Health POS - Demo Tenant Setup

## Overview

This document describes the demo tenant that has been configured for the Umi Health POS system. The demo includes a complete pharmacy setup with a pharmacist and cashier user accounts.

## Demo Tenant Information

### Tenant Details
- **Tenant ID**: `UMI001`
- **Name**: Umi Health Pharmacy
- **Description**: Demo pharmacy for Umi Health POS system
- **Status**: Active
- **Pharmacy Name**: Umi Health Pharmacy
- **Admin Name**: Dr. Sarah Mwansa
- **Phone**: +260211234567
- **Email**: admin@umihealth.com
- **Address**: 123 Cairo Road, Lusaka, Zambia
- **License Number**: ZMP-LIC-UMI001
- **Zambia Reg Number**: ZMR-REG-UMI001

### Branches

#### Lusaka Branch
- **Name**: Umi Health Pharmacy - Lusaka
- **Address**: 123 Cairo Road, Lusaka
- **Region**: Lusaka Province
- **Phone**: +260211234567
- **Email**: main@umihealth.com
- **Manager**: Dr. Sarah Mwansa
- **Manager Phone**: +260976543210
- **Operating Hours**: 08:00-18:00

#### Kitwe Branch
- **Name**: Umi Health Pharmacy - Kitwe
- **Address**: 456 Obote Avenue, Kitwe
- **Region**: Copperbelt Province
- **Phone**: +260212345678
- **Email**: kitwe@umihealth.com
- **Manager**: Dr. James Banda
- **Manager Phone**: +260976543211
- **Operating Hours**: 08:00-18:00

## User Accounts

### Tenant Admin
- **User ID**: admin-user-001
- **Name**: Admin User
- **Email**: admin@umihealth.com
- **Password**: Admin123!
- **Phone**: +260976543212
- **Role**: TenantAdmin
- **Department**: Management
- **Branch**: Main Branch
- **Access**: Full system access

### Pharmacist
- **User ID**: pharmacist-001
- **Name**: Grace Chilufya
- **Email**: grace@umihealth.com
- **Password**: Pharmacist123!
- **Phone**: +260976543213
- **Role**: Pharmacist
- **Department**: Pharmacy
- **Branch**: Main Branch
- **License Number**: ZMP-PHARM-001
- **Access**: Pharmacy operations, inventory management, prescriptions

### Cashier
- **User ID**: cashier-001
- **Name**: John Banda
- **Email**: john@umihealth.com
- **Password**: Cashier123!
- **Phone**: +260976543214
- **Role**: Cashier
- **Department**: Sales
- **Branch**: Main Branch
- **Access**: Sales operations, inventory viewing

## Demo Inventory

### Main Branch Inventory
1. **Paracetamol 500mg**
   - Generic Name: Paracetamol
   - Brand Name: Panadol
   - Batch Number: PAN-2023-001
   - License Number: ZMP-LIC-001
   - Zambia Reg Number: ZMR-REG-001
   - Packing Type: Box
   - Quantity: 100
   - Unit Price: ZMW 2.50
   - Selling Price: ZMW 5.00
   - Reorder Level: 20

2. **Amoxicillin 250mg**
   - Generic Name: Amoxicillin
   - Brand Name: Amoxil
   - Batch Number: AMX-2023-001
   - License Number: ZMP-LIC-002
   - Zambia Reg Number: ZMR-REG-002
   - Packing Type: Bottle
   - Quantity: 50
   - Unit Price: ZMW 15.00
   - Selling Price: ZMW 25.00
   - Reorder Level: 15

### Kitwe Branch Inventory
1. **Vitamin C 500mg**
   - Generic Name: Ascorbic Acid
   - Brand Name: Cevit
   - Batch Number: VIT-2023-001
   - License Number: ZMP-LIC-003
   - Zambia Reg Number: ZMR-REG-003
   - Packing Type: Packet
   - Quantity: 200
   - Unit Price: ZMW 1.00
   - Selling Price: ZMW 2.50
   - Reorder Level: 50

## Role-Based Access

### Tenant Admin
- ✅ Full CRUD operations on all entities
- ✅ User management and role assignments
- ✅ System configuration and settings
- ✅ Reports and analytics
- ✅ Inventory import/export
- ✅ Multi-branch management

### Pharmacist
- ✅ View and manage inventory
- ✅ Add/edit inventory items
- ✅ Prescription management
- ✅ Patient management
- ✅ Clinical tools and guidelines
- ✅ Export inventory data
- ❌ Delete inventory items
- ❌ Import inventory files
- ❌ User management

### Cashier
- ✅ View inventory (read-only)
- ✅ Process sales transactions
- ✅ Customer management
- ✅ Export inventory data
- ❌ Modify inventory
- ❌ Access clinical tools
- ❌ User management

## Setup Instructions

### Quick Setup
1. Run the demo setup script:
   ```powershell
   .\create-demo-tenant.ps1
   ```

### Manual Setup
1. Build the solution:
   ```bash
   dotnet build Umi-Health-POS.sln
   ```

2. Run the application:
   ```bash
   cd backend
   dotnet run
   ```

3. The demo data will be automatically seeded on first run.

### Database
The demo uses PostgreSQL with the following connection string (configured in appsettings.json):
```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Host=localhost;Database=umihealth_pos;Username=postgres;Password=password"
  }
}
```

## Access URLs

- **Frontend**: http://localhost:5000
- **API**: http://localhost:5000/api
- **API Documentation**: http://localhost:5000/swagger (if enabled)

## Security Notes

✅ **Password Security Implemented**
- Uses ASP.NET Core's built-in PasswordHasher for secure password storage
- Passwords are properly salted and hashed using industry-standard algorithms
- No plain text passwords stored in database
- Demo passwords are provided for easy testing but stored securely

⚠️ **Demo Environment Only**
- Demo passwords are simple for testing purposes
- In production, enforce strong password policies
- Enable proper authentication and authorization
- Configure HTTPS and security headers
- Set up proper user authentication flows

## Support

For issues with the demo setup:
1. Check the application logs for errors
2. Ensure PostgreSQL is running and accessible
3. Verify connection string configuration
4. Check that all required database migrations have been applied

## Next Steps

After setting up the demo tenant:
1. **Explore Features**: Test different user roles and permissions
2. **Add Data**: Create more inventory items, customers, and transactions
3. **Test Workflows**: Try prescription management, sales processing, and reporting
4. **Customize**: Modify tenant settings, user preferences, and system configuration
5. **Scale**: Add more branches, users, and inventory as needed
