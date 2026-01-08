using Microsoft.AspNetCore.Mvc;
using System.Net;

namespace Demo.API.Controllers;

[ApiController]
public class ErrorController : ControllerBase
{
    private readonly ILogger<ErrorController> _logger;

    public ErrorController(ILogger<ErrorController> logger)
    {
        _logger = logger;
    }

    [Route("/error")]
    [HttpGet, HttpPost, HttpPut, HttpDelete, HttpPatch]
    public IActionResult HandleError()
    {
        _logger.LogError("An unhandled exception occurred");
        
        return Problem(
            title: "An error occurred",
            detail: "An unexpected error occurred. Please try again later.",
            statusCode: (int)HttpStatusCode.InternalServerError
        );
    }
}