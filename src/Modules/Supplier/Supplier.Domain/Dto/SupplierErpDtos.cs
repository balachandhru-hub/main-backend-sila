namespace Supplier.Domain.Dto
{
    public class SupplierPurchaseOrderLineDto
    {
        public string Sku { get; set; } = string.Empty;
        public decimal Quantity { get; set; }
        public string? UnitOfMeasure { get; set; }
        public decimal? UnitPrice { get; set; }
    }

    public class SupplierPurchaseOrderRequestDto
    {
        public Guid WishlistId { get; set; }
        public Guid BuyerOrganizationId { get; set; }
        public Guid SupplierOrganizationId { get; set; }
        public string IdempotencyKey { get; set; } = string.Empty;
        public string BuyerDocumentType { get; set; } = string.Empty;
        public string BuyerDocumentNumber { get; set; } = string.Empty;
        public string? ShipTo { get; set; }
        public string? DeliveryInstruction { get; set; }
        public DateTime? RequiredDate { get; set; }
        public string? Currency { get; set; }
        public string CorrelationId { get; set; } = string.Empty;
        public List<SupplierPurchaseOrderLineDto> Lines { get; set; } = new();
    }

    public class SupplierPurchaseOrderResponseDto
    {
        public string? DocumentNumber { get; set; }
        public string Status { get; set; } = string.Empty;
        public string? ErrorMessage { get; set; }
    }

    public class SupplierErpWriteDto
    {
        public string ErpType { get; set; } = string.Empty;
        public string BaseUrl { get; set; } = string.Empty;
        public string? AuthPath { get; set; }
        public string OrderPath { get; set; } = string.Empty;
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
        public string? DefaultShipTo { get; set; }
        public string? OrderDateFormat { get; set; }
        public string? HeadersJson { get; set; }
        public int TimeoutSeconds { get; set; } = 60;
        public int MaxRetryCount { get; set; } = 3;
        public bool IsActive { get; set; } = true;
    }

    public class SupplierErpResponseDto
    {
        public Guid Id { get; set; }
        public string ErpType { get; set; } = string.Empty;
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
