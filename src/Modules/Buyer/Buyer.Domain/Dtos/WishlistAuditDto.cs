namespace Buyer.Domain.Dtos
{
    public class WishlistAuditDto
    {
        public string Action { get; set; } = string.Empty;
        public string? Detail { get; set; }
        public Guid? ActorUserId { get; set; }
        public DateTime DateCreated { get; set; }
    }
}
