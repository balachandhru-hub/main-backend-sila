using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using SharedKernel.Models;

namespace Supplier.Domain.Entities
{
    public class SupplierCatalog : BaseModel
    {
        [Key]
        [Required]
        public Guid Id { get; set; }

        [Required]
        [ForeignKey("SupplierBusinessProfile")]
        public Guid SupplierId { get; set; }

        public SupplierBusinessProfile Supplier { get; set; }
        public string CatalogName { get; set; }
        [Required]
        public string Description { get; set; }

     
        public decimal Price { get; set; }

        [Required]
        public string UnitOfMeasure { get; set; }
        public string? SegmentName {get;set;}
        public string? SegmentCode {get;set;}
        public string? FamilyName {get;set;}
        public string? FamilyCode {get;set;}
        public string? CommodityCode {get;set;}

        public SupplierCatalog()
        {
        }
    }
}