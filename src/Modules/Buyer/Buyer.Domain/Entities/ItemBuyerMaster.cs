using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using SharedKernel.Models;
namespace Buyer.Domain.Entities
{
    public class ItemBuyerMaster : BaseModel
    {

        [Key]
        [Required]
        public Guid Id { get; set; }
        [Required]
        [ForeignKey("BuyerBusinessProfile")]
        public Guid BuyerId { get; set; }
        public BuyerBusinessProfile BuyerBusinessProfile { get; set; }
        public string Description { get; set; }
        public string MaterialCode { get; set; }
        public string MaterialGroup { get; set; }
        public string ProductType { get; set; }
          public string BaseUnitOfMeasure { get; set; }

        
        public string OrderUnitOfMeasure { get; set; }

      
        public string AlternateUnitOfMeasure { get; set; }

        
        public string ValuationClass { get; set; }

    
        public string UnitOfMeasureMapping { get; set; }

        public string? SubUnit { get; set; }

        public string? MicroUnit { get; set; }

        public ItemBuyerMaster() { }
    }
}