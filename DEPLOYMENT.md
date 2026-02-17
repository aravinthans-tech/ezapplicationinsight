# Deployment Instructions

## Step 1: Push to GitHub

Run these commands in the TelemetryAPI directory:

```bash
# Add your GitHub remote (replace YOUR_USERNAME with your GitHub username)
git remote add origin https://github.com/YOUR_USERNAME/ezapplicationinsight.git

# Or if remote already exists, update it:
git remote set-url origin https://github.com/YOUR_USERNAME/ezapplicationinsight.git

# Push to GitHub
git push -u origin main
```

## Step 2: Deploy to Render

### Option A: Using Render Dashboard

1. Go to https://render.com and sign in
2. Click "New +" → "Web Service"
3. Connect your GitHub repository (`ezapplicationinsight`)
4. Configure:
   - **Name**: `telemetry-api`
   - **Environment**: `.NET Core`
   - **Build Command**: `dotnet publish -c Release -o ./publish`
   - **Start Command**: `dotnet ./publish/TelemetryAPI.dll`
   - **Plan**: Free (or Starter/Professional)

5. **Environment Variables** (in Render dashboard):
   - `ASPNETCORE_ENVIRONMENT` = `Production`
   - `ASPNETCORE_URLS` = `http://0.0.0.0:10000`
   - `ApplicationInsights__ConnectionString` = `InstrumentationKey=ac9f6aaf-beb8-41e1-bd8f-4d1bc9ece30c;IngestionEndpoint=https://canadacentral-1.in.applicationinsights.azure.com/;LiveEndpoint=https://canadacentral.livediagnostics.monitor.azure.com/;ApplicationId=7c0154d5-8cec-4d11-b94d-bb10295b9436`

6. Click "Create Web Service"

### Option B: Using render.yaml (Auto-detected)

If you pushed `render.yaml` to your repository:
1. In Render Dashboard, click "New +" → "Blueprint"
2. Connect your repository
3. Render will auto-detect `render.yaml`
4. Set the `ApplicationInsights__ConnectionString` environment variable manually

## Step 3: Update Main API

After deployment, update your main API's `appsettings.json`:

```json
"AppSettings": {
  "TelemetryApiUrl": "https://your-telemetry-api.onrender.com"
}
```

Replace `your-telemetry-api.onrender.com` with your actual Render URL.

## Testing

After deployment, test the endpoint:

```bash
curl -X POST https://your-telemetry-api.onrender.com/api/telemetry/log \
  -H "Content-Type: application/json" \
  -d '{
    "method": "GET",
    "endPoint": "/api/test",
    "responseStatusCode": 200,
    "requestTime": "2024-01-01T00:00:00.000Z",
    "responseTime": "2024-01-01T00:00:01.000Z"
  }'
```

