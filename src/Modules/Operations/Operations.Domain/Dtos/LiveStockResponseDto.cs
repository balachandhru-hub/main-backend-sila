namespace Operations.Domain.Dtos
{
    public class LiveStockResponseDto
    {
        /// <summary>False when the organization has no active stock API: its stock is then not known.</summary>
        public bool Configured { get; set; }

        /// <summary>True when a stock API could not be read: the items are then not the whole stock.</summary>
        public bool Failed { get; set; }
        public List<LiveStockItemDto> Items { get; set; } = new();
    }
}
