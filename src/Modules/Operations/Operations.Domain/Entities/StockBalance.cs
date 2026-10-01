using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;
using SharedKernel.Models;

namespace Operations.Domain.Entities
{
    /// <summary>
    /// On-hand quantity of a material in an operating unit.
    /// </summary>
    public class StockBalance : BaseModel
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

        [Precision(18, 4)]
        public decimal Quantity { get; set; }

        [Required]
        [MaxLength(50)]
        public string Uom { get; set; } = string.Empty;
    }
}
