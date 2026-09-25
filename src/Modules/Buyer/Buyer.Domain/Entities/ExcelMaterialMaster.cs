using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using SharedKernel.Models;

namespace Buyer.Domain.Entities
{
    public class ExcelMaterialMaster : BaseModel
    {
        [Key]
        [Required]
        public Guid Id { get; set; }

        [Required]
        [ForeignKey("Asset")]
        public Guid AssetId { get; set; }

        public Asset Asset { get; set; }

        [Required]
        [ForeignKey("BuyerBusinessProfile")]
        public Guid BuyerId { get; set; }

        public BuyerBusinessProfile BuyerBusinessProfile { get; set; }

        /// <summary>PROCESSING (approval in progress) / COMPLETE (approved, rows created) / REJECT (rejected).</summary>
        public string Status { get; set; }
        public string Title { get; set; }
        public ExcelMaterialMaster() { }
    }
}
