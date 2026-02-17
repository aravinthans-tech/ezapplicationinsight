# TelemetryAPI

A .NET 8 Web API service that receives telemetry data from the main API and stores it in Azure Application Insights.

## Features

- Receives API call telemetry data via HTTP POST
- Stores telemetry data in Azure Application Insights as custom events
- Tracks error messages and error codes
- CORS enabled for cross-origin requests
- Swagger/OpenAPI documentation

## Endpoints

### POST `/api/telemetry/log`
Receives telemetry data from the main API and stores it in Application Insights.

**Request Body:**
```json
{
  "method": "GET",
  "endPoint": "/api/users",
  "requestBody": "...",
  "token": "...",
  "responseTime": "2024-01-01T00:00:00.000Z",
  "requestTime": "2024-01-01T00:00:00.000Z",
  "responseStatusCode": 200,
  "userId": "123",
  "tenantId": "1",
  "errorMessage": "",
  "errorCode": ""
}
```

### GET `/api/telemetry/data`
Returns sample telemetry data (TODO: Implement Application Insights query)

### GET `/swagger`
Swagger UI for API documentation

## Configuration

### Environment Variables

- `ASPNETCORE_ENVIRONMENT`: Set to `Production` for production
- `ASPNETCORE_URLS`: Server URL (default: `http://0.0.0.0:10000`)
- `ApplicationInsights__ConnectionString`: Azure Application Insights connection string

### appsettings.json

The connection string can also be configured in `appsettings.json`:

```json
{
  "ApplicationInsights": {
    "ConnectionString": "InstrumentationKey=...;IngestionEndpoint=...;..."
  }
}
```

## Deployment

### Render.com

1. Connect your GitHub repository
2. Select "Web Service"
3. Environment: `.NET Core`
4. Build Command: `dotnet publish -c Release -o ./publish`
5. Start Command: `dotnet ./publish/TelemetryAPI.dll`
6. Set environment variables as listed above

## Local Development

```bash
dotnet restore
dotnet run
```

The API will be available at `http://localhost:5292`

## Dependencies

- .NET 8.0
- Microsoft.ApplicationInsights.AspNetCore (3.0.0)
- Microsoft.AspNetCore.OpenApi (8.0.19)
- Swashbuckle.AspNetCore (6.6.2)

