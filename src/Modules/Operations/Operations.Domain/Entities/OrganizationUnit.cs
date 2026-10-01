using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Operations.Domain.Enums;
using SharedKernel.Models;

namespace Operations.Domain.Entities
{
    /// <summary>
    /// Property / hotel / outlet / kitchen / store tree of one organization. OrganizationId is the Identity organization id from the token.
    /// </summary>
    public class OrganizationUnit : BaseModel
    {
        [Key]
        [Required]
        public Guid Id { get; set; }

        [Required]
        public Guid OrganizationId { get; set; }

        public Guid? ParentUnitId { get; set; }

        [Required]
        [MaxLength(64)]
        public string Code { get; set; } = string.Empty;

        [Required]
        [MaxLength(200)]
        public string Name { get; set; } = string.Empty;

        [Column(TypeName = "nvarchar(32)")]
        public OrganizationUnitKind Kind { get; set; }

        [Column(TypeName = "nvarchar(16)")]
        public StatusKind Status { get; set; } = StatusKind.ACTIVE;
    }
}
