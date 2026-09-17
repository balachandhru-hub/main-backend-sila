using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using SharedKernel.Models;

namespace Buyer.Domain.Entities
{
    public class MessageThread : BaseModel
    {
        [Key]
        public Guid Id { get; set; }

        [Required]
        [ForeignKey("RFQ")]
        public Guid RFQId { get; set; }

        public RFQ RFQ { get; set; }

        public string RFQNumber { get; set; }

        public Guid BuyerId { get; set; }

        public Guid? SupplierId { get; set; }

        [ForeignKey("ExternalSupplier")]
        public Guid? ExternalSupplierId { get; set; }

        public ExternalSupplier ExternalSupplier { get; set; }

        public DateTime? LastMessageAt { get; set; }

        public MessageThread()
        {
        }
    }
}
