namespace Operations.Domain.Dtos
{
    public class IntegrationDataUpdateResponseDto
    {
        public Guid ConfigurationId { get; set; }
        public int PurchaseOrders { get; set; }
        public int Suppliers { get; set; }
        public DateTime? LastSyncedAt { get; set; }
        public DateTime? LastWatermark { get; set; }
        public bool IsRunning { get; set; }
        public string? LastErrorSafe { get; set; }
        public List<IntegrationPurchaseOrderRowDto> PurchaseOrderRows { get; set; } = new();
        public List<IntegrationSupplierRowDto> SupplierRows { get; set; } = new();
        public string Kind { get; set; } = "PURCHASE_ORDERS";
        public int TotalRows { get; set; } = 0;
        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 25;
    }
}
