using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace Demo.API.Models;

/// <summary>
/// Data Transfer Object for Item creation requests
/// </summary>
public class CreateItemRequest
{
    /// <summary>
    /// Display name of the item (required)
    /// </summary>
    [Required(ErrorMessage = "Name is required")]
    [StringLength(100, MinimumLength = 1, ErrorMessage = "Name must be between 1 and 100 characters")]
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Category classification of the item (required)
    /// </summary>
    [Required(ErrorMessage = "Category is required")]
    [StringLength(50, MinimumLength = 1, ErrorMessage = "Category must be between 1 and 50 characters")]
    [JsonPropertyName("category")]
    public string Category { get; set; } = string.Empty;
}

/// <summary>
/// Data Transfer Object for Item update requests
/// </summary>
public class UpdateItemRequest
{
    /// <summary>
    /// Display name of the item
    /// </summary>
    [StringLength(100, MinimumLength = 1, ErrorMessage = "Name must be between 1 and 100 characters")]
    [JsonPropertyName("name")]
    public string? Name { get; set; }

    /// <summary>
    /// Category classification of the item
    /// Note: Changing category in Cosmos DB may require special handling due to partition key changes
    /// </summary>
    [StringLength(50, MinimumLength = 1, ErrorMessage = "Category must be between 1 and 50 characters")]
    [JsonPropertyName("category")]
    public string? Category { get; set; }
}

/// <summary>
/// Data Transfer Object for Item responses
/// </summary>
public class ItemResponse
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;

    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("category")]
    public string Category { get; set; } = string.Empty;

    [JsonPropertyName("createdAt")]
    public DateTime CreatedAt { get; set; }

    [JsonPropertyName("updatedAt")]
    public DateTime UpdatedAt { get; set; }

    /// <summary>
    /// Convert from internal Item model to response DTO
    /// </summary>
    public static ItemResponse FromItem(Item item)
    {
        return new ItemResponse
        {
            Id = item.Id,
            Name = item.Name,
            Category = item.Category,
            CreatedAt = item.CreatedAt,
            UpdatedAt = item.UpdatedAt
        };
    }
}