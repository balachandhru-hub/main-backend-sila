using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using SharedKernel.Models;

namespace Buyer.Domain.Entities
{
    public class RFQItem : BaseModel
    {
        [Key]
        [Required]
        public Guid Id { get; set; }
        [Required]
        [ForeignKey("RFQ")]
        public Guid RFQId { get; set; }
        public RFQ RFQ {get; set;}

        public string Description { get; set; }

        public decimal Quantity { get; set; }

        public string UOM { get; set; }
        public string MaterialCode{get;set;}
        public string MaterialGroup{get;set;}
        public RFQItem(){}

    }
}
