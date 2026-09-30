using System.ComponentModel.DataAnnotations;

namespace Supplier.Domain.Dto
{
    public class SendMessageDto
    {
        [Required]
        public Guid RFQId { get; set; }

        [Required]
        public Guid SupplierId { get; set; }

        public string? Body { get; set; }

        public List<MessageAttachmentUploadDto>? Attachments { get; set; }
    }
}
