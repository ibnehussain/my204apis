using Demo.Web.Models;
using System.Text.Json;

namespace Demo.Web.Services;

/// <summary>
/// Service for handling Item API operations
/// </summary>
public interface IItemService
{
    /// <summary>
    /// Get all items from the API
    /// </summary>
    /// <returns>List of items or empty list on error</returns>
    Task<List<Item>> GetAllItemsAsync();

    /// <summary>
    /// Get items by category from the API
    /// </summary>
    /// <param name="category">Category to filter by</param>
    /// <returns>List of items in the specified category</returns>
    Task<List<Item>> GetItemsByCategoryAsync(string category);

    /// <summary>
    /// Get a specific item by ID and category
    /// </summary>
    /// <param name="id">Item ID</param>
    /// <param name="category">Item category</param>
    /// <returns>The item if found, null otherwise</returns>
    Task<Item?> GetItemAsync(string id, string category);
}

/// <summary>
/// Implementation of ItemService using HttpClient to call Demo.API
/// </summary>
public class ItemService : IItemService
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<ItemService> _logger;
    private readonly JsonSerializerOptions _jsonOptions;

    public ItemService(HttpClient httpClient, ILogger<ItemService> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
        
        // Configure JSON serialization options to match API response
        _jsonOptions = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };
    }

    /// <summary>
    /// Get all items from the API
    /// </summary>
    public async Task<List<Item>> GetAllItemsAsync()
    {
        try
        {
            _logger.LogInformation("Fetching all items from API");
            
            var response = await _httpClient.GetAsync("/api/items");
            
            if (response.IsSuccessStatusCode)
            {
                var jsonContent = await response.Content.ReadAsStringAsync();
                
                if (string.IsNullOrWhiteSpace(jsonContent))
                {
                    _logger.LogWarning("API returned empty content for all items");
                    return new List<Item>();
                }

                var items = JsonSerializer.Deserialize<List<Item>>(jsonContent, _jsonOptions) ?? new List<Item>();
                
                _logger.LogInformation("Successfully retrieved {Count} items from API", items.Count);
                return items;
            }
            else
            {
                _logger.LogWarning("API request failed with status code: {StatusCode} - {ReasonPhrase}", 
                    response.StatusCode, response.ReasonPhrase);
                return new List<Item>();
            }
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "Network error occurred while fetching items from API");
            return new List<Item>();
        }
        catch (JsonException ex)
        {
            _logger.LogError(ex, "JSON deserialization error occurred while parsing items response");
            return new List<Item>();
        }
        catch (TaskCanceledException ex)
        {
            _logger.LogError(ex, "Request timeout occurred while fetching items from API");
            return new List<Item>();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error occurred while fetching items from API");
            return new List<Item>();
        }
    }

    /// <summary>
    /// Get items by category from the API
    /// </summary>
    public async Task<List<Item>> GetItemsByCategoryAsync(string category)
    {
        if (string.IsNullOrWhiteSpace(category))
        {
            _logger.LogWarning("Category parameter is null or empty");
            return new List<Item>();
        }

        try
        {
            _logger.LogInformation("Fetching items for category: {Category}", category);
            
            var encodedCategory = Uri.EscapeDataString(category);
            var response = await _httpClient.GetAsync($"/api/items/category/{encodedCategory}");
            
            if (response.IsSuccessStatusCode)
            {
                var jsonContent = await response.Content.ReadAsStringAsync();
                
                if (string.IsNullOrWhiteSpace(jsonContent))
                {
                    _logger.LogWarning("API returned empty content for category: {Category}", category);
                    return new List<Item>();
                }

                var items = JsonSerializer.Deserialize<List<Item>>(jsonContent, _jsonOptions) ?? new List<Item>();
                
                _logger.LogInformation("Successfully retrieved {Count} items for category: {Category}", items.Count, category);
                return items;
            }
            else
            {
                _logger.LogWarning("API request failed for category {Category} with status code: {StatusCode} - {ReasonPhrase}", 
                    category, response.StatusCode, response.ReasonPhrase);
                return new List<Item>();
            }
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "Network error occurred while fetching items for category: {Category}", category);
            return new List<Item>();
        }
        catch (JsonException ex)
        {
            _logger.LogError(ex, "JSON deserialization error occurred while parsing items response for category: {Category}", category);
            return new List<Item>();
        }
        catch (TaskCanceledException ex)
        {
            _logger.LogError(ex, "Request timeout occurred while fetching items for category: {Category}", category);
            return new List<Item>();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error occurred while fetching items for category: {Category}", category);
            return new List<Item>();
        }
    }

    /// <summary>
    /// Get a specific item by ID and category
    /// </summary>
    public async Task<Item?> GetItemAsync(string id, string category)
    {
        if (string.IsNullOrWhiteSpace(id) || string.IsNullOrWhiteSpace(category))
        {
            _logger.LogWarning("ID or category parameter is null or empty");
            return null;
        }

        try
        {
            _logger.LogInformation("Fetching item: {Id} in category: {Category}", id, category);
            
            var encodedId = Uri.EscapeDataString(id);
            var encodedCategory = Uri.EscapeDataString(category);
            var response = await _httpClient.GetAsync($"/api/items/{encodedId}/category/{encodedCategory}");
            
            if (response.IsSuccessStatusCode)
            {
                var jsonContent = await response.Content.ReadAsStringAsync();
                
                if (string.IsNullOrWhiteSpace(jsonContent))
                {
                    _logger.LogWarning("API returned empty content for item: {Id} in category: {Category}", id, category);
                    return null;
                }

                var item = JsonSerializer.Deserialize<Item>(jsonContent, _jsonOptions);
                
                _logger.LogInformation("Successfully retrieved item: {Id} in category: {Category}", id, category);
                return item;
            }
            else if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
            {
                _logger.LogInformation("Item not found: {Id} in category: {Category}", id, category);
                return null;
            }
            else
            {
                _logger.LogWarning("API request failed for item {Id} in category {Category} with status code: {StatusCode} - {ReasonPhrase}", 
                    id, category, response.StatusCode, response.ReasonPhrase);
                return null;
            }
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "Network error occurred while fetching item: {Id} in category: {Category}", id, category);
            return null;
        }
        catch (JsonException ex)
        {
            _logger.LogError(ex, "JSON deserialization error occurred while parsing item response for: {Id} in category: {Category}", id, category);
            return null;
        }
        catch (TaskCanceledException ex)
        {
            _logger.LogError(ex, "Request timeout occurred while fetching item: {Id} in category: {Category}", id, category);
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error occurred while fetching item: {Id} in category: {Category}", id, category);
            return null;
        }
    }
}