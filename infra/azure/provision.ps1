<#
.SYNOPSIS
  Provisions Azure App Service with a System-Assigned Managed Identity
  and grants it the Cosmos DB Built-in Data Contributor role.

.DESCRIPTION
  Run this once after creating the App Service and Cosmos DB account.
  Requires: Azure CLI (az) installed and logged in (az login).

.EJEMPLO
  .\infra\azure\provision.ps1 `
    -ResourceGroup  "rg-my204apis" `
    -AppServiceName "app-my204apis" `
    -CosmosAccount  "cosmos-my204apis" `
    -Location       "eastus"
#>

param(
  [Parameter(Mandatory)] [string] $ResourceGroup,
  [Parameter(Mandatory)] [string] $AppServiceName,
  [Parameter(Mandatory)] [string] $CosmosAccount,
  [string] $Location = "eastus",
  [string] $NodeVersion = "NODE|20-lts"
)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

Write-Host "`n=== Step 1: Create Resource Group (if not exists) ===" -ForegroundColor Cyan
az group create --name $ResourceGroup --location $Location --output none
Write-Host "  Resource group: $ResourceGroup"

Write-Host "`n=== Step 2: Create App Service Plan ===" -ForegroundColor Cyan
$PlanName = "$AppServiceName-plan"
az appservice plan create `
  --name $PlanName `
  --resource-group $ResourceGroup `
  --sku B1 `
  --is-linux `
  --output none
Write-Host "  App Service Plan: $PlanName (B1 Linux)"

Write-Host "`n=== Step 3: Create Web App ===" -ForegroundColor Cyan
az webapp create `
  --name $AppServiceName `
  --resource-group $ResourceGroup `
  --plan $PlanName `
  --runtime $NodeVersion `
  --output none
Write-Host "  Web App: $AppServiceName"

Write-Host "`n=== Step 4: Enable System-Assigned Managed Identity ===" -ForegroundColor Cyan
$IdentityJson = az webapp identity assign `
  --name $AppServiceName `
  --resource-group $ResourceGroup `
  --output json | ConvertFrom-Json

$PrincipalId = $IdentityJson.principalId
Write-Host "  Managed Identity Principal ID: $PrincipalId"

Write-Host "`n=== Step 5: Get Cosmos DB account resource ID ===" -ForegroundColor Cyan
$CosmosResourceId = az cosmosdb show `
  --name $CosmosAccount `
  --resource-group $ResourceGroup `
  --query id `
  --output tsv
Write-Host "  Cosmos DB Resource ID: $CosmosResourceId"

Write-Host "`n=== Step 6: Assign Cosmos DB Built-in Data Contributor role ===" -ForegroundColor Cyan
# Role definition ID for "Cosmos DB Built-in Data Contributor"
$CosmosRoleId = "00000000-0000-0000-0000-000000000002"

az cosmosdb sql role assignment create `
  --account-name $CosmosAccount `
  --resource-group $ResourceGroup `
  --role-definition-id $CosmosRoleId `
  --principal-id $PrincipalId `
  --scope $CosmosResourceId `
  --output none
Write-Host "  Role assigned: Cosmos DB Built-in Data Contributor"

Write-Host "`n=== Step 7: Configure App Service environment variables ===" -ForegroundColor Cyan
$CosmosEndpoint = az cosmosdb show `
  --name $CosmosAccount `
  --resource-group $ResourceGroup `
  --query documentEndpoint `
  --output tsv

az webapp config appsettings set `
  --name $AppServiceName `
  --resource-group $ResourceGroup `
  --settings `
    COSMOS_ENDPOINT=$CosmosEndpoint `
    COSMOS_DATABASE_ID=my204db `
    COSMOS_CONTAINER_ID=items `
    NODE_ENV=production `
  --output none
Write-Host "  App settings configured (no keys stored — Managed Identity only)"

Write-Host "`n=== Step 8: Set deployment source to local git / GitHub ===" -ForegroundColor Cyan
az webapp deployment source config-local-git `
  --name $AppServiceName `
  --resource-group $ResourceGroup `
  --output none
Write-Host "  Local git deployment endpoint configured"

Write-Host "`n========================================" -ForegroundColor Green
Write-Host "  Provisioning complete!" -ForegroundColor Green
Write-Host "  App URL : https://$AppServiceName.azurewebsites.net" -ForegroundColor Green
Write-Host "  Health  : https://$AppServiceName.azurewebsites.net/health" -ForegroundColor Green
Write-Host "========================================`n" -ForegroundColor Green

Write-Host "Next steps:" -ForegroundColor Yellow
Write-Host "  1. Add GitHub secrets: AZURE_CLIENT_ID, AZURE_TENANT_ID, AZURE_SUBSCRIPTION_ID, AZURE_WEBAPP_NAME"
Write-Host "  2. Push to main branch — GitHub Actions will deploy automatically"
Write-Host "  3. Run: node infra/cosmos/setup.js  (to create DB + container + seed data)"
