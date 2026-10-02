namespace Buyer.Domain.Dtos
{
    /// <summary>Stock of one material as the buyer's ERP reports it.</summary>
    public class StockInHandItemDto
    {
        public string MaterialCode { get; set; } = string.Empty;
        public string? Plant { get; set; }
        public string? StorageLocation { get; set; }
        public decimal Quantity { get; set; }
        public string? Uom { get; set; }
    }
}
