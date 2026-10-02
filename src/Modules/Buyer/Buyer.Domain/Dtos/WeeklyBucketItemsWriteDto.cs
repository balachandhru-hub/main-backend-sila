namespace Buyer.Domain.Dtos
{
    public class WeeklyBucketItemsWriteDto
    {
        /// <summary>
        /// Needed only when the caller works with outlets of more than one property, or has no outlet assignment.
        /// </summary>
        public Guid? OutletId { get; set; }
        public List<CatalogItemWriteDto> Items { get; set; } = new();
    }
}
