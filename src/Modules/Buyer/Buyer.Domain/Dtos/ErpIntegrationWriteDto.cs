namespace Buyer.Domain.Dtos
{
    public class ErpIntegrationWriteDto
    {
        public string ApiName { get; set; } = string.Empty;
        public string ErpType { get; set; } = string.Empty;
        public Guid? SupplierOrganizationId { get; set; }
        public string? PayloadFormat { get; set; }
        public string? RequestBody { get; set; }
        public string DocumentType { get; set; } = string.Empty;
        public string BaseUrl { get; set; } = string.Empty;
        public string CreateDocumentPath { get; set; } = string.Empty;
        public string HttpMethod { get; set; } = "POST";
        public string AuthType { get; set; } = string.Empty;
        public string? TokenUrl { get; set; }
        public string? Username { get; set; }
        public string? Password { get; set; }
        public string? ClientId { get; set; }
        public string? ClientSecret { get; set; }
        public string? Scope { get; set; }
        public string? ApiKeyHeader { get; set; }
        public string? ApiKey { get; set; }
        public string? AccessToken { get; set; }
        public string? HeadersJson { get; set; }
        public int TimeoutSeconds { get; set; } = 60;
        public int MaxRetryCount { get; set; } = 3;
        public bool IsActive { get; set; } = true;
    }
}
