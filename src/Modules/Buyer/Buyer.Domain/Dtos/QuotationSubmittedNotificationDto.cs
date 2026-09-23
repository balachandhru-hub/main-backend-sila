using System.ComponentModel.DataAnnotations;

namespace Buyer.Domain.Dto
{
    public class QuotationSubmittedNotificationDto
    {
        [Required]
        public Guid RFQId { get; set; }

        [Required]
        public Guid SupplierId { get; set; }

        [Required]
        public Guid QuotationId { get; set; }
    }
}
