namespace Demo.API.Models;

/// <summary>
/// Configuration settings for Azure Cosmos DB
/// </summary>
public class CosmosDbSettings
{
    /// <summary>
    /// Cosmos DB account endpoint URL
    /// </summary>
    public string Endpoint { get; set; } = string.Empty;

    /// <summary>
    /// Cosmos DB account key (primary or secondary)
    /// </summary>
    public string Key { get; set; } = string.Empty;

    /// <summary>
    /// Name of the Cosmos DB database
    /// </summary>
    public string DatabaseName { get; set; } = string.Empty;

    /// <summary>
    /// Name of the container for storing Items
    /// </summary>
    public string ContainerName { get; set; } = string.Empty;

    /// <summary>
    /// Partition key path for the container
    /// </summary>
    public string PartitionKeyPath { get; set; } = "/category";
}