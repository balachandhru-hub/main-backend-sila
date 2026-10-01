namespace Operations.Domain.Dtos
{
    public class MatchPurchaseOrderRequestDto
    {
        public Guid? PurchaseOrderId { get; set; }
        public string? PoNumber { get; set; }
    }
}
