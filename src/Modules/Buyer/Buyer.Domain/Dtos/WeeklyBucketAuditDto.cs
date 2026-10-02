namespace Buyer.Domain.Dtos
{
    public class WeeklyBucketAuditDto
    {
        public string Action { get; set; } = string.Empty;
        public string? Detail { get; set; }
        public Guid? ActorUserId { get; set; }
        public string? ActorName { get; set; }
        public DateTime DateCreated { get; set; }
    }
}
