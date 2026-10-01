using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;
using Operations.Domain.Enums;
using SharedKernel.Models;

namespace Operations.Domain.Entities
{
    /// <summary>
    /// Stock movement created by a posted goods receipt. CreatedBy is the receiving user.
    /// </summary>
    public class InventoryTransaction : BaseModel
    {
        [Key]
        [Required]
        public Guid Id { get; set; }

        [Required]
        public Guid OrganizationId { get; set; }

        [Required]
        public Guid OperatingUnitId { get; set; }

        public Guid? MaterialId { get; set; }

        [Required]
        [MaxLength(150)]
        public string MaterialCode { get; set; } = string.Empty;

        [Column(TypeName = "nvarchar(50)")]
        public InventoryTransactionType TransactionType { get; set; }

        [Required]
        [MaxLength(50)]
        public string ReferenceType { get; set; } = string.Empty;

        [Required]
        public Guid ReferenceId { get; set; }

        [Precision(18, 4)]
        public decimal Quantity { get; set; }

        [Required]
        [MaxLength(50)]
        public string Uom { get; set; } = string.Empty;
    }
}
