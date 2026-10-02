namespace Buyer.Domain.Dtos
{
    public class PersonalWishlistItemsWriteDto
    {
        public List<CatalogItemWriteDto> Items { get; set; } = new();
    }
}
