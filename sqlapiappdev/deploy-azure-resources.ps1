# Azure Resource Deployment Script
# This script creates all necessary Azure resources for the Demo ASP.NET Core solution

param(
    [Parameter(Mandatory=$true)]
    [string]$SubscriptionId,
    
    [Parameter(Mandatory=$true)]
    [string]$ResourceGroupName,
    
    [Parameter(Mandatory=$true)]
    [string]$Location,
    
    [Parameter(Mandatory=$true)]
    [string]$EnvironmentPrefix,
    
    [Parameter(Mandatory=$false)]
    [string]$CosmosAccountName = "$EnvironmentPrefix-demo-cosmos"
)

# Login to Azure (if not already logged in)
Write-Host "Logging into Azure..." -ForegroundColor Green
$context = Get-AzContext
if (-not $context) {
    Connect-AzAccount
}

# Select the subscription
Write-Host "Setting subscription to $SubscriptionId..." -ForegroundColor Green
Set-AzContext -SubscriptionId $SubscriptionId

# Create Resource Group if it doesn't exist
Write-Host "Creating resource group '$ResourceGroupName'..." -ForegroundColor Green
$rg = Get-AzResourceGroup -Name $ResourceGroupName -ErrorAction SilentlyContinue
if (-not $rg) {
    New-AzResourceGroup -Name $ResourceGroupName -Location $Location
    Write-Host "Resource group '$ResourceGroupName' created successfully." -ForegroundColor Yellow
} else {
    Write-Host "Resource group '$ResourceGroupName' already exists." -ForegroundColor Yellow
}

# Create App Service Plan
$appServicePlanName = "$EnvironmentPrefix-demo-asp"
Write-Host "Creating App Service Plan '$appServicePlanName'..." -ForegroundColor Green

$appServicePlan = Get-AzAppServicePlan -ResourceGroupName $ResourceGroupName -Name $appServicePlanName -ErrorAction SilentlyContinue
if (-not $appServicePlan) {
    $appServicePlan = New-AzAppServicePlan -ResourceGroupName $ResourceGroupName -Name $appServicePlanName -Location $Location -Tier "B1" -NumberofWorkers 1
    Write-Host "App Service Plan '$appServicePlanName' created successfully." -ForegroundColor Yellow
} else {
    Write-Host "App Service Plan '$appServicePlanName' already exists." -ForegroundColor Yellow
}

# Create App Service for API
$apiAppName = "$EnvironmentPrefix-demo-api"
Write-Host "Creating App Service for API '$apiAppName'..." -ForegroundColor Green

$apiApp = Get-AzWebApp -ResourceGroupName $ResourceGroupName -Name $apiAppName -ErrorAction SilentlyContinue
if (-not $apiApp) {
    $apiApp = New-AzWebApp -ResourceGroupName $ResourceGroupName -Name $apiAppName -Location $Location -AppServicePlan $appServicePlanName
    Write-Host "API App Service '$apiAppName' created successfully." -ForegroundColor Yellow
} else {
    Write-Host "API App Service '$apiAppName' already exists." -ForegroundColor Yellow
}

# Create App Service for Web
$webAppName = "$EnvironmentPrefix-demo-web"
Write-Host "Creating App Service for Web '$webAppName'..." -ForegroundColor Green

$webApp = Get-AzWebApp -ResourceGroupName $ResourceGroupName -Name $webAppName -ErrorAction SilentlyContinue
if (-not $webApp) {
    $webApp = New-AzWebApp -ResourceGroupName $ResourceGroupName -Name $webAppName -Location $Location -AppServicePlan $appServicePlanName
    Write-Host "Web App Service '$webAppName' created successfully." -ForegroundColor Yellow
} else {
    Write-Host "Web App Service '$webAppName' already exists." -ForegroundColor Yellow
}

# Create Cosmos DB Account
Write-Host "Creating Cosmos DB Account '$CosmosAccountName'..." -ForegroundColor Green

$cosmosAccount = Get-AzCosmosDBAccount -ResourceGroupName $ResourceGroupName -Name $CosmosAccountName -ErrorAction SilentlyContinue
if (-not $cosmosAccount) {
    $cosmosAccount = New-AzCosmosDBAccount -ResourceGroupName $ResourceGroupName -Name $CosmosAccountName -Location $Location -DefaultConsistencyLevel "Session" -EnableFreeTier $false
    Write-Host "Cosmos DB Account '$CosmosAccountName' created successfully." -ForegroundColor Yellow
} else {
    Write-Host "Cosmos DB Account '$CosmosAccountName' already exists." -ForegroundColor Yellow
}

# Create Cosmos DB Database and Container
Write-Host "Creating Cosmos DB Database and Container..." -ForegroundColor Green

$databaseName = "DemoDatabase"
$containerName = "Items"

# Create database
$database = Get-AzCosmosDBSqlDatabase -ResourceGroupName $ResourceGroupName -AccountName $CosmosAccountName -Name $databaseName -ErrorAction SilentlyContinue
if (-not $database) {
    New-AzCosmosDBSqlDatabase -ResourceGroupName $ResourceGroupName -AccountName $CosmosAccountName -Name $databaseName
    Write-Host "Cosmos DB Database '$databaseName' created successfully." -ForegroundColor Yellow
} else {
    Write-Host "Cosmos DB Database '$databaseName' already exists." -ForegroundColor Yellow
}

# Create container
$container = Get-AzCosmosDBSqlContainer -ResourceGroupName $ResourceGroupName -AccountName $CosmosAccountName -DatabaseName $databaseName -Name $containerName -ErrorAction SilentlyContinue
if (-not $container) {
    New-AzCosmosDBSqlContainer -ResourceGroupName $ResourceGroupName -AccountName $CosmosAccountName -DatabaseName $databaseName -Name $containerName -PartitionKeyPath "/category" -Throughput 400
    Write-Host "Cosmos DB Container '$containerName' created successfully." -ForegroundColor Yellow
} else {
    Write-Host "Cosmos DB Container '$containerName' already exists." -ForegroundColor Yellow
}

# Configure App Service Settings
Write-Host "Configuring App Service Settings..." -ForegroundColor Green

# Get Cosmos DB connection information
$cosmosKeys = Get-AzCosmosDBAccountKey -ResourceGroupName $ResourceGroupName -Name $CosmosAccountName
$cosmosEndpoint = "https://$CosmosAccountName.documents.azure.com:443/"

# Configure API App Settings
$apiSettings = @{
    "ASPNETCORE_ENVIRONMENT" = "Production"
    "Cosmos__Endpoint" = $cosmosEndpoint
    "Cosmos__Key" = $cosmosKeys.PrimaryMasterKey
    "Cosmos__DatabaseName" = $databaseName
}

foreach ($setting in $apiSettings.GetEnumerator()) {
    Set-AzWebAppSetting -ResourceGroupName $ResourceGroupName -Name $apiAppName -AppSetting @{$setting.Name = $setting.Value} | Out-Null
}

Write-Host "API App settings configured successfully." -ForegroundColor Yellow

# Configure Web App Settings
$webSettings = @{
    "ASPNETCORE_ENVIRONMENT" = "Production"
    "ApiBaseUrl" = "https://$apiAppName.azurewebsites.net"
}

foreach ($setting in $webSettings.GetEnumerator()) {
    Set-AzWebAppSetting -ResourceGroupName $ResourceGroupName -Name $webAppName -AppSetting @{$setting.Name = $setting.Value} | Out-Null
}

Write-Host "Web App settings configured successfully." -ForegroundColor Yellow

# Output summary
Write-Host "`n=== DEPLOYMENT SUMMARY ===" -ForegroundColor Green
Write-Host "Resource Group: $ResourceGroupName" -ForegroundColor White
Write-Host "App Service Plan: $appServicePlanName" -ForegroundColor White
Write-Host "API App Service: $apiAppName" -ForegroundColor White
Write-Host "API URL: https://$apiAppName.azurewebsites.net" -ForegroundColor White
Write-Host "Web App Service: $webAppName" -ForegroundColor White
Write-Host "Web URL: https://$webAppName.azurewebsites.net" -ForegroundColor White
Write-Host "Cosmos DB Account: $CosmosAccountName" -ForegroundColor White
Write-Host "Cosmos DB Endpoint: $cosmosEndpoint" -ForegroundColor White
Write-Host "Database Name: $databaseName" -ForegroundColor White
Write-Host "Container Name: $containerName" -ForegroundColor White
Write-Host "`nDeployment completed successfully!" -ForegroundColor Green

# Output pipeline variables for Azure DevOps
Write-Host "`n=== AZURE DEVOPS PIPELINE VARIABLES ===" -ForegroundColor Green
Write-Host "Set these variables in your Azure DevOps pipeline:" -ForegroundColor White
Write-Host "apiAppServiceName: $apiAppName" -ForegroundColor Yellow
Write-Host "webAppServiceName: $webAppName" -ForegroundColor Yellow
Write-Host "resourceGroupName: $ResourceGroupName" -ForegroundColor Yellow
Write-Host "cosmosEndpoint: $cosmosEndpoint" -ForegroundColor Yellow
Write-Host "cosmosKey: [Set as secret variable]" -ForegroundColor Yellow