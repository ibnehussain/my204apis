# Azure Resource Setup & Testing Guide

Step-by-step instructions to provision all Azure resources for **my204apis** and verify the full stack is working.

---

## Prerequisites

| Tool | Minimum Version | Install |
|------|----------------|---------|
| Azure CLI | 2.60+ | https://learn.microsoft.com/cli/azure/install-azure-cli |
| Node.js | 18+ | https://nodejs.org |
| Git | any | https://git-scm.com |
| Postman (optional) | any | https://www.postman.com |

Login to Azure before running any commands:

```bash
az login
az account set --subscription "<your-subscription-id>"
```

---

## Part 1 — Create Azure Resources

### 1.1 Resource Group

```bash
az group create \
  --name rg-my204apis \
  --location eastus
```

---

### 1.2 Azure Cosmos DB (NoSQL API)

```bash
az cosmosdb create \
  --name cosmos-my204apis \
  --resource-group rg-my204apis \
  --kind GlobalDocumentDB \
  --default-consistency-level Session \
  --locations regionName=eastus \
  --capabilities EnableServerless
```

> **Note:** `EnableServerless` provisions a serverless account — ideal for dev/test.  
> Remove it for a provisioned-throughput account in production.

Get the endpoint (you will need this later):

```bash
az cosmosdb show \
  --name cosmos-my204apis \
  --resource-group rg-my204apis \
  --query documentEndpoint \
  --output tsv
```

---

### 1.3 App Service Plan (Linux B1)

```bash
az appservice plan create \
  --name plan-my204apis \
  --resource-group rg-my204apis \
  --sku B1 \
  --is-linux
```

---

### 1.4 Web App (Node.js 20)

```bash
az webapp create \
  --name app-my204apis \
  --resource-group rg-my204apis \
  --plan plan-my204apis \
  --runtime "NODE|20-lts"
```

---

### 1.5 Enable System-Assigned Managed Identity

```bash
az webapp identity assign \
  --name app-my204apis \
  --resource-group rg-my204apis
```

Save the `principalId` from the output — you need it for the role assignment:

```bash
PRINCIPAL_ID=$(az webapp identity show \
  --name app-my204apis \
  --resource-group rg-my204apis \
  --query principalId \
  --output tsv)

echo "Principal ID: $PRINCIPAL_ID"
```

---

### 1.6 Assign Cosmos DB Built-in Data Contributor Role

```bash
COSMOS_RESOURCE_ID=$(az cosmosdb show \
  --name cosmos-my204apis \
  --resource-group rg-my204apis \
  --query id \
  --output tsv)

az cosmosdb sql role assignment create \
  --account-name cosmos-my204apis \
  --resource-group rg-my204apis \
  --role-definition-id "00000000-0000-0000-0000-000000000002" \
  --principal-id $PRINCIPAL_ID \
  --scope $COSMOS_RESOURCE_ID
```

> Role `00000000-0000-0000-0000-000000000002` = **Cosmos DB Built-in Data Contributor**  
> This grants read/write access to data — **no keys are needed**.

---

### 1.7 Configure App Service Environment Variables

```bash
COSMOS_ENDPOINT=$(az cosmosdb show \
  --name cosmos-my204apis \
  --resource-group rg-my204apis \
  --query documentEndpoint \
  --output tsv)

az webapp config appsettings set \
  --name app-my204apis \
  --resource-group rg-my204apis \
  --settings \
    COSMOS_ENDPOINT=$COSMOS_ENDPOINT \
    COSMOS_DATABASE_ID=my204db \
    COSMOS_CONTAINER_ID=items \
    NODE_ENV=production
```

---

### 1.8 Azure API Management (APIM)

> APIM creation takes 30–45 minutes. Use `--no-wait` to continue working.

```bash
az apim create \
  --name apim-my204apis \
  --resource-group rg-my204apis \
  --publisher-name "My204 APIs" \
  --publisher-email "admin@example.com" \
  --sku-name Developer \
  --location eastus \
  --no-wait
```

Monitor provisioning status:

```bash
az apim show \
  --name apim-my204apis \
  --resource-group rg-my204apis \
  --query provisioningState \
  --output tsv
```

Wait until output is `Succeeded`, then import the OpenAPI spec:

```bash
az apim api import \
  --service-name apim-my204apis \
  --resource-group rg-my204apis \
  --path "/my204apis/v1" \
  --api-id my204apis \
  --specification-format OpenApi \
  --specification-path ./infra/apim/openapi.yaml \
  --display-name "my204apis Items API" \
  --protocols https
```

---

### 1.9 Configure APIM Named Values

Set the three named values required by the APIM policies:

```bash
TENANT_ID=$(az account show --query tenantId --output tsv)
APP_SERVICE_URL="https://app-my204apis.azurewebsites.net"

# Tenant ID
az apim nv create \
  --service-name apim-my204apis \
  --resource-group rg-my204apis \
  --named-value-id tenant-id \
  --display-name tenant-id \
  --value $TENANT_ID

# Backend App Service URL
az apim nv create \
  --service-name apim-my204apis \
  --resource-group rg-my204apis \
  --named-value-id backend-app-service-url \
  --display-name backend-app-service-url \
  --value $APP_SERVICE_URL

# APIM Audience (set to your App Registration Client ID)
az apim nv create \
  --service-name apim-my204apis \
  --resource-group rg-my204apis \
  --named-value-id apim-audience \
  --display-name apim-audience \
  --value "<your-app-registration-client-id>" \
  --secret true
```

---

### 1.10 Apply APIM Policies

In the **Azure Portal**:

1. Go to **API Management** → `apim-my204apis` → **APIs** → `my204apis Items API`
2. Click **All operations** → **Inbound processing** → `</>` (Policy editor)
3. Paste the contents of `infra/apim/policies/transform.xml`
4. For JWT validation, paste `infra/apim/policies/jwt-validate.xml`
5. For rate limiting, paste `infra/apim/policies/rate-limit.xml`
6. Click **Save**

---

### 1.11 Log Analytics Workspace + Azure Monitor

```bash
az monitor log-analytics workspace create \
  --workspace-name law-my204apis \
  --resource-group rg-my204apis \
  --location eastus

WORKSPACE_ID=$(az monitor log-analytics workspace show \
  --workspace-name law-my204apis \
  --resource-group rg-my204apis \
  --query id \
  --output tsv)

# Enable diagnostic logs for App Service
az monitor diagnostic-settings create \
  --name diag-appservice \
  --resource $(az webapp show --name app-my204apis --resource-group rg-my204apis --query id --output tsv) \
  --workspace $WORKSPACE_ID \
  --logs '[{"category":"AppServiceHTTPLogs","enabled":true},{"category":"AppServiceConsoleLogs","enabled":true}]' \
  --metrics '[{"category":"AllMetrics","enabled":true}]'
```

---

## Part 2 — Provision Cosmos DB Database & Upload Sample Data

### 2.1 Set environment variable locally

```powershell
# PowerShell
$env:COSMOS_ENDPOINT = "https://cosmos-my204apis.documents.azure.com:443/"
```

```bash
# Bash
export COSMOS_ENDPOINT="https://cosmos-my204apis.documents.azure.com:443/"
```

### 2.2 Create database, container, and seed data

```bash
node infra/cosmos/setup.js
```

Expected output:

```
Creating database "my204db" if not exists...
  ✓ Database: my204db
Creating container "items" if not exists...
  ✓ Container: items
Seeding sample data...
  ✓ Seeded item: Laptop Stand
  ✓ Seeded item: Mechanical Keyboard
  ✓ Seeded item: USB-C Hub

Setup complete.
```

### 2.3 Upload full sample documents

```bash
node infra/cosmos/upload.js
```

Expected output:

```
Uploading 10 document(s) to "my204db/items"
Mode: create (skip existing)

  ✓ a1b2c3d4-0001-...  "Wireless Noise-Cancelling Headphones"
  ✓ a1b2c3d4-0002-...  "Ergonomic Office Chair"
  ...

── Summary ──────────────────────
  Uploaded : 10
  Skipped  : 0
  Failed   : 0
─────────────────────────────────
```

---

## Part 3 — Deploy the Application

### 3.1 Set GitHub Secrets

In your GitHub repo → **Settings** → **Secrets and variables** → **Actions**, add:

| Secret Name | Value |
|-------------|-------|
| `AZURE_CLIENT_ID` | App Registration Client ID (OIDC) |
| `AZURE_TENANT_ID` | Azure AD Tenant ID |
| `AZURE_SUBSCRIPTION_ID` | Azure Subscription ID |
| `AZURE_WEBAPP_NAME` | `app-my204apis` |

### 3.2 Trigger deployment

```bash
git push origin main
```

GitHub Actions (`.github/workflows/deploy.yml`) will automatically build and deploy to the App Service.

Monitor the deployment:

```bash
az webapp log tail \
  --name app-my204apis \
  --resource-group rg-my204apis
```

---

## Part 4 — Testing

### 4.1 Health Check (no auth required)

```bash
curl https://app-my204apis.azurewebsites.net/health
```

Expected response:

```json
{
  "status": "healthy",
  "timestamp": "2026-04-13T12:00:00.000Z"
}
```

---

### 4.2 Test via APIM Gateway

Get your APIM gateway URL:

```bash
az apim show \
  --name apim-my204apis \
  --resource-group rg-my204apis \
  --query gatewayUrl \
  --output tsv
```

Base URL: `https://apim-my204apis.azure-api.net/my204apis/v1`

---

### 4.3 Obtain a Bearer Token (Azure AD)

```bash
TOKEN=$(az account get-access-token \
  --resource "<your-app-registration-client-id>" \
  --query accessToken \
  --output tsv)
```

---

### 4.4 CRUD Endpoint Tests

**List all items**

```bash
curl -s -H "Authorization: Bearer $TOKEN" \
  https://apim-my204apis.azure-api.net/my204apis/v1/items | jq .
```

**Filter by category**

```bash
curl -s -H "Authorization: Bearer $TOKEN" \
  "https://apim-my204apis.azure-api.net/my204apis/v1/items?category=electronics" | jq .
```

**Get item by ID**

```bash
curl -s -H "Authorization: Bearer $TOKEN" \
  https://apim-my204apis.azure-api.net/my204apis/v1/items/a1b2c3d4-0001-0000-0000-000000000001 | jq .
```

**Create a new item**

```bash
curl -s -X POST \
  -H "Authorization: Bearer $TOKEN" \
  -H "Content-Type: application/json" \
  -d '{
    "name": "Test Item",
    "description": "Created via API test",
    "category": "accessories",
    "price": 19.99
  }' \
  https://apim-my204apis.azure-api.net/my204apis/v1/items | jq .
```

**Update an item** (replace `<id>` with the id from the create response)

```bash
curl -s -X PUT \
  -H "Authorization: Bearer $TOKEN" \
  -H "Content-Type: application/json" \
  -d '{
    "price": 24.99,
    "description": "Updated description"
  }' \
  https://apim-my204apis.azure-api.net/my204apis/v1/items/<id> | jq .
```

**Delete an item**

```bash
curl -s -X DELETE \
  -H "Authorization: Bearer $TOKEN" \
  https://apim-my204apis.azure-api.net/my204apis/v1/items/<id>
# Expected: HTTP 204 No Content
```

---

### 4.5 Test APIM Policies

**Missing token → should return 401**

```bash
curl -s https://apim-my204apis.azure-api.net/my204apis/v1/items
# Expected: {"statusCode":401,"message":"Unauthorized: valid JWT required"}
```

**Invalid token → should return 401**

```bash
curl -s -H "Authorization: Bearer invalid.token.here" \
  https://apim-my204apis.azure-api.net/my204apis/v1/items
# Expected: 401
```

**Rate limit test** — send 101 requests in quick succession; the 101st should return 429:

```bash
for i in $(seq 1 101); do
  STATUS=$(curl -s -o /dev/null -w "%{http_code}" \
    -H "Authorization: Bearer $TOKEN" \
    https://apim-my204apis.azure-api.net/my204apis/v1/items)
  echo "Request $i: HTTP $STATUS"
done
```

---

### 4.6 Test Using Postman

1. Open Postman → **Import** → paste the URL or upload `infra/apim/openapi.yaml`
2. Set up an **Environment** with:
   - `baseUrl` = `https://apim-my204apis.azure-api.net/my204apis/v1`
   - `token` = (value from `az account get-access-token`)
3. Add a **Collection-level Authorization** header:
   - Type: `Bearer Token`
   - Token: `{{token}}`
4. Run the collection

---

## Part 5 — Verify Azure Monitor

### 5.1 Check App Service logs in Log Analytics

```bash
az monitor log-analytics query \
  --workspace $WORKSPACE_ID \
  --analytics-query "AppServiceHTTPLogs | order by TimeGenerated desc | take 20" \
  --output table
```

### 5.2 View metrics in Azure Portal

1. Go to **App Service** → `app-my204apis` → **Monitoring** → **Metrics**
2. Add metric: **Requests**, **Response Time**, **Http 4xx**, **Http 5xx**
3. Set time range to **Last 30 minutes**

---

## Resource Summary

| Resource | Name | Type |
|----------|------|------|
| Resource Group | `rg-my204apis` | Resource Group |
| Cosmos DB | `cosmos-my204apis` | NoSQL (Serverless) |
| App Service Plan | `plan-my204apis` | B1 Linux |
| Web App | `app-my204apis` | Node.js 20 |
| API Management | `apim-my204apis` | Developer SKU |
| Log Analytics | `law-my204apis` | Workspace |

---

## Cleanup

To delete all resources when no longer needed:

```bash
az group delete \
  --name rg-my204apis \
  --yes \
  --no-wait
```

> ⚠️ This permanently deletes all resources in the group.
