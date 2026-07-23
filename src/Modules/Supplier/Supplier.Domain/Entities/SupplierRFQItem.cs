using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using SharedKernel.Models;

namespace Supplier.Domain.Entities
{
    public class SupplierRFQItem : BaseModel
    {
        [Key]
        [Required]
        public Guid Id { get; set; }

        [Required]
        [ForeignKey(nameof(SupplierRFQ))]
        public Guid SupplierRFQId { get; set; }

        public SupplierRFQ SupplierRFQ { get; set; }

        [Required]
        public Guid BuyerRFQItemId { get; set; }

        public string Description { get; set; }

        public decimal Quantity { get; set; }

        public string UOM { get; set; }

        public string? MaterialCode { get; set; }

        public string? MaterialGroup { get; set; }

        public string? CostCenter { get; set; }

        public SupplierRFQItem()
        {
        }
    }
}