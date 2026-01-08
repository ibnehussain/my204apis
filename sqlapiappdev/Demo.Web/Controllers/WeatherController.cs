using Microsoft.AspNetCore.Mvc;
using System.Text.Json;
using Demo.Web.Models;

namespace Demo.Web.Controllers;

public class WeatherController : Controller
{
    private readonly ILogger<WeatherController> _logger;
    private readonly IHttpClientFactory _httpClientFactory;

    public WeatherController(ILogger<WeatherController> logger, IHttpClientFactory httpClientFactory)
    {
        _logger = logger;
        _httpClientFactory = httpClientFactory;
    }

    public async Task<IActionResult> Index()
    {
        try
        {
            var httpClient = _httpClientFactory.CreateClient("DemoAPI");
            var response = await httpClient.GetAsync("api/weather");
            
            if (response.IsSuccessStatusCode)
            {
                var jsonString = await response.Content.ReadAsStringAsync();
                var weatherForecasts = JsonSerializer.Deserialize<WeatherForecast[]>(jsonString, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });
                
                return View(weatherForecasts);
            }
            else
            {
                _logger.LogWarning("Failed to fetch weather data. Status code: {StatusCode}", response.StatusCode);
                ViewBag.Error = "Unable to fetch weather data from API";
                return View(new WeatherForecast[0]);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred while fetching weather data");
            ViewBag.Error = "An error occurred while fetching weather data";
            return View(new WeatherForecast[0]);
        }
    }
}