
using System.ComponentModel.DataAnnotations;
using SharedKernel.Models;

namespace Buyer.Domain.Entities
{
    public class RFQItem : BaseModel
    {
        [Key]
        public Guid Id { get; set; }

        public Guid RFQId { get; set; }

        public string Description { get; set; }

        public decimal Quantity { get; set; }

        public string UOM { get; set; }
        public string MaterialCode{get;set;}
        public string MaterialGroup{get;set;}
public RFQItem(){}

    }
}
