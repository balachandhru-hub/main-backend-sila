namespace Supplier.Domain.Dto
{
    public class MessageAttachmentResponseDto
    {
        public Guid Id { get; set; }

        public string FileName { get; set; }

        public string? ContentType { get; set; }

        public long FileSizeBytes { get; set; }
    }
}
