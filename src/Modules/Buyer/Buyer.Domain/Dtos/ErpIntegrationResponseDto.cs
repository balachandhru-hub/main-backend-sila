namespace Buyer.Domain.Dtos
{
    public class ErpIntegrationResponseDto
    {
        public Guid Id { get; set; }
        public string ApiName { get; set; } = string.Empty;
        public string Process { get; set; } = string.Empty;
        public string ErpType { get; set; } = string.Empty;
        public Guid? SupplierOrganizationId { get; set; }
        public string PayloadFormat { get; set; } = "JSON";
        public string? RequestBody { get; set; }
        public string DocumentType { get; set; } = string.Empty;
        public string BaseUrl { get; set; } = string.Empty;
        public string CreateDocumentPath { get; set; } = string.Empty;
        public string HttpMethod { get; set; } = string.Empty;
        public string AuthType { get; set; } = string.Empty;
        public string? TokenUrl { get; set; }
        public string? Username { get; set; }
        public bool HasPassword { get; set; }
        public string? ClientId { get; set; }
        public bool HasClientSecret { get; set; }
        public string? Scope { get; set; }
        public string? ApiKeyHeader { get; set; }
        public bool HasApiKey { get; set; }
        public bool HasAccessToken { get; set; }
        public string? HeadersJson { get; set; }
        public int TimeoutSeconds { get; set; }
        public int MaxRetryCount { get; set; }
        public int Version { get; set; }
        public bool IsActive { get; set; }
    }
}
