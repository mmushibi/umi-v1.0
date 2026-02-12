# Build and Testing Configuration

This document outlines the comprehensive build and testing setup for the Umi Health POS application across GitHub Actions and Azure Pipelines.

## Overview

The CI/CD pipeline includes:
- **Build**: .NET application compilation and frontend asset building
- **Testing**: Unit tests, integration tests, and code coverage
- **Quality**: Code formatting, security scanning, and vulnerability checks
- **Deployment**: Docker image building and deployment to staging/production

## Test Structure

### Unit Tests
- **Location**: `backend/Tests/Unit/`
- **Coverage**: Controllers, Services, and business logic
- **Framework**: xUnit with FluentAssertions and Moq

### Integration Tests
- **Location**: `backend/Tests/Integration/`
- **Coverage**: API endpoints and database operations
- **Framework**: xUnit with ASP.NET Core testing utilities

### Test Categories
- Use `[Trait("Category", "Unit")]` for unit tests
- Use `[Trait("Category", "Integration")]` for integration tests

## GitHub Actions Configuration

### Workflow Triggers
- **Push**: `main`, `develop`, `staging` branches
- **Pull Request**: `main`, `develop` branches
- **Manual**: Workflow dispatch with environment selection

### Jobs Overview

#### 1. Lint and Format
- Node.js CSS build and formatting check
- .NET code formatting verification
- Runs on: `ubuntu-latest`

#### 2. Security Scan
- Trivy vulnerability scanner
- SARIF output for GitHub security tab
- Runs on: `ubuntu-latest`

#### 3. Build and Test
- **Services**: PostgreSQL 15 database
- **Steps**:
  - Restore NuGet packages
  - Build application in Release mode
  - Run unit tests with code coverage
  - Run integration tests
  - Upload test results and coverage reports
  - Publish application artifacts
- Runs on: `ubuntu-latest`

#### 4. Frontend Build
- Install Node.js dependencies
- Build CSS assets for production
- Upload frontend artifacts
- Runs on: `ubuntu-latest`

#### 5. Docker Build
- Build and push backend and frontend Docker images
- Multi-platform caching with GitHub Actions cache
- Conditional: Only on `main` and `develop` branches
- Runs on: `ubuntu-latest`

#### 6. Deploy Staging
- Deploys to staging environment
- Conditional: Only on `develop` branch
- Runs on: `ubuntu-latest`

#### 7. Deploy Production
- Deploys to production environment
- Conditional: Only on `main` branch
- Runs on: `ubuntu-latest`

#### 8. Deploy Azure
- Azure Web App deployment
- Conditional: Production environment or main branch
- Runs on: `ubuntu-latest`

## Azure Pipelines Configuration

### Triggers
- **Branches**: `main`, `develop`, `staging`
- **Pull Requests**: `main`, `develop`

### Stages Overview

#### Stage 1: Build and Test
- **Matrix Strategy**: Linux and Windows agents
- **Steps**:
  - .NET and Node.js setup
  - NuGet package restore
  - Frontend dependency installation and CSS build
  - .NET application build
  - Unit and integration test execution
  - Test results publishing
  - Code coverage reporting
  - Application publishing
  - Artifact publishing

#### Stage 2: Security Scan
- Trivy security scanning
- SARIF result publishing
- Runs on: `ubuntu-latest`

#### Stage 3: Docker Build
- Download build artifacts
- Build and push Docker images to registry
- Conditional: Only on `main` and `develop` branches
- Runs on: `ubuntu-latest`

#### Stage 4: Deploy Staging
- Azure Web App deployment to staging
- Conditional: Only on `develop` branch
- Environment: `staging`

#### Stage 5: Deploy Production
- Azure Web App deployment to production
- Conditional: Only on `main` branch
- Environment: `production`

## Environment Variables and Secrets

### GitHub Actions
- `DOTNET_VERSION`: '8.0.x'
- `NODE_VERSION`: '18'
- `REGISTRY`: ghcr.io
- `AZURE_CREDENTIALS`: Azure service principal
- `AZURE_WEBAPP_NAME`: Azure Web App name

### Azure Pipelines
- `dotnetVersion`: '8.0.x'
- `nodeVersion`: '18'
- `buildConfiguration`: 'Release'
- `vmImageName`: 'ubuntu-latest'

## Required Secrets

### GitHub Repository Secrets
- `AZURE_CREDENTIALS`: JSON service principal for Azure deployment
- `AZURE_WEBAPP_NAME`: Name of the Azure Web App

### Azure DevOps Variable Groups
- `AzureServiceConnection`: Azure service connection
- `dockerhub`: Docker Hub registry connection

## Code Quality and Security

### Code Formatting
- **.NET**: `dotnet format --verify-no-changes`
- **CSS**: Tailwind CSS production build

### Security Scanning
- **Trivy**: Container and filesystem vulnerability scanning
- **SARIF**: Results uploaded to security tabs

### Code Coverage
- **Tool**: Coverlet with XPlat Code Coverage
- **Format**: Cobertura XML
- **Reporting**: Codecov integration

## Testing Commands

### Local Testing
```bash
# Run all tests
dotnet test backend/UmiHealthPOS.Tests.csproj

# Run unit tests only
dotnet test backend/UmiHealthPOS.Tests.csproj --filter Category=Unit

# Run integration tests only
dotnet test backend/UmiHealthPOS.Tests.csproj --filter Category=Integration

# Run with coverage
dotnet test backend/UmiHealthPOS.Tests.csproj --collect:"XPlat Code Coverage"
```

### Build Commands
```bash
# Build application
dotnet build backend/UmiHealthPOS.csproj --configuration Release

# Build frontend
npm run build-css-prod

# Publish application
dotnet publish backend/UmiHealthPOS.csproj --configuration Release --output ./publish
```

## Docker Configuration

### Backend Dockerfile
- **Base**: `mcr.microsoft.com/dotnet/aspnet:8.0`
- **Build Stage**: Multi-stage build with SDK
- **Runtime**: ASP.NET runtime
- **Port**: 8080

### Frontend Dockerfile
- **Base**: `nginx:alpine`
- **Static Files**: Served from wwwroot
- **Configuration**: Nginx configuration included

## Deployment Strategy

### Staging Environment
- **Trigger**: Push to `develop` branch
- **Target**: Azure Web App (staging slot)
- **Validation**: Smoke tests and health checks

### Production Environment
- **Trigger**: Push to `main` branch
- **Target**: Azure Web App (production slot)
- **Validation**: Full regression tests

### Rollback Strategy
- Manual rollback through Azure portal
- Previous Docker image tags maintained
- Database migration rollback procedures

## Monitoring and Logging

### Application Logs
- **Framework**: Serilog with structured logging
- **Levels**: Information, Warning, Error
- **Destinations**: Console, File, Azure Application Insights

### Build Logs
- **GitHub Actions**: Workflow run logs
- **Azure Pipelines**: Build and release logs
- **Retention**: 30 days standard, 90 days premium

## Performance Considerations

### Build Optimization
- **NuGet Caching**: Package caching across builds
- **Docker Layer Caching**: Multi-stage build optimization
- **Parallel Execution**: Matrix strategies for multiple agents

### Test Performance
- **In-Memory Database**: Fast test execution
- **Parallel Tests**: xUnit parallel execution
- **Test Isolation**: Independent test data

## Troubleshooting

### Common Issues
1. **Test Failures**: Check database seeding and test data
2. **Build Errors**: Verify NuGet package versions and dependencies
3. **Deployment Issues**: Check Azure configuration and connection strings
4. **Docker Issues**: Verify Dockerfile syntax and base images

### Debugging Steps
1. Check build logs for specific error messages
2. Verify environment variables and secrets
3. Test locally before CI/CD execution
4. Use debug builds for detailed error information

## Best Practices

### Code Quality
- Write unit tests for all business logic
- Use integration tests for API endpoints
- Maintain >80% code coverage
- Follow .NET coding conventions

### Security
- Regularly update dependencies
- Scan for vulnerabilities
- Use secure secrets management
- Implement proper authentication/authorization

### Performance
- Optimize Docker image sizes
- Use appropriate caching strategies
- Monitor build times and optimize bottlenecks
- Implement proper resource limits

## Future Enhancements

### Planned Improvements
- [ ] Add performance testing with k6
- [ ] Implement contract testing with Pact
- [ ] Add visual regression testing
- [ ] Implement chaos engineering tests
- [ ] Add compliance scanning (PCI, HIPAA)

### Monitoring Enhancements
- [ ] Real-time build status dashboards
- [ ] Automated performance regression detection
- [ ] Security alerting and remediation
- [ ] Cost optimization monitoring
