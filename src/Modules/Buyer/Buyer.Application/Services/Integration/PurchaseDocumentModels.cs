namespace Buyer.Application.Services.Integration
{
    public sealed class ExternalCallResult
    {
        public bool Succeeded { get; set; }
        public int StatusCode { get; set; }
        public string? DocumentNumber { get; set; }
        public string? ErrorMessage { get; set; }
        public string? ResponseBody { get; set; }
        public bool OutcomeUnknown { get; set; }
        public long DurationMs { get; set; }
    }

    public sealed class BuyerPurchaseLine
    {
        /// <summary>
        /// Item Master material code mapped to the catalog product.
        /// </summary>
        public string MaterialCode { get; set; } = string.Empty;
        public string MaterialName { get; set; } = string.Empty;
        public string? Sku { get; set; }
        public decimal Quantity { get; set; }
        public string? UnitOfMeasure { get; set; }
        public decimal? UnitPrice { get; set; }
        public string? Currency { get; set; }
        public string? StorageLocation { get; set; }
    }

    /// <summary>
    /// One purchase order: the lines of one supplier of a weekly bucket.
    /// </summary>
    public sealed class BuyerPurchaseDocumentRequest
    {
        public string IdempotencyKey { get; set; } = string.Empty;
        public Guid WeeklyBucketId { get; set; }
        public string BucketCode { get; set; } = string.Empty;
        public Guid BuyerOrganizationId { get; set; }
        public string CompanyCode { get; set; } = string.Empty;
        public string PlantCode { get; set; } = string.Empty;
        public Guid SupplierId { get; set; }
        public string? SupplierName { get; set; }
        public string DocumentType { get; set; } = string.Empty;
        public string? BuyerDocumentNumber { get; set; }
        public string? ShipTo { get; set; }
        public string? OutletCode { get; set; }
        public string? OutletName { get; set; }
        public string? Currency { get; set; }
        public string? DeliveryInstruction { get; set; }
        public DateTime? RequiredDate { get; set; }
        public string CorrelationId { get; set; } = string.Empty;
        public List<BuyerPurchaseLine> Lines { get; set; } = new();
    }
}
