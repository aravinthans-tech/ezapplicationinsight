namespace TelemetryAPI.Models
{
    public class TelemetryData
    {
        public string method { get; set; } = string.Empty;
        public string endPoint { get; set; } = string.Empty;
        public string requestBody { get; set; } = string.Empty;
        public string token { get; set; } = string.Empty;
        public string responseTime { get; set; } = string.Empty;
        public string requestTime { get; set; } = string.Empty;
        public int responseStatusCode { get; set; } = 0;
        public string userId { get; set; } = string.Empty;
        public string tenantId { get; set; } = string.Empty;
        public string errorMessage { get; set; } = string.Empty;
        public string errorCode { get; set; } = string.Empty;
    }
}

