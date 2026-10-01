using Operations.Domain.Enums;

namespace Operations.Domain.Dtos
{
    public class IntegrationPurchaseOrderRowDto
    {
        public Guid Id { get; set; }
        public string PoNumber { get; set; } = string.Empty;
        public string SupplierName { get; set; } = string.Empty;
        public PurchaseOrderStatus Status { get; set; }
        public string Currency { get; set; } = string.Empty;
        public DateOnly? PoDate { get; set; }
        public DateTime? LastSyncedAt { get; set; }
    }
}
