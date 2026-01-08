using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Cosmos;
using Demo.API.Models;
using Microsoft.Extensions.Options;
using System.Net;

namespace Demo.API.Controllers;

/// <summary>
/// API Controller for managing Items in Azure Cosmos DB
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
[ProducesResponseType(StatusCodes.Status500InternalServerError)]
public class ItemsController : ControllerBase
{
    private readonly CosmosClient _cosmosClient;
    private readonly CosmosDbSettings _cosmosDbSettings;
    private readonly Container _container;
    private readonly ILogger<ItemsController> _logger;

    public ItemsController(
        CosmosClient cosmosClient,
        IOptions<CosmosDbSettings> cosmosDbSettings,
        ILogger<ItemsController> logger)
    {
        _cosmosClient = cosmosClient;
        _cosmosDbSettings = cosmosDbSettings.Value;
        _logger = logger;
        
        // Get reference to the container
        _container = _cosmosClient.GetContainer(_cosmosDbSettings.DatabaseName, _cosmosDbSettings.ContainerName);
    }

    /// <summary>
    /// Get all items from the Cosmos DB container
    /// </summary>
    /// <remarks>
    /// Sample request:
    ///
    ///     GET /api/items
    ///
    /// </remarks>
    /// <returns>List of all non-deleted items</returns>
    /// <response code="200">Returns the list of items</response>
    /// <response code="500">Internal server error occurred</response>
    [HttpGet]
    [ProducesResponseType(typeof(IEnumerable<ItemResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<IEnumerable<ItemResponse>>> GetItems()
    {
        _logger.LogInformation("Starting request to get all items");
        var stopwatch = System.Diagnostics.Stopwatch.StartNew();
        
        var items = new List<Item>();
        
        // Query all items (cross-partition query)
        var query = "SELECT * FROM c WHERE c.isDeleted = false";
        using var feedIterator = _container.GetItemQueryIterator<Item>(query);

        while (feedIterator.HasMoreResults)
        {
            var response = await feedIterator.ReadNextAsync();
            items.AddRange(response);
            
            _logger.LogDebug("Retrieved {Count} items from Cosmos DB. Request charge: {RequestCharge} RU", 
                response.Count, response.RequestCharge);
        }

        var itemResponses = items.Select(ItemResponse.FromItem);
        
        stopwatch.Stop();
        _logger.LogInformation("Successfully retrieved {Count} items from Cosmos DB in {ElapsedMs}ms", 
            items.Count, stopwatch.ElapsedMilliseconds);
        return Ok(itemResponses);
    }

    /// <summary>
    /// Get items by category (efficient partition-scoped query)
    /// </summary>
    /// <remarks>
    /// Sample request:
    ///
    ///     GET /api/items/category/electronics
    ///
    /// </remarks>
    /// <param name="category">The category to filter by (e.g., electronics, books, clothing)</param>
    /// <returns>List of items in the specified category</returns>
    /// <response code="200">Returns the list of items for the specified category</response>
    /// <response code="400">Category parameter is missing or invalid</response>
    /// <response code="500">Internal server error occurred</response>
    [HttpGet("category/{category}")]
    [ProducesResponseType(typeof(IEnumerable<ItemResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<IEnumerable<ItemResponse>>> GetItemsByCategory(string category)
    {
        _logger.LogInformation("Starting request to get items for category '{Category}'", category);
        var stopwatch = System.Diagnostics.Stopwatch.StartNew();
        
        if (string.IsNullOrWhiteSpace(category))
        {
            stopwatch.Stop();
            _logger.LogWarning("GetItemsByCategory request failed - invalid category parameter. Request completed in {ElapsedMs}ms", stopwatch.ElapsedMilliseconds);
            throw new ArgumentException("Category cannot be empty", nameof(category));
        }

        var items = new List<Item>();
        
        // Efficient partition-scoped query
        var query = "SELECT * FROM c WHERE c.category = @category AND c.isDeleted = false";
        var queryDefinition = new QueryDefinition(query)
            .WithParameter("@category", category);

        // Use partition key for efficient querying
        var partitionKey = new PartitionKey(category);
        var queryRequestOptions = new QueryRequestOptions
        {
            PartitionKey = partitionKey
        };

        using var feedIterator = _container.GetItemQueryIterator<Item>(queryDefinition, requestOptions: queryRequestOptions);

        while (feedIterator.HasMoreResults)
        {
            var response = await feedIterator.ReadNextAsync();
            items.AddRange(response);
            
            _logger.LogDebug("Retrieved {Count} items for category '{Category}'. Request charge: {RequestCharge} RU", 
                response.Count, category, response.RequestCharge);
        }

        var itemResponses = items.Select(ItemResponse.FromItem);
        
        stopwatch.Stop();
        _logger.LogInformation("Successfully retrieved {Count} items for category '{Category}' in {ElapsedMs}ms", 
            items.Count, category, stopwatch.ElapsedMilliseconds);
        return Ok(itemResponses);
    }

    /// <summary>
    /// Get a specific item by ID and category
    /// </summary>
    /// <param name="id">Item ID</param>
    /// <param name="category">Item category (partition key)</param>
    /// <returns>The requested item</returns>
    [HttpGet("{id}/category/{category}")]
    [ProducesResponseType(typeof(ItemResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<ItemResponse>> GetItem(string id, string category)
    {
        _logger.LogInformation("Starting request to get item '{Id}' from category '{Category}'", id, category);
        var stopwatch = System.Diagnostics.Stopwatch.StartNew();
        
        if (string.IsNullOrWhiteSpace(id) || string.IsNullOrWhiteSpace(category))
        {
            stopwatch.Stop();
            _logger.LogWarning("GetItem request failed - invalid parameters (ID: '{Id}', Category: '{Category}'). Request completed in {ElapsedMs}ms", id, category, stopwatch.ElapsedMilliseconds);
            throw new ArgumentException("ID and category are required");
        }

        var partitionKey = new PartitionKey(category);
        var response = await _container.ReadItemAsync<Item>(id, partitionKey);
        
        if (response.Resource.IsDeleted)
        {
            stopwatch.Stop();
            _logger.LogWarning("Item '{Id}' in category '{Category}' is marked as deleted. Request completed in {ElapsedMs}ms", id, category, stopwatch.ElapsedMilliseconds);
            throw new InvalidOperationException($"Item with ID '{id}' in category '{category}' was not found");
        }

        stopwatch.Stop();
        _logger.LogInformation("Successfully retrieved item '{Id}' from category '{Category}'. Request charge: {RequestCharge} RU, completed in {ElapsedMs}ms", 
            id, category, response.RequestCharge, stopwatch.ElapsedMilliseconds);
        
        return Ok(ItemResponse.FromItem(response.Resource));
    }
}