namespace Buyer.Domain.Dtos
{
    public class WeeklyBucketDecisionDto
    {
        /// <summary>
        /// APPROVE or REJECT.
        /// </summary>
        public string Status { get; set; } = string.Empty;
        public string? Comment { get; set; }
    }
}
