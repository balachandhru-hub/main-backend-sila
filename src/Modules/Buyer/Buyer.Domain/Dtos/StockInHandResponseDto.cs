namespace Buyer.Domain.Dtos
{
    public class StockInHandResponseDto
    {
        /// <summary>False when the organization has no active stock API: its stock is then not known.</summary>
        public bool Configured { get; set; }

        /// <summary>True when a stock API could not be read: the items are then not the whole stock.</summary>
        public bool Failed { get; set; }
        public List<StockInHandItemDto> Items { get; set; } = new();
    }
}
