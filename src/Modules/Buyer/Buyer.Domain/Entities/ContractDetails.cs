using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using SharedKernel.Models;

namespace Buyer.Domain.Entities
{
    public class ContractDetails : BaseModel
    {
        [Key]
        [Required]
        public Guid Id { get; set; }

        [Required]
        [ForeignKey("PredefinedContract")]
        public Guid PredefinedContractId { get; set; }
        public PredefinedContract PredefinedContract { get; set; }

        public string? ContractNumber { get; set; }
        public string? ContractName { get; set; }

        public Guid RFQId { get; set; }

        [Required]
        [ForeignKey("BuyerBusinessProfile")]
        public Guid BuyerId { get; set; }
        public BuyerBusinessProfile BuyerBusinessProfile { get; set; }

        public Guid SupplierId { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public decimal Amount { get; set; }
        public string? Status { get; set; }
        public string? ContractStatus { get; set; }

        public ContractDetails() { }
    }
}
