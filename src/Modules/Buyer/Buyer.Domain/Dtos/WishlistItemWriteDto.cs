namespace Buyer.Domain.Dtos
{
    public class WishlistItemWriteDto
    {
        public Guid MaterialId { get; set; }
        public decimal Quantity { get; set; }
        public decimal? UnitPrice { get; set; }
        public string? Currency { get; set; }
        public DateTime? RequiredDate { get; set; }
    }
}
