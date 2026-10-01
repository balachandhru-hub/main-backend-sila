using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using SharedKernel.Models;

namespace Operations.Domain.Entities
{
    /// <summary>
    /// Supplier-specific code and description of a material.
    /// </summary>
    public class SupplierMaterial : BaseModel
    {
        [Key]
        [Required]
        public Guid Id { get; set; }

        [Required]
        public Guid OrganizationId { get; set; }

        [Required]
        [ForeignKey("Supplier")]
        public Guid SupplierId { get; set; }

        public SupplierMaster Supplier { get; set; } = null!;

        [Required]
        public Guid MaterialId { get; set; }

        [MaxLength(150)]
        public string? SupplierMaterialCode { get; set; }

        [MaxLength(500)]
        public string? SupplierDescription { get; set; }

        [MaxLength(50)]
        public string? PurchaseUom { get; set; }
    }
}
