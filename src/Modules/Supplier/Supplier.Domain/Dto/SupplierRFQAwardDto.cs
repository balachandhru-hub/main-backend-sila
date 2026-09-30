using System.ComponentModel.DataAnnotations;

namespace Supplier.Domain.Dto
{
    public class SaveSupplierRFQAwardDto
    {
        [Required]
        public Guid BuyerRFQId { get; set; }

        [Required]
        public List<SupplierRFQAwardItemDto> Items { get; set; } = new();
    }

    public class SupplierRFQAwardItemDto
    {
        [Required]
        public Guid BuyerRFQItemId { get; set; }

        [Required]
        public Guid SupplierId { get; set; }
    }

    public class ResetSupplierRFQAwardDto
    {
        [Required]
        public Guid BuyerRFQId { get; set; }
    }
}
