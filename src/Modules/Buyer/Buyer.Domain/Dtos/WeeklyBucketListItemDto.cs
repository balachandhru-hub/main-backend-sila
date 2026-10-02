namespace Buyer.Domain.Dtos
{
    public class WeeklyBucketListItemDto
    {
        public Guid Id { get; set; }
        public string BucketCode { get; set; } = string.Empty;
        public int WeekNumber { get; set; }
        public int Year { get; set; }
        public string? PropertyName { get; set; }
        public string PlantCode { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public int ItemCount { get; set; }
        public DateTime? FrozenOn { get; set; }
    }
}
