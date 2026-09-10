using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using SharedKernel.Models;

namespace Buyer.Domain.Entities
{
    public class Message : BaseModel
    {
        [Key]
        public Guid Id { get; set; }

        [Required]
        [ForeignKey("MessageThread")]
        public Guid ThreadId { get; set; }

        public MessageThread MessageThread { get; set; }

        public Guid SenderUserId { get; set; }

        public string SenderOrganizationType { get; set; }

        public Guid SenderOrganizationId { get; set; }

        public string? Body { get; set; }

        public bool IsReadByBuyer { get; set; }

        public bool IsReadBySupplier { get; set; }

        public DateTime? ReadByBuyerAt { get; set; }

        public DateTime? ReadBySupplierAt { get; set; }

        public Message()
        {
        }
    }
}
