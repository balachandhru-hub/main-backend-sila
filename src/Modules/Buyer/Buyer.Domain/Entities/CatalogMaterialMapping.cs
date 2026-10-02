using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using SharedKernel.Models;

namespace Buyer.Domain.Entities
{
    /// <summary>
    /// Maps a supplier catalog product to an Item Master material of the buyer.
    /// The material code is the one sent on the purchase order.
    /// </summary>
    public class CatalogMaterialMapping : BaseModel
    {
        [Key]
        [Required]
        public Guid Id { get; set; }

        [Required]
        [ForeignKey("BuyerBusinessProfile")]
        public Guid BuyerId { get; set; }

        public BuyerBusinessProfile BuyerBusinessProfile { get; set; } = null!;

        /// <summary>
        /// Supplier catalog id. The catalog lives in the Supplier service, so this is a reference.
        /// </summary>
        [Required]
        public Guid CatalogId { get; set; }

        public string? Sku { get; set; }

        /// <summary>
        /// ItemBuyerMaster id. Kept as a reference, not a foreign key.
        /// </summary>
        [Required]
        public Guid MaterialId { get; set; }

        [Required]
        public string MaterialCode { get; set; } = string.Empty;
    }
}
