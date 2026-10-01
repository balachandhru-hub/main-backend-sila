using Operations.Domain.Enums;

namespace Operations.Domain.Dtos
{
    public class InvoiceExtractionResponseDto
    {
        public InvoiceExtractionHeaderResponseDto Header { get; set; } = new();
        public List<InvoiceExtractionLineResponseDto> Lines { get; set; } = new();
        public string Provider { get; set; } = string.Empty;
        public string ExtractionMethod { get; set; } = string.Empty;
        public decimal? Confidence { get; set; }
        public bool FallbackUsed { get; set; }
        public string Status { get; set; } = string.Empty;
        public DateTime? CompletedAt { get; set; }
    }
}
