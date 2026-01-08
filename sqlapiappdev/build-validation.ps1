# Build Validation Script
# Simulates the Azure Pipeline build process locally

Write-Host "Starting local build validation..." -ForegroundColor Green

# Check if .NET 8 SDK is installed
Write-Host "Checking .NET SDK version..." -ForegroundColor Yellow
$dotnetVersion = dotnet --version
if ($LASTEXITCODE -ne 0) {
    Write-Error ".NET SDK not found. Please install .NET 8 SDK."
    exit 1
}
Write-Host "Found .NET SDK version: $dotnetVersion" -ForegroundColor Green

# Check if this is .NET 8.x
if (-not $dotnetVersion.StartsWith("8.")) {
    Write-Warning "Expected .NET 8.x, but found version $dotnetVersion"
}

# Set build configuration
$buildConfiguration = "Release"
$outputDir = "./build-output"

Write-Host "Build Configuration: $buildConfiguration" -ForegroundColor Yellow

# Clean previous build output
if (Test-Path $outputDir) {
    Write-Host "Cleaning previous build output..." -ForegroundColor Yellow
    Remove-Item -Recurse -Force $outputDir
}
New-Item -ItemType Directory -Path $outputDir | Out-Null

# Restore packages
Write-Host "Restoring NuGet packages..." -ForegroundColor Yellow
dotnet restore
if ($LASTEXITCODE -ne 0) {
    Write-Error "Package restore failed"
    exit 1
}
Write-Host "✓ Package restore completed" -ForegroundColor Green

# Build solution
Write-Host "Building solution in $buildConfiguration mode..." -ForegroundColor Yellow
dotnet build --configuration $buildConfiguration --no-restore
if ($LASTEXITCODE -ne 0) {
    Write-Error "Build failed"
    exit 1
}
Write-Host "✓ Solution build completed" -ForegroundColor Green

# Publish Demo.API
Write-Host "Publishing Demo.API..." -ForegroundColor Yellow
$apiOutput = Join-Path $outputDir "api"
dotnet publish Demo.API/Demo.API.csproj --configuration $buildConfiguration --output $apiOutput --no-build
if ($LASTEXITCODE -ne 0) {
    Write-Error "Demo.API publish failed"
    exit 1
}
Write-Host "✓ Demo.API published to: $apiOutput" -ForegroundColor Green

# Publish Demo.Web
Write-Host "Publishing Demo.Web..." -ForegroundColor Yellow
$webOutput = Join-Path $outputDir "web"
dotnet publish Demo.Web/Demo.Web.csproj --configuration $buildConfiguration --output $webOutput --no-build
if ($LASTEXITCODE -ne 0) {
    Write-Error "Demo.Web publish failed"
    exit 1
}
Write-Host "✓ Demo.Web published to: $webOutput" -ForegroundColor Green

# Display artifact information
Write-Host "`nBuild artifacts created:" -ForegroundColor Cyan
Write-Host "API Artifact:" -ForegroundColor White
Get-ChildItem -Path $apiOutput | Select-Object Name, Length, LastWriteTime | Format-Table -AutoSize

Write-Host "Web Artifact:" -ForegroundColor White
Get-ChildItem -Path $webOutput | Select-Object Name, Length, LastWriteTime | Format-Table -AutoSize

Write-Host "`n✓ Local build validation completed successfully!" -ForegroundColor Green
Write-Host "Build outputs are available in: $outputDir" -ForegroundColor Yellow

# Optional: Create zip files like Azure Pipelines would
Write-Host "`nCreating zip archives..." -ForegroundColor Yellow
$apiZip = Join-Path $outputDir "demo-api.zip"
$webZip = Join-Path $outputDir "demo-web.zip"

Compress-Archive -Path "$apiOutput\*" -DestinationPath $apiZip -Force
Compress-Archive -Path "$webOutput\*" -DestinationPath $webZip -Force

Write-Host "✓ API artifact: $apiZip" -ForegroundColor Green
Write-Host "✓ Web artifact: $webZip" -ForegroundColor Green