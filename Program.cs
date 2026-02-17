using Microsoft.ApplicationInsights;
using Microsoft.ApplicationInsights.Extensibility;
using Microsoft.ApplicationInsights.Channel;
using Microsoft.ApplicationInsights.DataContracts;
using TelemetryAPI.Models;
using System.Text.Json;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Configure Application Insights
builder.Services.AddApplicationInsightsTelemetry();

// Add CORS to allow main API calls
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAllOrigins",
        policy =>
        {
            policy.AllowAnyOrigin()
                  .AllowAnyHeader()
                  .AllowAnyMethod();
        });
});

var app = builder.Build();


// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseCors("AllowAllOrigins");

// Middleware to disable Application Insights request tracking for /api/telemetry/log
app.Use(async (context, next) =>
{
    if (context.Request.Path.Value?.ToLower().Contains("/api/telemetry/log") == true)
    {
        // Disable Application Insights automatic request tracking for this endpoint
        var activity = System.Diagnostics.Activity.Current;
        if (activity != null)
        {
            activity.IsAllDataRequested = false;
        }
    }
    await next();
});

// Telemetry endpoint to receive and log API call data
app.MapPost("/api/telemetry/log", async (TelemetryData data, IServiceProvider serviceProvider, IConfiguration configuration) =>
{
    TelemetryClient? telemetryClient = null;
    try
    {
        // Don't log the telemetry/log endpoint itself - only log other main API endpoints
        var endpoint = data.endPoint?.ToLower() ?? "";
        if (endpoint.Contains("/api/telemetry/log") || endpoint.Contains("api/telemetry/log"))
        {
            return Results.Ok(new { success = true, message = "Telemetry endpoint excluded from logging" });
        }

        // Verify Application Insights connection string is configured
        var connectionString = configuration["ApplicationInsights:ConnectionString"];
        if (string.IsNullOrEmpty(connectionString))
        {
            return Results.Ok(new { success = false, message = "Application Insights connection string not configured" });
        }

        // Get TelemetryClient from service provider
        telemetryClient = serviceProvider.GetService<TelemetryClient>();
        if (telemetryClient == null)
        {
            // Try to create TelemetryClient manually if DI doesn't provide it
            var telemetryConfiguration = TelemetryConfiguration.CreateDefault();
            telemetryConfiguration.ConnectionString = connectionString;
            telemetryClient = new TelemetryClient(telemetryConfiguration);
            Console.WriteLine($"[Telemetry] Created TelemetryClient manually");
        }

        // Log custom event to Application Insights with all telemetry properties
        var properties = new Dictionary<string, string>
        {
            { "Method", data.method ?? "" },
            { "Endpoint", data.endPoint ?? "" },
            { "RequestTime", data.requestTime ?? "" },
            { "ResponseTime", data.responseTime ?? "" },
            { "StatusCode", data.responseStatusCode.ToString() },
            { "UserId", data.userId ?? "" },
            { "TenantId", data.tenantId ?? "" },
            { "Token", data.token ?? "" }
        };
        
        // Add error information if available
        if (!string.IsNullOrEmpty(data.errorMessage))
        {
            // Truncate error message to 8000 chars (Application Insights property limit)
            if (data.errorMessage.Length > 8000)
            {
                properties.Add("ErrorMessage", data.errorMessage.Substring(0, 8000) + "...[truncated]");
            }
            else
            {
                properties.Add("ErrorMessage", data.errorMessage);
            }
        }
        
        if (!string.IsNullOrEmpty(data.errorCode))
        {
            properties.Add("ErrorCode", data.errorCode);
        }

        // Include request body if not too large (Application Insights has property size limits)
        if (!string.IsNullOrEmpty(data.requestBody) && data.requestBody.Length < 8000)
        {
            properties.Add("RequestBody", data.requestBody);
        }
        else if (!string.IsNullOrEmpty(data.requestBody))
        {
            properties.Add("RequestBody", data.requestBody.Substring(0, Math.Min(8000, data.requestBody.Length)) + "...[truncated]");
        }

        // Log to console for local debugging (before tracking)
        Console.WriteLine($"[Telemetry] Received: Endpoint={data.endPoint}, Method={data.method}, StatusCode={data.responseStatusCode}, UserId={data.userId}, TenantId={data.tenantId}");
        Console.WriteLine($"[Telemetry] Connection String configured: {!string.IsNullOrEmpty(connectionString)}");
        Console.WriteLine($"[Telemetry] TelemetryClient is null: {telemetryClient == null}");
        
        if (telemetryClient != null)
        {
            try
            {
                // Track custom event
                telemetryClient.TrackEvent("ApiCallTelemetry", properties);
                Console.WriteLine($"[Telemetry] Event tracked: ApiCallTelemetry");
                
                // Note: Flush() is optional - Application Insights sends data asynchronously
                // We'll skip Flush() to avoid NullReferenceException issues
                // Data will still be sent to Application Insights automatically
                Console.WriteLine($"[Telemetry] Skipping Flush() - data will be sent asynchronously by Application Insights");
                
                // Log to console after tracking
                Console.WriteLine($"[Telemetry] Properties count: {properties.Count}");
            }
            catch (Exception trackEx)
            {
                Console.WriteLine($"[Telemetry] TrackEvent error: {trackEx.Message}");
                Console.WriteLine($"[Telemetry] StackTrace: {trackEx.StackTrace}");
                throw; // Re-throw to be caught by outer catch
            }
        }
        else
        {
            Console.WriteLine($"[Telemetry] ERROR: TelemetryClient is null, cannot track event");
            return Results.Ok(new { 
                success = false, 
                message = "TelemetryClient not available",
                endpoint = data.endPoint
            });
        }
        
        // Application Insights sends data asynchronously, but Flush() ensures it's queued for sending
        // Note: There may still be a 2-5 minute delay before data appears in Azure Portal when running locally

        return Results.Ok(new { 
            success = true, 
            message = "Telemetry logged successfully",
            endpoint = data.endPoint,
            method = data.method,
            statusCode = data.responseStatusCode,
            connectionStringConfigured = !string.IsNullOrEmpty(connectionString),
            propertiesCount = properties.Count
        });
    }
    catch (Exception ex)
    {
        // Log error to console for local debugging
        Console.WriteLine($"[Telemetry] ERROR: {ex.Message}");
        Console.WriteLine($"[Telemetry] StackTrace: {ex.StackTrace}");
        
        // Log error but don't fail
        if (telemetryClient != null)
        {
            try
            {
                telemetryClient.TrackException(ex);
                // Skip Flush() to avoid NullReferenceException - data will be sent asynchronously
                Console.WriteLine($"[Telemetry] Exception tracked (Flush skipped)");
            }
            catch (Exception trackEx)
            {
                Console.WriteLine($"[Telemetry] TrackException error: {trackEx.Message}");
            }
        }
        return Results.StatusCode(500);
    }
})
.WithName("LogTelemetry")
.WithOpenApi();

var summaries = new[]
{
    "Freezing", "Bracing", "Chilly", "Cool", "Mild", "Warm", "Balmy", "Hot", "Sweltering", "Scorching"
};

// API endpoint to fetch telemetry data for dashboard
app.MapGet("/api/telemetry/data", async (TelemetryClient telemetryClient, IConfiguration configuration) =>
{
    try
    {
        // For now, return sample data structure
        // TODO: Implement Application Insights query using REST API or SDK
        // You'll need Application Insights API key from Azure Portal
        
        var sampleData = new[]
        {
            new
            {
                timestamp = DateTime.UtcNow.AddSeconds(-5).ToString("yyyy-MM-ddTHH:mm:ss.fffZ"),
                method = "POST",
                endpoint = "/api/workflow/inboxList/{id}",
                statusCode = 200,
                duration = 63600,
                tenantId = "1",
                userId = "8",
                requestBody = "{\"workflowId\": 123, \"status\": \"open\"}",
                requestTime = DateTime.UtcNow.AddSeconds(-65).ToString("yyyy-MM-ddTHH:mm:ss.fffZ"),
                responseTime = DateTime.UtcNow.AddSeconds(-5).ToString("yyyy-MM-ddTHH:mm:ss.fffZ")
            },
            new
            {
                timestamp = DateTime.UtcNow.AddSeconds(-15).ToString("yyyy-MM-ddTHH:mm:ss.fffZ"),
                method = "GET",
                endpoint = "/swagger/v1/swagger.json",
                statusCode = 200,
                duration = 542,
                tenantId = "0",
                userId = "0",
                requestBody = "",
                requestTime = DateTime.UtcNow.AddSeconds(-15).ToString("yyyy-MM-ddTHH:mm:ss.fffZ"),
                responseTime = DateTime.UtcNow.AddSeconds(-15).AddMilliseconds(542).ToString("yyyy-MM-ddTHH:mm:ss.fffZ")
            },
            new
            {
                timestamp = DateTime.UtcNow.AddSeconds(-25).ToString("yyyy-MM-ddTHH:mm:ss.fffZ"),
                method = "POST",
                endpoint = "/api/authentication/decryptAES",
                statusCode = 200,
                duration = 1250,
                tenantId = "1",
                userId = "8",
                requestBody = "{\"encryptedData\": \"abc123...\"}",
                requestTime = DateTime.UtcNow.AddSeconds(-26).ToString("yyyy-MM-ddTHH:mm:ss.fffZ"),
                responseTime = DateTime.UtcNow.AddSeconds(-25).ToString("yyyy-MM-ddTHH:mm:ss.fffZ")
            }
        };
        
        return Results.Ok(new { data = sampleData });
    }
    catch (Exception ex)
    {
        telemetryClient.TrackException(ex);
        return Results.StatusCode(500);
    }
})
.WithName("GetTelemetryData")
.WithOpenApi();

app.MapGet("/weatherforecast", () =>
{
    var forecast =  Enumerable.Range(1, 5).Select(index =>
        new WeatherForecast
        (
            DateOnly.FromDateTime(DateTime.Now.AddDays(index)),
            Random.Shared.Next(-20, 55),
            summaries[Random.Shared.Next(summaries.Length)]
        ))
        .ToArray();
    return forecast;
})
.WithName("GetWeatherForecast")
.WithOpenApi();

app.Run();

record WeatherForecast(DateOnly Date, int TemperatureC, string? Summary)
{
    public int TemperatureF => 32 + (int)(TemperatureC / 0.5556);
}

