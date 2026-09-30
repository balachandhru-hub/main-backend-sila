using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using SharedKernel.Models;

namespace Supplier.Domain.Entities
{
    public class RFQOrganizationUserMapping : BaseModel
    {
        [Key]
        [Required]
        public Guid Id { get; set; }

        [Required]
        public Guid BuyerRFQId { get; set; }

        [Required]
        public string RFQNumber { get; set; }

        [Required]
        public Guid BuyerId { get; set; }

        [Required]
        public Guid SupplierId { get; set; }

        [Required]
        [ForeignKey(nameof(SupplierRFQ))]
        public Guid SupplierRFQId { get; set; }

        public SupplierRFQ SupplierRFQ { get; set; }

        public Guid OrganizationId { get; set; }

        public Guid UserId { get; set; }

        public RFQOrganizationUserMapping()
        {
        }
    }
}
