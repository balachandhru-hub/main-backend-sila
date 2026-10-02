namespace Buyer.Domain.Dtos
{
    public class WeeklyBucketRecommendationDecisionDto
    {
        /// <summary>
        /// APPROVE or REJECT.
        /// </summary>
        public string Status { get; set; } = string.Empty;
    }
}
