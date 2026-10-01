using Operations.Domain.Enums;

namespace Operations.Domain.Dtos
{
    public class ExtractionHistoryItemDto
    {
        public Guid Id { get; set; }
        public Guid DocumentId { get; set; }
        public string Provider { get; set; } = string.Empty;
        public ProcessingStatus Status { get; set; }
        public ExtractionTrigger Trigger { get; set; }
        public string? OcrRequestId { get; set; }
        public ExtractionMethod ExtractionMethod { get; set; }
        public decimal? Confidence { get; set; }
        public string? ContentHash { get; set; }
        public bool FallbackUsed { get; set; }
        public DateTime? ProcessingStartedAt { get; set; }
        public DateTime? ProcessingCompletedAt { get; set; }
        public long? ProcessingDurationMs { get; set; }
        public string? ErrorCode { get; set; }
        public string? ErrorMessage { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}
