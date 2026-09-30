namespace Buyer.Domain.Dtos
{
    public class WishlistItemResponseDto
    {
        public Guid Id { get; set; }
        public Guid MaterialId { get; set; }
        public string MaterialCode { get; set; } = string.Empty;
        public string MaterialName { get; set; } = string.Empty;
        public string? UnitOfMeasure { get; set; }
        public decimal Quantity { get; set; }
        public decimal? UnitPrice { get; set; }
        public string? Currency { get; set; }
        public DateTime? RequiredDate { get; set; }
    }
}
