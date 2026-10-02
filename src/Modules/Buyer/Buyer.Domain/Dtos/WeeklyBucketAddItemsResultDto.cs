namespace Buyer.Domain.Dtos
{
    public class WeeklyBucketAddItemsResultDto
    {
        public Guid BucketId { get; set; }
        public string BucketCode { get; set; } = string.Empty;
    }
}
