namespace Buyer.Domain.Dtos
{
    public class PersonalWishlistResponseDto
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
        public DateTime DateCreated { get; set; }
        public DateTime DateUpdated { get; set; }
        public List<PersonalWishlistItemDto> Items { get; set; } = new();
    }
}
