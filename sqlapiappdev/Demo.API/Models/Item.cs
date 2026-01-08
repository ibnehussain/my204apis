using System.Text.Json.Serialization;

namespace Demo.API.Models;

/// <summary>
/// Represents an Item entity compatible with Azure Cosmos DB SQL API
/// </summary>
public class Item
{
    /// <summary>
    /// Unique identifier for the item (required for Cosmos DB)
    /// </summary>
    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;

    /// <summary>
    /// Display name of the item
    /// </summary>
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Category classification of the item (good candidate for partition key)
    /// </summary>
    [JsonPropertyName("category")]
    public string Category { get; set; } = string.Empty;

    /// <summary>
    /// Partition key property for efficient querying and scaling
    /// Maps to category for logical partitioning
    /// </summary>
    [JsonIgnore]
    public string PartitionKey => Category;

    /// <summary>
    /// Optional: Track when the item was created (useful for queries and filtering)
    /// </summary>
    [JsonPropertyName("createdAt")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Optional: Track when the item was last modified
    /// </summary>
    [JsonPropertyName("updatedAt")]
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Optional: Soft delete flag (recommended over hard deletes in Cosmos DB)
    /// </summary>
    [JsonPropertyName("isDeleted")]
    public bool IsDeleted { get; set; } = false;
}