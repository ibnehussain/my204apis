using Microsoft.Azure.Cosmos;
using System.Net;
using System.Text.Json;

namespace Demo.API.Middleware;

/// <summary>
/// Global exception handling middleware that catches unhandled exceptions
/// and returns appropriate HTTP responses without exposing internal details
/// </summary>
public class GlobalExceptionHandlingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<GlobalExceptionHandlingMiddleware> _logger;

    public GlobalExceptionHandlingMiddleware(RequestDelegate next, ILogger<GlobalExceptionHandlingMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (Exception ex)
        {
            await HandleExceptionAsync(context, ex);
        }
    }

    private async Task HandleExceptionAsync(HttpContext context, Exception exception)
    {
        var traceId = context.TraceIdentifier;
        var requestPath = context.Request.Path;
        var requestMethod = context.Request.Method;

        // Log the exception with context
        _logger.LogError(exception, 
            "Unhandled exception occurred. TraceId: {TraceId}, Path: {RequestPath}, Method: {RequestMethod}", 
            traceId, requestPath, requestMethod);

        // Determine response based on exception type
        var (statusCode, message) = GetErrorResponse(exception, traceId);

        // Create error response
        var errorResponse = new ErrorResponse
        {
            Message = message,
            TraceId = traceId,
            Timestamp = DateTime.UtcNow
        };

        // Set response properties
        context.Response.StatusCode = (int)statusCode;
        context.Response.ContentType = "application/json";

        // Serialize and write response
        var jsonResponse = JsonSerializer.Serialize(errorResponse, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        });

        await context.Response.WriteAsync(jsonResponse);
    }

    private static (HttpStatusCode StatusCode, string Message) GetErrorResponse(Exception exception, string traceId)
    {
        return exception switch
        {
            CosmosException cosmosEx => HandleCosmosException(cosmosEx),
            ArgumentNullException => (HttpStatusCode.BadRequest, "Required parameter is missing"),
            ArgumentException => (HttpStatusCode.BadRequest, "Invalid request parameters"),
            UnauthorizedAccessException => (HttpStatusCode.Unauthorized, "Access denied"),
            NotImplementedException => (HttpStatusCode.NotImplemented, "Feature not implemented"),
            TimeoutException => (HttpStatusCode.RequestTimeout, "Request timed out"),
            OperationCanceledException => (HttpStatusCode.RequestTimeout, "Request was cancelled"),
            _ => (HttpStatusCode.InternalServerError, "An unexpected error occurred")
        };
    }

    private static (HttpStatusCode StatusCode, string Message) HandleCosmosException(CosmosException cosmosException)
    {
        return cosmosException.StatusCode switch
        {
            HttpStatusCode.NotFound => (HttpStatusCode.NotFound, "Resource not found"),
            HttpStatusCode.BadRequest => (HttpStatusCode.BadRequest, "Invalid request"),
            HttpStatusCode.Unauthorized => (HttpStatusCode.Unauthorized, "Unauthorized access"),
            HttpStatusCode.Forbidden => (HttpStatusCode.Forbidden, "Access forbidden"),
            HttpStatusCode.Conflict => (HttpStatusCode.Conflict, "Resource conflict"),
            HttpStatusCode.TooManyRequests => (HttpStatusCode.TooManyRequests, "Too many requests - please try again later"),
            HttpStatusCode.RequestEntityTooLarge => (HttpStatusCode.RequestEntityTooLarge, "Request too large"),
            HttpStatusCode.PreconditionFailed => (HttpStatusCode.PreconditionFailed, "Precondition failed"),
            _ => (HttpStatusCode.InternalServerError, "Database error occurred")
        };
    }
}

/// <summary>
/// Standard error response model
/// </summary>
public class ErrorResponse
{
    public string Message { get; set; } = string.Empty;
    public string TraceId { get; set; } = string.Empty;
    public DateTime Timestamp { get; set; }
}