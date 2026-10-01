namespace Buyer.Domain.Dtos
{
    public class WishlistIntegrationDto
    {
        public string IntegrationType { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public string? DocumentType { get; set; }
        public string? ExternalDocumentNumber { get; set; }
        public string? ErrorMessage { get; set; }
        public int RetryCount { get; set; }
        public DateTime? LastAttemptOn { get; set; }
        public DateTime? NextAttemptOn { get; set; }
        public string? CorrelationId { get; set; }
        public bool OutcomeUnknown { get; set; }
        public Guid? ConfigurationId { get; set; }
    }
}
