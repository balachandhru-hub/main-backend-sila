
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using SharedKernel.Models;

namespace Buyer.Domain.Entities
{
    public class BuyerSupplierMapping : BaseModel
    {
        [Key]
        [Required]
        public Guid Id { get; set; }

        [Required]
        [ForeignKey("BuyerBusinessProfile")]
        public Guid BuyerId { get; set; }
        public BuyerBusinessProfile BuyerBusinessProfile { get; set; }

        [Required]
        public Guid SupplierId { get; set; }
        public BuyerSupplierMapping()
        {
        }
    }
}