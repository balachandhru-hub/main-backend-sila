namespace Supplier.Domain.Dto
{
    public class MessageResponseDto
    {
        public Guid Id { get; set; }

        public Guid ThreadId { get; set; }

        public Guid SenderUserId { get; set; }

        public string? SenderName { get; set; }

        public string SenderOrganizationType { get; set; }

        public string? Body { get; set; }

        public List<MessageAttachmentResponseDto> Attachments { get; set; } = new();

        public DateTime DateCreated { get; set; }

        public bool IsReadByBuyer { get; set; }

        public bool IsReadBySupplier { get; set; }
    }
}
