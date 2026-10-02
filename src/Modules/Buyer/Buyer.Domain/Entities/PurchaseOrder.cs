using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;
using SharedKernel.Models;

namespace Buyer.Domain.Entities
{
    /// <summary>
    /// A purchase order created in the ERP of the buyer. One order is for one supplier.
    /// </summary>
    public class PurchaseOrder : BaseModel
    {
        [Key]
        [Required]
        public Guid Id { get; set; }

        [Required]
        [ForeignKey("BuyerBusinessProfile")]
        public Guid BuyerId { get; set; }

        public BuyerBusinessProfile BuyerBusinessProfile { get; set; } = null!;

        [Required]
        public Guid BuyerOrganizationId { get; set; }

        /// <summary>
        /// Document number given by the ERP.
        /// </summary>
        [Required]
        public string PoNumber { get; set; } = string.Empty;

        /// <summary>
        /// Supplier id of the Supplier service. A reference, not a foreign key.
        /// </summary>
        [Required]
        public Guid SupplierId { get; set; }

        public string? SupplierName { get; set; }

        /// <summary>
        /// What the order was created from, for example a weekly bucket.
        /// </summary>
        [Required]
        public string SourceType { get; set; } = string.Empty;

        /// <summary>
        /// Weekly bucket the order was created from. Kept as a reference, not a foreign key.
        /// </summary>
        public Guid? WeeklyBucketId { get; set; }

        public string? BucketCode { get; set; }

        public string? CompanyCode { get; set; }

        public string? PlantCode { get; set; }

        public string? Currency { get; set; }

        [Precision(18, 4)]
        public decimal TotalAmount { get; set; }

        [Required]
        public string Status { get; set; } = string.Empty;

        public DateTime OrderDate { get; set; }

        /// <summary>
        /// Name of the ERP system the order was created in.
        /// </summary>
        public string? SourceSystem { get; set; }

        public ICollection<PurchaseOrderItem> Items { get; set; } = new List<PurchaseOrderItem>();
    }
}
