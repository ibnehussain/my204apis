# ASP.NET Core Azure Solution

Enterprise-grade ASP.NET Core solution demonstrating microservices architecture, Azure cloud integration, and modern DevOps practices.

## 🏗️ Architecture Overview

### Application Tier
- **Demo.API**: RESTful Web API (.NET 8)
  - Azure Cosmos DB integration with partition key strategy
  - Global exception handling middleware
  - Structured logging with performance tracking
  - Health checks and OpenAPI documentation
- **Demo.Web**: MVC Frontend (.NET 8)
  - HttpClient factory pattern for service communication
  - Responsive UI with Bootstrap framework
  - Centralized error handling

### Data Tier
- **Azure Cosmos DB**: NoSQL document database
  - Partition key: `/category` for optimal performance
  - Session consistency level
  - Optimized for multi-region distribution

### Infrastructure
- **Azure App Service**: PaaS hosting platform
- **Azure DevOps**: CI/CD pipeline automation
- **Application Insights**: Monitoring and telemetry (configurable)

## 🚀 Deployment Architecture

```
┌─────────────────┐    ┌─────────────────┐    ┌─────────────────┐
│   Developer     │    │  Azure DevOps   │    │  Azure Cloud    │
│                 │    │                 │    │                 │
│ ┌─────────────┐ │    │ ┌─────────────┐ │    │ ┌─────────────┐ │
│ │ Local Dev   │─┼────┼→│ Build Stage │─┼────┼→│ App Service │ │
│ └─────────────┘ │    │ └─────────────┘ │    │ └─────────────┘ │
│                 │    │ ┌─────────────┐ │    │ ┌─────────────┐ │
│ ┌─────────────┐ │    │ │Deploy Stage │─┼────┼→│ Cosmos DB   │ │
│ │ Git Commit  │─┼────┼→│             │ │    │ └─────────────┘ │
│ └─────────────┘ │    │ └─────────────┘ │    │                 │
└─────────────────┘    └─────────────────┘    └─────────────────┘
```

## 📋 CI/CD Pipeline Stages

### 1. **Build Stage**
```yaml
# Triggered on: main branch commits
Jobs:
  - Restore NuGet packages
  - Build solution (.NET 8)
  - Run unit tests (when available)
  - Publish artifacts (zip packages)
```

### 2. **Deploy Stage** 
```yaml
# Environment: Production
Dependencies: [Build Stage]
Jobs:
  - Deploy Demo.API → Azure App Service
  - Deploy Demo.Web → Azure App Service  
  - Configure app settings
  - Health check validation
```

### 3. **Staging Pipeline** (Optional)
```yaml
# Triggered on: develop, feature/* branches
Environment: Staging
Flow: Build → Deploy to Staging Environment
```

## 🔧 Key Implementation Patterns

### **Dependency Injection**
- Singleton CosmosClient with optimized configuration
- HttpClient factory for external service calls
- Scoped controllers with constructor injection

### **Error Handling**
- Global exception handling middleware
- Structured error responses with trace IDs
- Security-focused (no internal details exposed)

### **Performance Optimization**
- Partition key-based queries for Cosmos DB
- Connection pooling and retry policies  
- Request/response logging with timing

### **Security**
- HTTPS enforcement
- Security headers (X-Frame-Options, X-Content-Type-Options)
- Secrets management via Azure App Settings

## 🛠️ Local Development

### Prerequisites
- .NET 8 SDK
- Azure Cosmos DB Emulator
- Visual Studio 2022 / VS Code

### Quick Start
```bash
# Start Cosmos DB Emulator
# Run API (Terminal 1)
cd Demo.API && dotnet run --urls https://localhost:7001

# Run Web (Terminal 2)  
cd Demo.Web && dotnet run --urls https://localhost:7000
```

### Configuration
```json
// Demo.API/appsettings.Development.json
{
  "Cosmos": {
    "Endpoint": "https://localhost:8081/",
    "Key": "emulator-key",
    "DatabaseName": "DemoDatabase"
  }
}

// Demo.Web/appsettings.Development.json
{
  "ApiBaseUrl": "https://localhost:7001"
}
```

## 📊 Azure DevOps Setup

### Required Variables
| Variable | Description | Type |
|----------|-------------|------|
| `azureServiceConnection` | Service connection name | Variable |
| `apiAppServiceName` | API app service name | Variable |
| `webAppServiceName` | Web app service name | Variable | 
| `cosmosEndpoint` | Cosmos DB endpoint URL | Variable |
| `cosmosKey` | Cosmos DB access key | Secret |

### Resource Provisioning
```powershell
# Create Azure resources
.\deploy-azure-resources.ps1 -SubscriptionId "sub-id" -ResourceGroupName "rg-name" -Location "East US" -EnvironmentPrefix "prod"
```

## 🏆 Certification-Relevant Features

### **AZ-204 (Azure Developer)**
- ✅ App Service deployment and configuration
- ✅ Cosmos DB integration and partitioning
- ✅ Azure DevOps CI/CD implementation
- ✅ Application monitoring and logging

### **AZ-400 (DevOps Engineer)**  
- ✅ Multi-stage YAML pipelines
- ✅ Infrastructure as Code (PowerShell)
- ✅ Environment-specific deployments
- ✅ Automated testing integration points

### **Modern Development Practices**
- ✅ Microservices architecture
- ✅ Global exception handling
- ✅ Structured logging and monitoring  
- ✅ Security best practices
- ✅ Performance optimization patterns

---

**Tech Stack**: .NET 8, ASP.NET Core, Azure Cosmos DB, Azure App Service, Azure DevOps, Bootstrap, Swagger/OpenAPI
   git clone <repository-url>
   cd sqlapiappdev
   dotnet run
   ```

2. **Configure local settings**
   
   For Demo.API, update `appsettings.Development.json`:
   ```json
   {
     "Cosmos": {
       "Endpoint": "https://localhost:8081/",
       "Key": "C2y6yDjf5/R+ob0N8A7Cgv30VRDJIWEHLM+4QDU5DE2nQ9nDuVTqobD4b8mGGyPMbIZnqyMsEcaGQy67XIw/Jw==",
       "DatabaseName": "DemoDatabase"
     }
   }
   ```

   For Demo.Web, update `appsettings.Development.json`:
   ```json
   {
     "ApiBaseUrl": "https://localhost:7001"
   }
   ```

3. **Start Cosmos DB Emulator**
   
   Download and run the [Azure Cosmos DB Emulator](https://learn.microsoft.com/azure/cosmos-db/emulator)

4. **Run the applications**
   
   Open two terminal windows:
   
   ```bash
   # Terminal 1 - Start API
   cd Demo.API
   dotnet run
   # API will be available at https://localhost:7001
   ```
   
   ```bash
   # Terminal 2 - Start Web
   cd Demo.Web
   dotnet run
   # Web will be available at https://localhost:7000
   ```

5. **Access the applications**
   - Web App: https://localhost:7000
   - API Swagger: https://localhost:7001/swagger
   - Health checks: `/health` on both applications

## Azure Deployment

### 1. Azure Resource Setup

Run the PowerShell script to create all necessary Azure resources:

```powershell
# Install Azure PowerShell module if not installed
Install-Module -Name Az -AllowClobber

# Run deployment script
.\deploy-azure-resources.ps1 -SubscriptionId "your-subscription-id" -ResourceGroupName "demo-rg" -Location "East US" -EnvironmentPrefix "prod"
```

This creates:
- Resource Group
- App Service Plan (B1 tier)
- App Service for API
- App Service for Web
- Cosmos DB Account with database and container
- Configured app settings

### 2. Azure DevOps Pipeline Setup

1. **Import the repository** into Azure DevOps

2. **Create a service connection**
   - Go to Project Settings > Service Connections
   - Create new Azure Resource Manager connection
   - Note the connection name for pipeline variables

3. **Create pipeline variables**
   
   Set these variables in your Azure DevOps pipeline:
   
   | Variable Name | Value | Secret |
   |---------------|--------|--------|
   | azureServiceConnection | Your service connection name | No |
   | apiAppServiceName | prod-demo-api | No |
   | webAppServiceName | prod-demo-web | No |
   | resourceGroupName | demo-rg | No |
   | cosmosEndpoint | https://prod-demo-cosmos.documents.azure.com:443/ | No |
   | cosmosKey | Your Cosmos DB primary key | Yes |

4. **Create pipeline**
   - Create new pipeline using `azure-pipelines.yml`
   - The pipeline will automatically build and deploy on push to main branch

### 3. Staging Environment (Optional)

Use `azure-pipelines-staging.yml` for staging deployments:

1. Run the deployment script with staging parameters:
   ```powershell
   .\deploy-azure-resources.ps1 -SubscriptionId "your-subscription-id" -ResourceGroupName "demo-staging-rg" -Location "East US" -EnvironmentPrefix "staging"
   ```

2. Create a separate pipeline for staging using `azure-pipelines-staging.yml`

## API Endpoints

The Demo.API provides the following endpoints:

- `GET /api/items` - Get all items
- `GET /api/items/category/{category}` - Get items by category  
- `GET /api/items/{id}` - Get specific item
- `GET /health` - Health check

All endpoints support:
- Swagger documentation at `/swagger`
- Structured logging
- Error handling

## Project Structure

```
sqlapiappdev/
├── Demo.API/                    # ASP.NET Core Web API
│   ├── Controllers/
│   │   └── ItemsController.cs   # Items API endpoints
│   ├── Models/
│   │   └── Item.cs             # Item model
│   └── Program.cs              # API configuration
├── Demo.Web/                   # ASP.NET Core MVC
│   ├── Controllers/
│   │   └── HomeController.cs   # Home controller
│   ├── Services/
│   │   └── ItemService.cs      # API client service
│   ├── Models/
│   │   └── Item.cs            # Item model
│   └── Program.cs             # Web app configuration
├── azure-pipelines.yml        # Production pipeline
├── azure-pipelines-staging.yml # Staging pipeline
├── deploy-azure-resources.ps1  # Azure resource deployment
└── Demo.sln                   # Solution file
```

## Configuration

### Demo.API Configuration

```json
{
  "Cosmos": {
    "Endpoint": "https://your-cosmos-account.documents.azure.com:443/",
    "Key": "your-cosmos-key", 
    "DatabaseName": "DemoDatabase"
  }
}
```

### Demo.Web Configuration

```json
{
  "ApiBaseUrl": "https://your-api-url"
}
```

## Security Considerations

- Cosmos DB keys are stored as Azure App Service application settings
- No secrets are committed to source control
- HTTPS is enforced in production
- Security headers are configured
- CORS is properly configured

## Performance Features

- HttpClient factory pattern for optimal connection pooling
- Singleton CosmosClient for efficient database connections
- Async/await patterns throughout
- Structured logging for monitoring
- Health checks for monitoring
- API keys (use User Secrets for development)

### Environment Variables
- `ASPNETCORE_ENVIRONMENT` - Set to `Development`, `Staging`, or `Production`
- `ASPNETCORE_URLS` - Override default URLs if needed

## Security Considerations

- HTTPS enforcement in production
- Security headers configured
- Input validation on API endpoints
- CORS properly configured
- Error details hidden in production

## Troubleshooting

### Common Issues

1. **Build Errors with Newtonsoft.Json**
   - The solution includes `AzureCosmosDisableNewtonsoftJsonCheck` to resolve conflicts

2. **Local Development Connection Issues**
   - Ensure Cosmos DB Emulator is running
   - Verify API is accessible from Web application  
   - Check firewall/antivirus settings

3. **Azure Deployment Issues**
   - Verify all pipeline variables are set correctly
   - Ensure service connection has proper permissions
   - Check Azure App Service logs for runtime errors

### Logs and Monitoring

- Azure App Service logs: Available in Azure Portal under App Service > Log stream
- Application logs: Structured logging to Azure App Service logs
- API health: `https://your-api.azurewebsites.net/health`

## Contributing

1. Create feature branches from `develop`
2. Submit pull requests for review
3. Staging deployments trigger automatically on `develop` branch
4. Production deployments trigger on `main` branch

## License

This project is for demonstration purposes.