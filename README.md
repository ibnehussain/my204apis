# my204apis

[![Node.js](https://img.shields.io/badge/Node.js-%3E%3D18-brightgreen)](https://nodejs.org)
[![Azure App Service](https://img.shields.io/badge/Azure-App%20Service-blue)](https://azure.microsoft.com/en-us/products/app-service)
[![License: MIT](https://img.shields.io/badge/License-MIT-yellow.svg)](LICENSE)

A production-ready REST API built with **Node.js / Express**, deployed on **Azure App Service**, fronted by **Azure API Management (APIM)**, and backed by **Azure Cosmos DB** — fully keyless using **System-Assigned Managed Identity**.

---

## Architecture

```
Client (Browser / Postman)
        │
        │ HTTPS
        ▼
┌─────────────────────────┐
│      Azure APIM         │  ← Dev Portal (API docs)
│  • JWT validation       │
│  • Rate limiting        │
│  • Request transforms   │
│  • Security headers     │
└────────────┬────────────┘
             │ route (internal HTTP)
             ▼
┌─────────────────────────┐
│  App Service (Node.js)  │  ← GitHub Actions CI/CD
│  • Express REST API     │
│  • CRUD endpoints       │
│  • App Insights logging │
└────────────┬────────────┘
             │ SDK — Managed Identity (no keys)
             ▼
┌─────────────────────────┐
│    Azure Cosmos DB      │
│  • NoSQL / Core SQL     │
│  • multi-region         │
│  • serverless           │
└─────────────────────────┘
             ↑↑
      Azure Monitor
   (logs + metrics from
    APIM + App Service)
```

---

## Project Structure

```
my204apis/
├── app.js                          # Express entry point + App Insights init
├── package.json
├── .env.example                    # Environment variable template
│
├── src/
│   ├── config/
│   │   └── cosmosClient.js         # Cosmos DB client (Managed Identity)
│   ├── controllers/
│   │   └── itemsController.js      # CRUD handlers
│   ├── middleware/
│   │   ├── errorHandler.js         # Global error handler
│   │   └── requestLogger.js        # Request/response logger
│   ├── models/
│   │   └── item.js                 # Item factory + validation
│   └── routes/
│       └── items.js                # Express router
│
├── infra/
│   ├── apim/
│   │   ├── openapi.yaml            # OpenAPI 3.0 spec (import into APIM)
│   │   └── policies/
│   │       ├── jwt-validate.xml    # Azure AD JWT validation policy
│   │       ├── rate-limit.xml      # 100 req/60s rate limit policy
│   │       └── transform.xml       # Strip auth header + security headers
│   ├── azure/
│   │   └── provision.ps1           # One-shot Azure provisioning script
│   └── cosmos/
│       ├── setup.js                # Create DB + container + seed data
│       └── sampleData.json         # Sample items
│
└── .github/
    └── workflows/
        └── deploy.yml              # GitHub Actions → Azure App Service
```

---

## API Endpoints

| Method | Path | Description |
|--------|------|-------------|
| `GET` | `/health` | Health check (no auth required) |
| `GET` | `/api/items` | List all items (optional `?category=` filter) |
| `GET` | `/api/items/:id` | Get item by ID |
| `POST` | `/api/items` | Create a new item |
| `PUT` | `/api/items/:id` | Update an existing item |
| `DELETE` | `/api/items/:id` | Delete an item |

All endpoints except `/health` require a valid **Bearer JWT** (enforced by APIM).

### Item Schema

```json
{
  "id": "uuid",
  "name": "string (required)",
  "description": "string",
  "category": "string",
  "price": 0.00,
  "createdAt": "ISO 8601",
  "updatedAt": "ISO 8601"
}
```

---

## Getting Started

### Prerequisites

- [Node.js](https://nodejs.org) >= 18
- [Azure CLI](https://learn.microsoft.com/cli/azure/install-azure-cli) installed and logged in
- An Azure Cosmos DB account (NoSQL API)

### Local Development

```powershell
# 1. Clone and install
git clone https://github.com/ibnehussain/my204apis.git
cd my204apis
npm install

# 2. Configure environment
cp .env.example .env
# Edit .env — set COSMOS_ENDPOINT to your Cosmos DB endpoint

# 3. Login to Azure (used by DefaultAzureCredential locally)
az login

# 4. Provision Cosmos DB (creates database + container + seeds data)
$env:COSMOS_ENDPOINT="https://<your-account>.documents.azure.com:443/"
node infra/cosmos/setup.js

# 5. Start the server
npm run dev        # development (nodemon)
npm start          # production
```

Server starts at `http://localhost:3000`.

---

## Deployment to Azure App Service

> **Full step-by-step provisioning guide:** see [AZURE_SETUP.md](AZURE_SETUP.md)

### Prerequisites

- [Azure CLI](https://learn.microsoft.com/cli/azure/install-azure-cli) 2.60+ installed
- An active Azure subscription
- [Node.js](https://nodejs.org) >= 18

Login and select your subscription before running any commands:

```bash
az login
az account set --subscription "<your-subscription-id>"
```

---

### Step 1 — Create a Resource Group

```bash
az group create \
  --name rg-my204apis \
  --location eastus
```

---

### Step 2 — Create an Azure Cosmos DB Account (NoSQL)

```bash
az cosmosdb create \
  --name cosmos-my204apis \
  --resource-group rg-my204apis \
  --kind GlobalDocumentDB \
  --default-consistency-level Session \
  --locations regionName=eastus \
  --capabilities EnableServerless
```

Save the endpoint URL — you will need it later:

```bash
COSMOS_ENDPOINT=$(az cosmosdb show \
  --name cosmos-my204apis \
  --resource-group rg-my204apis \
  --query documentEndpoint \
  --output tsv)
```

---

### Step 3 — Create an App Service Plan and Web App

```bash
# Linux B1 App Service Plan
az appservice plan create \
  --name plan-my204apis \
  --resource-group rg-my204apis \
  --sku B1 \
  --is-linux

# Node.js 20 Web App
az webapp create \
  --name app-my204apis \
  --resource-group rg-my204apis \
  --plan plan-my204apis \
  --runtime "NODE|20-lts"
```

---

### Step 4 — Enable System-Assigned Managed Identity

```bash
az webapp identity assign \
  --name app-my204apis \
  --resource-group rg-my204apis
```

Capture the principal ID for the next step:

```bash
PRINCIPAL_ID=$(az webapp identity show \
  --name app-my204apis \
  --resource-group rg-my204apis \
  --query principalId \
  --output tsv)
```

---

### Step 5 — Grant the App Service Access to Cosmos DB

Assign the **Cosmos DB Built-in Data Contributor** role — no keys required:

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

---

### Step 6 — Configure App Service Environment Variables

```bash
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

### Step 7 — Seed Cosmos DB

The setup script uses `DefaultAzureCredential`, which picks up your local Azure CLI session. Make sure you are logged in first (`az login`), then run:

```bash
# Use the endpoint captured in Step 2
export COSMOS_ENDPOINT=$COSMOS_ENDPOINT

# Create DB + container + seed data
node infra/cosmos/setup.js
```

---

### Step 8 — Configure GitHub Secrets for CI/CD

The GitHub Actions workflow uses OpenID Connect (OIDC) to authenticate to Azure — no long-lived credentials are stored. You need an **App Registration** with a federated identity credential:

```bash
# Create an app registration and capture the client ID
APP_ID=$(az ad app create --display-name "my204apis-gh-actions" --query appId --output tsv)

# Create a service principal for the app
az ad sp create --id $APP_ID

# Add a federated credential for GitHub Actions
az ad app federated-credential create \
  --id $APP_ID \
  --parameters '{
    "name": "github-oidc",
    "issuer": "https://token.actions.githubusercontent.com",
    "subject": "repo:ibnehussain/my204apis:ref:refs/heads/main",
    "audiences": ["api://AzureADTokenExchange"]
  }'

# Assign Contributor role on the subscription (or scope it to the resource group)
az role assignment create \
  --assignee $APP_ID \
  --role Contributor \
  --scope /subscriptions/$(az account show --query id --output tsv)
```

In your GitHub repository go to **Settings → Secrets and variables → Actions** and add:

| Secret | Value |
|--------|-------|
| `AZURE_CLIENT_ID` | `$APP_ID` (app registration client ID from above) |
| `AZURE_TENANT_ID` | `$(az account show --query tenantId --output tsv)` |
| `AZURE_SUBSCRIPTION_ID` | `$(az account show --query id --output tsv)` |
| `AZURE_WEBAPP_NAME` | `app-my204apis` |

---

### Step 9 — Deploy via GitHub Actions

Push to `main` — the workflow in `.github/workflows/deploy.yml` builds and deploys automatically:

```bash
git push origin main
```

Monitor the live logs:

```bash
az webapp log tail \
  --name app-my204apis \
  --resource-group rg-my204apis
```

Verify the deployment:

```bash
curl https://app-my204apis.azurewebsites.net/health
# Expected: {"status":"healthy","timestamp":"..."}
```

---

### Alternative: One-Shot Provisioning Script (PowerShell)

If you prefer, a single PowerShell script automates Steps 1–6:

```powershell
.\infra\azure\provision.ps1 `
  -ResourceGroup  "rg-my204apis" `
  -AppServiceName "app-my204apis" `
  -CosmosAccount  "cosmos-my204apis" `
  -Location       "eastus"
```

---

## APIM Configuration

1. Import `infra/apim/openapi.yaml` into your Azure APIM instance
2. Apply policies from `infra/apim/policies/`:
   - `jwt-validate.xml` — validates Azure AD Bearer tokens
   - `rate-limit.xml` — 100 requests per 60 seconds per subscription
   - `transform.xml` — strips auth header, adds security response headers, sets backend URL

Set the following **Named Values** in APIM:
- `tenant-id` — Azure AD Tenant ID
- `apim-audience` — App Registration Application ID
- `backend-app-service-url` — App Service URL (e.g. `https://app-my204apis.azurewebsites.net`)

---

## Security

- **No secrets in code or environment** — Cosmos DB access via Managed Identity only
- APIM enforces **JWT authentication** before requests reach the backend
- **Rate limiting** prevents abuse (HTTP 429 on breach)
- Security headers applied on all responses (`X-Content-Type-Options`, `X-Frame-Options`, `HSTS`)
- `COSMOS_KEY` is never used or stored anywhere

---

## Environment Variables

| Variable | Required | Description |
|----------|----------|-------------|
| `COSMOS_ENDPOINT` | Yes | Cosmos DB account endpoint URL |
| `COSMOS_DATABASE_ID` | No | Database name (default: `my204db`) |
| `COSMOS_CONTAINER_ID` | No | Container name (default: `items`) |
| `PORT` | No | Port to listen on (default: `3000`) |
| `APPLICATIONINSIGHTS_CONNECTION_STRING` | No | Enables Azure Monitor telemetry |

See [`.env.example`](.env.example) for a template.

---

## Cleanup

To delete all Azure resources when they are no longer needed:

```bash
az group delete \
  --name rg-my204apis \
  --yes \
  --no-wait
```

> ⚠️ This permanently deletes all resources in the resource group.
