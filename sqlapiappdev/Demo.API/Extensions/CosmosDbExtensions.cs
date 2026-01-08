using Microsoft.Azure.Cosmos;
using Demo.API.Models;

namespace Demo.API.Extensions;

/// <summary>
/// Extension methods for Cosmos DB service configuration
/// </summary>
public static class CosmosDbExtensions
{
    /// <summary>
    /// Ensures the Cosmos DB database and container exist
    /// Should be called during application startup
    /// </summary>
    public static async Task EnsureCosmosDbCreatedAsync(this IServiceProvider services)
    {
        try
        {
            var cosmosClient = services.GetRequiredService<CosmosClient>();
            var settings = services.GetRequiredService<IConfiguration>()
                .GetSection("Cosmos").Get<CosmosDbSettings>()
                ?? throw new InvalidOperationException("Cosmos configuration is missing");

            // Create database if it doesn't exist
            var databaseResponse = await cosmosClient.CreateDatabaseIfNotExistsAsync(
                settings.DatabaseName,
                throughput: 400); // Minimum RU/s for development

            // Create container if it doesn't exist
            var containerProperties = new ContainerProperties
            {
                Id = settings.ContainerName,
                PartitionKeyPath = settings.PartitionKeyPath,
                IndexingPolicy = new IndexingPolicy
                {
                    IndexingMode = IndexingMode.Consistent,
                    Automatic = true,
                    IncludedPaths =
                    {
                        new IncludedPath { Path = "/*" }
                    }
                }
            };

            await databaseResponse.Database.CreateContainerIfNotExistsAsync(
                containerProperties,
                throughput: 400);

            Console.WriteLine($"Cosmos DB setup completed: Database='{settings.DatabaseName}', Container='{settings.ContainerName}'");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error setting up Cosmos DB: {ex.Message}");
            // In production, you might want to throw here or handle differently
            // For development with emulator, we'll continue
        }
    }
}