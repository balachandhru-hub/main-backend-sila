namespace Supplier.Domain.Dto
{
    public class SupplierErpResponseDto
    {
        public Guid Id { get; set; }
        public string Process { get; set; } = "PO_CREATE";
        public string ErpType { get; set; } = string.Empty;
        public string PayloadFormat { get; set; } = "JSON";
        public string? RequestBody { get; set; }
        public string BaseUrl { get; set; } = string.Empty;
        public string? AuthPath { get; set; }
        public string OrderPath { get; set; } = string.Empty;
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
        public string? DefaultShipTo { get; set; }
        public string OrderDateFormat { get; set; } = "dd/MM/yyyy";
        public string? HeadersJson { get; set; }
        public int TimeoutSeconds { get; set; }
        public int MaxRetryCount { get; set; }
        public int Version { get; set; }
        public bool IsActive { get; set; }
    }
}
