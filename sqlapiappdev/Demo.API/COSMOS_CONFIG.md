# Cosmos DB Configuration Guide

This application uses Azure Cosmos DB for data storage. Configuration is handled through appsettings.json files with proper secret management.

## Configuration Structure

```json
{
  "Cosmos": {
    "Endpoint": "",
    "Key": "",
    "DatabaseName": "demodb",
    "ContainerName": "items",
    "PartitionKeyPath": "/category"
  }
}
```

## Environment Setup

### Development (Local Cosmos DB Emulator)

The Cosmos DB Emulator connection details are included in `appsettings.Development.json` for local development:

- **Endpoint**: `https://localhost:8081`
- **Key**: Well-known emulator key (safe for development)

### Production Configuration

For production environments, **DO NOT** store secrets in appsettings files. Use one of these approaches:

#### Option 1: Environment Variables

Set these environment variables:

```bash
Cosmos__Endpoint=https://your-account.documents.azure.com:443/
Cosmos__Key=your-actual-cosmos-key
```

#### Option 2: Azure Key Vault (Recommended)

1. Store secrets in Azure Key Vault:
   - `cosmos-endpoint`
   - `cosmos-key`

2. Configure Key Vault integration in `Program.cs`:

```csharp
builder.Configuration.AddAzureKeyVault(
    keyVaultUri,
    new DefaultAzureCredential()
);
```

#### Option 3: User Secrets (Development)

For local development with real Azure Cosmos DB:

```bash
dotnet user-secrets set "Cosmos:Endpoint" "https://your-account.documents.azure.com:443/"
dotnet user-secrets set "Cosmos:Key" "your-cosmos-key"
```

## Security Notes

- ✅ **Development**: Uses Cosmos DB Emulator with well-known key
- ✅ **Production**: Uses empty placeholders requiring external configuration
- ✅ **Never commit**: Real connection strings or keys to source control
- ✅ **Use Key Vault**: For production secret management
- ✅ **Environment variables**: Alternative for containerized deployments

## Cosmos DB Emulator Setup

For local development:

1. Download and install [Cosmos DB Emulator](https://learn.microsoft.com/azure/cosmos-db/emulator)
2. Start the emulator
3. Use the default endpoint: `https://localhost:8081`
4. Use the well-known emulator key (already configured)

## Configuration Validation

The application will throw an exception at startup if required Cosmos configuration is missing:

```
InvalidOperationException: Cosmos configuration is missing
```

Ensure all required configuration values are properly set for your environment.