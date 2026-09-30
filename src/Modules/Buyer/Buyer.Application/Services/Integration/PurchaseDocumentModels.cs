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
        public string MaterialCode { get; set; } = string.Empty;
        public string MaterialName { get; set; } = string.Empty;
        public decimal Quantity { get; set; }
        public string? UnitOfMeasure { get; set; }
        public decimal? UnitPrice { get; set; }
        public string? Currency { get; set; }
    }

    public sealed class BuyerPurchaseDocumentRequest
    {
        public string IdempotencyKey { get; set; } = string.Empty;
        public Guid WishlistId { get; set; }
        public Guid BuyerOrganizationId { get; set; }
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
