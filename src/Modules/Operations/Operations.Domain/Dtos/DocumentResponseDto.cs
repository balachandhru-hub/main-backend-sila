using Operations.Domain.Enums;

namespace Operations.Domain.Dtos
{
    public class DocumentResponseDto
    {
        public Guid Id { get; set; }
        public string Filename { get; set; } = string.Empty;
        public string ContentType { get; set; } = string.Empty;
        public long FileSizeBytes { get; set; }
        public int? PageCount { get; set; }
        public DocumentSourceChannel SourceChannel { get; set; }
        public DocumentStatus Status { get; set; }
        public DateTime CreatedAt { get; set; }
        public Guid InvoiceId { get; set; }
        public string SaveStatus { get; set; } = "SAVED";
        public string NextStep { get; set; } = "PO_MATCH";
        public string? Message { get; set; }
    }
}
