using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using SharedKernel.Models;

namespace Buyer.Domain.Entities
{
    public class Contract : BaseModel
    {
        [Key]
        [Required]
        public Guid Id { get; set; }

        public string ContractNumber { get; set; }

        [Required]
        [ForeignKey("RFQ")]
        public Guid RFQId { get; set; }
        public RFQ RFQ { get; set; }
        public string ContractName { get; set; }

        [Required]
        [ForeignKey("BuyerBusinessProfile")]
        public Guid BuyerId { get; set; }
        public BuyerBusinessProfile BuyerBusinessProfile { get; set; }

        [Required]
        public Guid SupplierId { get; set; }

        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public decimal Amount { get; set; }
        public string Status { get; set; }
        public Contract() { }
    }
}
