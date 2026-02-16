# Umi Health POS System

A comprehensive Point of Sale (POS) system designed for healthcare and pharmacy operations in Zambia. This multi-portal application provides role-based access control for inventory management, sales operations, and pharmaceutical compliance.

## 🏗️ Architecture

### Multi-Portal System
- **Tenant Admin Portal**: Full inventory management and system administration
- **Pharmacist Portal**: Clinical operations and inventory management (limited write access)
- **Cashier Portal**: Sales operations and inventory viewing (read-only access)
- **Super Admin Portal**: System-wide administration

### Technology Stack
- **Backend**: ASP.NET Core with Entity Framework Core
- **Frontend**: HTML5, CSS3, JavaScript with TailwindCSS
- **Database**: PostgreSQL
- **Authentication**: JWT-based authentication
- **Containerization**: Docker and Docker Compose

## 🚀 Quick Start

### Prerequisites
- Docker and Docker Compose
- .NET 8.0 SDK
- PostgreSQL (if running locally)

### Using Docker (Recommended)

```bash
# Clone the repository
git clone <repository-url>
cd Umi-Health-POS

# Start all services
docker-compose up -d

# View logs
docker-compose logs -f

# Stop all services
docker-compose down
```

### Manual Setup

1. **Backend Setup**
```bash
cd backend
dotnet restore
dotnet build
dotnet run
```

2. **Frontend Setup**
```bash
# Install dependencies
npm install

# Build CSS
npm run build

# Serve the frontend (using any static server)
npx serve .
```

## 📋 Features

### Inventory Management
- Complete CRUD operations for pharmaceutical products
- Zambia-specific compliance fields (Zambia REG Number, License Number)
- Batch tracking and expiry management
- Stock level monitoring and reorder alerts
- CSV import/export functionality
- Real-time inventory synchronization across portals

### Sales Operations
- Point of Sale functionality
- Customer management
- Sales reporting and analytics
- Receipt generation
- Payment processing

### Role-Based Access Control
- **Tenant Admin**: Full system access
- **Pharmacist**: Inventory management and clinical operations
- **Cashier**: Sales operations and inventory viewing
- **Super Admin**: System-wide administration

### Compliance & Reporting
- Zambia pharmaceutical compliance
- Audit logging
- Sales reporting
- Inventory reports
- Export functionality

## 🔧 Configuration

### Environment Variables
Create a `.env` file or configure in `appsettings.json`:

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Host=localhost;Database=umi_health_pos;Username=your_username;Password=your_password"
  },
  "Jwt": {
    "Issuer": "your-issuer",
    "Audience": "your-audience",
    "Key": "your-secret-key"
  },
  "Frontend": {
    "AllowedOrigins": ["http://localhost:3000", "http://localhost:5000"]
  }
}
```

### Database Setup
The application uses PostgreSQL. The database schema is automatically created through Entity Framework migrations.

## 📁 Project Structure

```
Umi-Health-POS/
├── backend/                 # ASP.NET Core backend
│   ├── Controllers/         # API controllers
│   ├── Models/             # Entity models
│   ├── Data/               # Database context and seeding
│   ├── Configuration/      # Service configuration
│   └── Program.cs          # Application entry point
├── modules/                # Frontend portal modules
│   ├── Tenant-Admin/       # Tenant admin portal
│   ├── Pharmacist/         # Pharmacist portal
│   ├── Cashier/            # Cashier portal
│   └── Super-Admin/        # Super admin portal
├── wwwroot/               # Static assets
├── scripts/               # Utility scripts
└── docker-compose.yml     # Docker configuration
```

## 🔐 Authentication

The system uses JWT-based authentication with role-based authorization. Users are assigned to specific roles that determine their access levels across different portals.

### User Roles
- **TenantAdmin**: Full inventory and system management
- **Pharmacist**: Clinical operations and inventory management
- **Cashier**: Sales operations and inventory viewing
- **SuperAdmin**: System-wide administration

## 📊 Database Schema

The system includes comprehensive tables for:
- **Inventory**: Products, stock tracking, batch management
- **Sales**: Transactions, customers, receipts
- **Clinical**: Prescriptions, patients, medication records
- **System**: Users, tenants, audit logs, settings

## 🚢 Deployment

### Docker Deployment
```bash
# Build and deploy
docker-compose -f docker-compose.prod.yml up -d
```

### Manual Deployment
1. Configure production database connection
2. Set up JWT secrets
3. Configure CORS origins
4. Run database migrations
5. Deploy backend and frontend

## 🧪 Testing

```bash
# Run backend tests
cd backend
dotnet test

# Run frontend tests (if configured)
npm test
```

## 📝 API Documentation

The API provides endpoints for:
- Inventory management (`/api/tenantadmin/inventory`)
- User authentication (`/api/auth`)
- Sales operations (`/api/sales`)
- System administration (`/api/admin`)

## 🤝 Contributing

1. Fork the repository
2. Create a feature branch
3. Make your changes
4. Add tests if applicable
5. Submit a pull request

## 📄 License

This project is proprietary software for Umi Health Systems.

## 🆘 Support

For support and inquiries:
- Contact the development team
- Review the project documentation
- Check existing issues and discussions

## 🔄 Version History

- **v1.0.0**: Initial release with core POS functionality
- Multi-portal architecture
- Role-based access control
- Zambia compliance features
- Real-time inventory management
