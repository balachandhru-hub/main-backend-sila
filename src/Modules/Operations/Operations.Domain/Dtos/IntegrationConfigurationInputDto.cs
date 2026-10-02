using System.ComponentModel.DataAnnotations;
using Operations.Domain.Enums;

namespace Operations.Domain.Dtos
{
    public class IntegrationConfigurationInputDto
    {
        [Required] public string Name { get; set; } = string.Empty;
        [Required] public string EntityCode { get; set; } = "ALL";
        public Guid? OrganizationUnitId { get; set; }
        public IntegrationProcessType ProcessType { get; set; } = IntegrationProcessType.GET_PO;
        public IntegrationProtocol Protocol { get; set; } = IntegrationProtocol.ODATA_V4;
        [Required, Url] public string BaseUrl { get; set; } = string.Empty;
        public string? ResourcePath { get; set; } = "API_PURCHASEORDER_PROCESS_SRV/A_PurchaseOrder";
        public IntegrationAuthenticationType AuthenticationType { get; set; } = IntegrationAuthenticationType.NONE;
        public string? Username { get; set; }
        public string? Password { get; set; }
        public string? ClientId { get; set; }
        public string? ClientSecret { get; set; }
        public string? BearerToken { get; set; }
        public string? TokenEndpoint { get; set; }
        public string? TokenScope { get; set; }
        public Dictionary<string, string>? TokenHeaders { get; set; }
        public Dictionary<string, string>? TokenBody { get; set; }
        [Range(5, 300)] public int TimeoutSeconds { get; set; } = 30;
        [Range(0, 5)] public int RetryCount { get; set; } = 2;
        [Range(1, 1000)] public int? PageSize { get; set; } = 100;
        public string? WatermarkField { get; set; }
        public string? ScheduleCron { get; set; }
        public string? SystemName { get; set; }
        public string? HttpMethod { get; set; }
        public string? PayloadFormat { get; set; }
        public string? RequestBody { get; set; }
        public Dictionary<string, string>? Headers { get; set; }
        public string? ApiKeyHeader { get; set; }
        public string? ApiKey { get; set; }
    }
}
