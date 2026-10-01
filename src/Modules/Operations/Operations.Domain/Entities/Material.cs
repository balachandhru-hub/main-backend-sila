using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Operations.Domain.Enums;
using SharedKernel.Models;

namespace Operations.Domain.Entities
{
    /// <summary>
    /// Material master record of an organization.
    /// </summary>
    public class Material : BaseModel
    {
        [Key]
        [Required]
        public Guid Id { get; set; }

        [Required]
        public Guid OrganizationId { get; set; }

        [Required]
        [MaxLength(100)]
        public string MaterialCode { get; set; } = string.Empty;

        [Required]
        [MaxLength(500)]
        public string Description { get; set; } = string.Empty;

        [Required]
        [MaxLength(500)]
        public string NormalizedDescription { get; set; } = string.Empty;

        [Required]
        [MaxLength(50)]
        public string BaseUom { get; set; } = string.Empty;

        [MaxLength(150)]
        public string? Category { get; set; }

        [Column(TypeName = "nvarchar(16)")]
        public StatusKind Status { get; set; } = StatusKind.ACTIVE;

        [MaxLength(100)]
        public string? SourceSystem { get; set; }

        [MaxLength(250)]
        public string? ExternalId { get; set; }
    }
}
