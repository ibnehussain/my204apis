using Microsoft.AspNetCore.Mvc;
using Demo.Web.Services;
using Demo.Web.Models;

namespace Demo.Web.Controllers;

public class ItemsController : Controller
{
    private readonly IItemService _itemService;
    private readonly ILogger<ItemsController> _logger;

    public ItemsController(IItemService itemService, ILogger<ItemsController> logger)
    {
        _itemService = itemService;
        _logger = logger;
    }

    /// <summary>
    /// Display all items
    /// </summary>
    public async Task<IActionResult> Index()
    {
        try
        {
            var items = await _itemService.GetAllItemsAsync();
            return View(items);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred while fetching items for display");
            ViewBag.Error = "Unable to load items at this time. Please try again later.";
            return View(new List<Item>());
        }
    }

    /// <summary>
    /// Display items by category
    /// </summary>
    /// <param name="category">Category to filter by</param>
    public async Task<IActionResult> Category(string category)
    {
        if (string.IsNullOrWhiteSpace(category))
        {
            return RedirectToAction(nameof(Index));
        }

        try
        {
            var items = await _itemService.GetItemsByCategoryAsync(category);
            ViewBag.Category = category;
            return View("Index", items);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred while fetching items for category: {Category}", category);
            ViewBag.Error = $"Unable to load items for category '{category}' at this time. Please try again later.";
            return View("Index", new List<Item>());
        }
    }

    /// <summary>
    /// Display a specific item
    /// </summary>
    /// <param name="id">Item ID</param>
    /// <param name="category">Item category</param>
    public async Task<IActionResult> Details(string id, string category)
    {
        if (string.IsNullOrWhiteSpace(id) || string.IsNullOrWhiteSpace(category))
        {
            return RedirectToAction(nameof(Index));
        }

        try
        {
            var item = await _itemService.GetItemAsync(id, category);
            
            if (item == null)
            {
                ViewBag.Error = $"Item with ID '{id}' in category '{category}' was not found.";
                return View();
            }

            return View(item);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred while fetching item details: {Id} in category: {Category}", id, category);
            ViewBag.Error = "Unable to load item details at this time. Please try again later.";
            return View();
        }
    }
}