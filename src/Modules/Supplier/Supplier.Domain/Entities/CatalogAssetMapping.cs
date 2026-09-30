using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using SharedKernel.Models;

namespace Supplier.Domain.Entities
{
    public class CatalogAssetMapping : BaseModel
    {
        [Key]
        [Required]
        public Guid Id { get; set; }

        [Required]
        [ForeignKey("SupplierCatalog")]
        public Guid CatalogId { get; set; }

        public SupplierCatalog SupplierCatalog { get; set; }

        [Required]
        public Guid AssetId { get; set; }

        public CatalogAssetMapping()
        {
        }
    }
}