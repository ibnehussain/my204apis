using Microsoft.Azure.Cosmos;
using Demo.API.Models;
using Demo.API.Extensions;
using Demo.API.Middleware;

var builder = WebApplication.CreateBuilder(args);

// Configure Cosmos DB settings
builder.Services.Configure<CosmosDbSettings>(
    builder.Configuration.GetSection("Cosmos"));

// Register CosmosClient as a singleton with proper configuration
var cosmosDbSettings = builder.Configuration.GetSection("Cosmos").Get<CosmosDbSettings>()
    ?? throw new InvalidOperationException("Cosmos configuration is missing");

builder.Services.AddSingleton<CosmosClient>(serviceProvider =>
{
    var cosmosClientOptions = new CosmosClientOptions
    {
        ApplicationName = "Demo.API",
        ConnectionMode = ConnectionMode.Direct,
        ConsistencyLevel = ConsistencyLevel.Session,
        MaxRetryAttemptsOnRateLimitedRequests = 3,
        MaxRetryWaitTimeOnRateLimitedRequests = TimeSpan.FromSeconds(30),
        EnableContentResponseOnWrite = false, // Improve performance for writes
        SerializerOptions = new CosmosSerializationOptions
        {
            PropertyNamingPolicy = CosmosPropertyNamingPolicy.CamelCase
        }
    };

    return new CosmosClient(cosmosDbSettings.Endpoint, cosmosDbSettings.Key, cosmosClientOptions);
});

// Add services to the container.
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new Microsoft.OpenApi.Models.OpenApiInfo
    {
        Title = "Demo API",
        Version = "v1",
        Description = "A demo ASP.NET Core Web API with Azure Cosmos DB integration for managing Items",
        Contact = new Microsoft.OpenApi.Models.OpenApiContact
        {
            Name = "Demo API Support",
            Email = "support@demo-api.com"
        }
    });

    // Enable XML comments for API documentation
    var xmlFile = $"{System.Reflection.Assembly.GetExecutingAssembly().GetName().Name}.xml";
    var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
    if (File.Exists(xmlPath))
    {
        c.IncludeXmlComments(xmlPath);
    }

    // Add security definition for future authentication
    c.AddSecurityDefinition("Bearer", new Microsoft.OpenApi.Models.OpenApiSecurityScheme
    {
        Description = "JWT Authorization header using the Bearer scheme. Example: \"Authorization: Bearer {token}\"",
        Name = "Authorization",
        In = Microsoft.OpenApi.Models.ParameterLocation.Header,
        Type = Microsoft.OpenApi.Models.SecuritySchemeType.ApiKey,
        Scheme = "Bearer"
    });

    // Configure response types
    c.UseInlineDefinitionsForEnums();
});

// Add health checks
builder.Services.AddHealthChecks();

// Add CORS
builder.Services.AddCors(options =>
{
    options.AddPolicy("DefaultPolicy", policy =>
    {
        policy.WithOrigins("https://localhost:5001") // Demo.Web URL
              .AllowAnyHeader()
              .AllowAnyMethod();
    });
});

// Configure logging
builder.Logging.ClearProviders();
builder.Logging.AddConsole();
builder.Logging.AddDebug();

var app = builder.Build();

// Add global exception handling middleware (must be early in pipeline)
app.UseMiddleware<GlobalExceptionHandlingMiddleware>();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "Demo API v1");
        c.RoutePrefix = "swagger";
        c.DocumentTitle = "Demo API Documentation";
        c.DisplayRequestDuration();
        c.EnableDeepLinking();
        c.EnableFilter();
        c.ShowExtensions();
        c.EnableValidator();
    });
    app.UseDeveloperExceptionPage();
}

app.UseHttpsRedirection();

// Security headers
app.Use(async (context, next) =>
{
    context.Response.Headers["X-Frame-Options"] = "DENY";
    context.Response.Headers["X-Content-Type-Options"] = "nosniff";
    context.Response.Headers["Referrer-Policy"] = "strict-origin-when-cross-origin";
    await next();
});

app.UseCors("DefaultPolicy");
app.UseAuthentication();
app.UseAuthorization();

// Map controllers
app.MapControllers();

// Map health checks
app.MapHealthChecks("/health");

// Define summaries for weather endpoint
var summaries = new[]
{
    "Freezing", "Bracing", "Chilly", "Cool", "Mild", "Warm", "Balmy", "Hot", "Sweltering", "Scorching"
};

// Create a sample API endpoint for demonstration
app.MapGet("/api/weather", () =>
{
    var forecast = Enumerable.Range(1, 5).Select(index =>
        new WeatherForecast
        (
            DateOnly.FromDateTime(DateTime.Now.AddDays(index)),
            Random.Shared.Next(-20, 55),
            summaries[Random.Shared.Next(summaries.Length)]
        ))
        .ToArray();
    return Results.Ok(forecast);
})
.WithName("GetWeatherForecast")
.WithSummary("Get weather forecast")
.WithDescription("Returns a 5-day weather forecast with random temperature data")
.WithTags("Weather")
.Produces<WeatherForecast[]>(StatusCodes.Status200OK)
.WithOpenApi();

// Initialize Cosmos DB (create database and container if needed)
// Comment out in production if using pre-provisioned resources
// Skip initialization if Cosmos DB is not available (e.g., emulator not running)
if (app.Environment.IsDevelopment())
{
    try
    {
        await app.Services.EnsureCosmosDbCreatedAsync();
    }
    catch (Exception ex)
    {
        app.Logger.LogWarning(ex, "Cosmos DB initialization failed - continuing without database setup");
    }
}
else
{
    await app.Services.EnsureCosmosDbCreatedAsync();
}

app.Run();

record WeatherForecast(DateOnly Date, int TemperatureC, string? Summary)
{
    public int TemperatureF => 32 + (int)(TemperatureC / 0.5556);
}
