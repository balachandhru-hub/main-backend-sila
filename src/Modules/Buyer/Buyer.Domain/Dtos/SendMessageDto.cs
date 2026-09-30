using System.ComponentModel.DataAnnotations;

namespace Buyer.Domain.Dto
{
    public class SendMessageDto
    {
        [Required]
        public Guid RFQId { get; set; }

        /// <summary>Required when sending to a real Supplier. Mutually exclusive with <see cref="ExternalSupplierId"/>.</summary>
        public Guid? SupplierId { get; set; }

        /// <summary>Required when sending to an ExternalSupplier. Mutually exclusive with <see cref="SupplierId"/>.</summary>
        public Guid? ExternalSupplierId { get; set; }

        public string? Body { get; set; }

        public List<MessageAttachmentUploadDto>? Attachments { get; set; }
    }
}
