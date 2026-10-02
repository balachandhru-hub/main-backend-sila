using Operations.Domain.Enums;

namespace Operations.Domain.Dtos
{
    public class IntegrationConfigurationResponseDto
    {
        public Guid Id { get; set; }
        public Guid OrganizationId { get; set; }
        public Guid? OrganizationUnitId { get; set; }
        public string EntityCode { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public IntegrationProcessType ProcessType { get; set; }
        public IntegrationProtocol Protocol { get; set; }
        public string BaseUrl { get; set; } = string.Empty;
        public string? ResourcePath { get; set; }
        public IntegrationAuthenticationType AuthenticationType { get; set; }
        public string? Username { get; set; }
        public string CredentialStatus { get; set; } = string.Empty;
        public int TimeoutSeconds { get; set; }
        public int RetryCount { get; set; }
        public int? PageSize { get; set; }
        public string? WatermarkField { get; set; }
        public DateTime? LastWatermark { get; set; }
        public DateTime? LastAttemptAt { get; set; }
        public DateTime? LastSuccessfulRunAt { get; set; }
        public DateTime? NextRunAt { get; set; }
        public bool IsRunning { get; set; }
        public string? LastErrorSafe { get; set; }
        public string? ScheduleCron { get; set; }
        public IntegrationConfigurationStatus Status { get; set; }
        public DateTime? TestedAt { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
        public string? SystemName { get; set; }
        public string HttpMethod { get; set; } = "GET";
        public string PayloadFormat { get; set; } = "JSON";
        public string? RequestBody { get; set; }
        public Dictionary<string, string>? Headers { get; set; }
        public string? ApiKeyHeader { get; set; }
    }
}
