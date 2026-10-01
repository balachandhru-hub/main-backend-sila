using Operations.Domain.Enums;

namespace Operations.Domain.Dtos
{
    public class DocumentStatusResponseDto
    {
        public Guid DocumentId { get; set; }
        public Guid InvoiceId { get; set; }
        public DocumentStatus DocumentStatus { get; set; }
        public InvoiceStatus InvoiceStatus { get; set; }
        public string? Message { get; set; }
    }
}
