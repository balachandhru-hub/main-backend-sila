using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;
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

     
        public decimal? Price { get; set; }
         public string Currency { get; set; }

        [Required]
        public string UnitOfMeasure { get; set; }
        public long? Segment {get;set;}
        public string? SegmentTitle  {get;set;}
        public long? Family {get;set;}
        public string? FamilyTitle {get;set;}
        public long? Commodity {get;set;}
        public string? CommodityTitle {get;set;}
        public long? Class {get;set;}
        public string? ClassTitle {get;set;}
        public string CatalogType { get; set; }

        public bool IsPunchOut { get; set; }

        public string? PunchOutUrl { get; set; }

        /// <summary>
        /// Supplier's own product code.
        /// </summary>
        public string? Sku { get; set; }

        /// <summary>
        /// Stock the supplier can deliver, in the catalog unit of measure. Null when the supplier does not publish stock.
        /// </summary>
        [Precision(18, 4)]
        public decimal? AvailableStock { get; set; }

        [Precision(9, 4)]
        public decimal? DiscountPercent { get; set; }
        public SupplierCatalog()
        {
        }
    }
}