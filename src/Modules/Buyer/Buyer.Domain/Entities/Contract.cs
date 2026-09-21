using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;
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
        [DeleteBehavior(DeleteBehavior.NoAction)]
        public BuyerBusinessProfile BuyerBusinessProfile { get; set; }

        [Required]
        public Guid SupplierId { get; set; }

        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public decimal Amount { get; set; }
        public string Status { get; set; }

        /// <summary>
        /// Set to CONTRACT_CREATED when the contract is created; shown to the supplier once the RFQ is awarded.
        /// </summary>
        public string? ContractStatus { get; set; }
        public Contract() { }
    }
}
