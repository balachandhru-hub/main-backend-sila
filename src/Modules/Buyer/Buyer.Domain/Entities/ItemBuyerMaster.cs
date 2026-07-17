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
        public ItemBuyerMaster() { }
    }
}