using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using SharedKernel.Models;

namespace Buyer.Domain.Entities
{
    public class RFQSupplierMapping : BaseModel
    {
        [Key]
        public Guid Id { get; set; }

        [Required]
        [ForeignKey("RFQ")]
        public Guid RFQId { get; set; }

        public RFQ RFQ { get; set; }

        public string RFQNumber { get; set; }

        public Guid BuyerId { get; set; }

        public Guid SupplierId { get; set; }

        /// <summary>ACCEPTED, REJECTED or PENDING - see Buyer.Domain.Common.Common.</summary>
        public string SupplierTermsAndConditionAccepted { get; set; } 

        public RFQSupplierMapping()
        {
        }
    }
}