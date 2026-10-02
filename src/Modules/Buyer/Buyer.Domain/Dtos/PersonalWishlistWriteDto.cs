namespace Buyer.Domain.Dtos
{
    public class PersonalWishlistWriteDto
    {
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }

        /// <summary>
        /// Products added when the wishlist is created. Not read when the wishlist is renamed.
        /// </summary>
        public List<CatalogItemWriteDto> Items { get; set; } = new();
    }
}
