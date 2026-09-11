using System.ComponentModel.DataAnnotations;

namespace Buyer.Domain.Dto
{
    public class SendMessageDto
    {
        [Required]
        public Guid RFQId { get; set; }

        [Required]
        [MinLength(1)]
        public List<Guid> SupplierId { get; set; }

        public string? Body { get; set; }

        public List<MessageAttachmentUploadDto>? Attachments { get; set; }
    }
}
