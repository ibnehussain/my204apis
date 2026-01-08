# App Service Configuration Guide

This document explains how to configure the Demo.Web application for deployment to Azure App Service.

## Configuration Settings

### ApiBaseUrl Configuration

The application requires the `ApiBaseUrl` setting to connect to the Demo.API backend.

**Local Development:**
- Configured in `appsettings.Development.json`: `https://localhost:7001`

**Production/App Service:**
- Must be configured through Azure App Service Application Settings
- Uses empty placeholder in `appsettings.json` and `appsettings.Production.json`

## Azure App Service Configuration

### Method 1: Azure Portal

1. Navigate to your App Service in the Azure Portal
2. Go to **Configuration** → **Application settings**
3. Add a new application setting:
   - **Name**: `ApiBaseUrl`
   - **Value**: `https://your-api-app-name.azurewebsites.net`

### Method 2: Azure CLI

```bash
az webapp config appsettings set \
    --resource-group myResourceGroup \
    --name myWebApp \
    --settings ApiBaseUrl=https://your-api-app-name.azurewebsites.net
```

### Method 3: ARM Template / Bicep

```json
{
  "type": "Microsoft.Web/sites/config",
  "apiVersion": "2022-03-01",
  "name": "[concat(parameters('webAppName'), '/appsettings')]",
  "properties": {
    "ApiBaseUrl": "[concat('https://', parameters('apiAppName'), '.azurewebsites.net')]"
  }
}
```

### Method 4: Environment Variables

App Service also supports environment variables:

```bash
# Environment variable format
ApiBaseUrl=https://your-api-app-name.azurewebsites.net
```

## Configuration Override Hierarchy

ASP.NET Core configuration follows this order (last wins):

1. `appsettings.json`
2. `appsettings.{Environment}.json`
3. User Secrets (Development only)
4. Environment Variables
5. **Azure App Service Application Settings** (highest priority)

## Example Scenarios

### Scenario 1: Separate API and Web Apps

```
Web App: https://demo-web.azurewebsites.net
API App: https://demo-api.azurewebsites.net

App Service Setting:
ApiBaseUrl = https://demo-api.azurewebsites.net
```

### Scenario 2: API Gateway/Custom Domain

```
Web App: https://demo-web.azurewebsites.net
API: https://api.mydomain.com

App Service Setting:
ApiBaseUrl = https://api.mydomain.com
```

### Scenario 3: Staging Slots

**Production Slot:**
```
ApiBaseUrl = https://demo-api.azurewebsites.net
```

**Staging Slot:**
```
ApiBaseUrl = https://demo-api-staging.azurewebsites.net
```

## Security Considerations

- ✅ **HTTPS Only**: Always use HTTPS URLs in production
- ✅ **No Hardcoding**: Never hardcode production URLs in appsettings files
- ✅ **Environment Separation**: Use different API endpoints for different environments
- ✅ **Slot Settings**: Consider making ApiBaseUrl a slot setting for blue-green deployments

## Validation

To verify the configuration is working:

1. Check the application logs for startup messages
2. Navigate to `/api/items` endpoint calls in the browser developer tools
3. Verify the correct API base URL is being used

## Troubleshooting

**Issue**: Application can't connect to API
- Verify ApiBaseUrl is set in App Service configuration
- Check that the API endpoint is accessible from the web app
- Ensure CORS is properly configured on the API

**Issue**: Configuration not taking effect
- Restart the App Service after changing application settings
- Check the configuration override hierarchy
- Verify the setting name matches exactly: `ApiBaseUrl`