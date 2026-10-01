namespace Buyer.Domain.Dtos
{
    public class WishlistApprovalStepDto
    {
        public Guid UserId { get; set; }
        public string? Name { get; set; }
        public string? Email { get; set; }
        public int Order { get; set; }
        public string Status { get; set; } = string.Empty;
        public string? Comment { get; set; }
        public DateTime? ActedOn { get; set; }
    }
}
