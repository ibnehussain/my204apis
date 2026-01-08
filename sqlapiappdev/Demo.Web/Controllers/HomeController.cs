using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Demo.Web.Models;
using Demo.Web.Services;

namespace Demo.Web.Controllers;

public class HomeController : Controller
{
    private readonly ILogger<HomeController> _logger;
    private readonly IItemService _itemService;

    public HomeController(ILogger<HomeController> logger, IItemService itemService)
    {
        _logger = logger;
        _itemService = itemService;
    }

    public async Task<IActionResult> Index()
    {
        try
        {
            _logger.LogInformation("Fetching items for home page");
            
            var items = await _itemService.GetAllItemsAsync();
            
            // Pass items to the view via ViewBag for now
            ViewBag.Items = items;
            ViewBag.ItemCount = items.Count;
            
            // Get categories for display
            var categories = items.Select(i => i.Category).Distinct().OrderBy(c => c).ToList();
            ViewBag.Categories = categories;
            
            _logger.LogInformation("Successfully loaded {ItemCount} items with {CategoryCount} categories for home page", 
                items.Count, categories.Count);
                
            return View();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred while fetching items for home page");
            
            // Set error state for view
            ViewBag.Error = "Unable to load items at this time. Please try again later.";
            ViewBag.Items = new List<Item>();
            ViewBag.ItemCount = 0;
            ViewBag.Categories = new List<string>();
            
            return View();
        }
    }

    public IActionResult Privacy()
    {
        return View();
    }

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }
}
