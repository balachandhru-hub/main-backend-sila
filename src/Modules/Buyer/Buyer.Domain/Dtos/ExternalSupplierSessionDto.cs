namespace Buyer.Domain.Dto
{
    /// <summary>
    /// Identity resolved from a validated external-supplier session token
    /// (validated cross-service against the Supplier microservice's session-token store).
    /// </summary>
    public class ExternalSupplierSessionDto
    {
        public Guid RFQId { get; set; }

        public Guid ExternalSupplierId { get; set; }

        public Guid ExternalSupplierRFQId { get; set; }
    }
}
