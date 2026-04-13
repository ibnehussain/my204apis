# my204apis

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

## Deployment to Azure

### Step 1 — Provision Azure resources

```powershell
.\infra\azure\provision.ps1 `
  -ResourceGroup  "rg-my204apis" `
  -AppServiceName "app-my204apis" `
  -CosmosAccount  "cosmos-my204apis" `
  -Location       "eastus"
```

This script will:
- Create a Resource Group, App Service Plan, and Web App
- Enable **System-Assigned Managed Identity** on the App Service
- Assign the **Cosmos DB Built-in Data Contributor** role to the identity
- Set `COSMOS_ENDPOINT` and other app settings — **no keys stored**

### Step 2 — Add GitHub Secrets

| Secret | Value |
|--------|-------|
| `AZURE_CLIENT_ID` | Service principal / app registration client ID |
| `AZURE_TENANT_ID` | Azure AD Tenant ID |
| `AZURE_SUBSCRIPTION_ID` | Azure Subscription ID |
| `AZURE_WEBAPP_NAME` | `app-my204apis` |

### Step 3 — Deploy

Push to `main` — GitHub Actions deploys automatically.

```powershell
git push origin main
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
