namespace Buyer.Domain.Dto
{
    public class SupplierRFQAwardRequestDto
    {
        public Guid BuyerRFQId { get; set; }

        public List<SupplierRFQAwardItemRequestDto> Items { get; set; } = new();
    }

    public class SupplierRFQAwardItemRequestDto
    {
        public Guid BuyerRFQItemId { get; set; }

        public Guid SupplierId { get; set; }
    }
}
