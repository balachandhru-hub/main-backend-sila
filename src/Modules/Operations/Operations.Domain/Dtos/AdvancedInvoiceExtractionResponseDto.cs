namespace Operations.Domain.Dtos
{
    public class AdvancedInvoiceExtractionResponseDto
    {
        public Guid? DocumentId { get; set; }
        public string Provider { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public string Trigger { get; set; } = string.Empty;
        public decimal? Confidence { get; set; }
        public AdvancedInvoiceHeaderResponseDto Header { get; set; } = new();
        public List<AdvancedInvoiceLineResponseDto> Lines { get; set; } = new();
        public AdvancedInvoiceValidationResponseDto Validation { get; set; } = new();
        public bool RequiresReview { get; set; }
        public bool FallbackUsed { get; set; }
        public DateTime? StartedAt { get; set; }
        public DateTime? CompletedAt { get; set; }
        public int PageCount { get; set; }
        public string? ErrorMessage { get; set; }
        public string? OcrRequestId { get; set; }
        public string? RawText { get; set; }
        public AdvancedOcrConfigurationSnapshotDto? EffectiveConfiguration { get; set; }
    }
}
