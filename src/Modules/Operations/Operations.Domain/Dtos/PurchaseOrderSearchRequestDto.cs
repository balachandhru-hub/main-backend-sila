namespace Operations.Domain.Dtos
{
    public class PurchaseOrderSearchRequestDto
    {
        public string? Query { get; set; }
        public string? EntityCode { get; set; }
        public Guid? OperatingUnitId { get; set; }
        public Guid? SupplierId { get; set; }
        public bool OpenOnly { get; set; }
    }
}
