using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using SharedKernel.Models;

namespace Operations.Domain.Entities
{
    /// <summary>
    /// Audit trail of the receiving process. DateCreated is the time of the event.
    /// </summary>
    public class AuditEvent : BaseModel
    {
        [Key]
        [Required]
        public Guid Id { get; set; }

        public Guid? OrganizationId { get; set; }

        public Guid? OperatingUnitId { get; set; }

        public Guid? UserId { get; set; }

        [Required]
        [MaxLength(100)]
        public string EventType { get; set; } = string.Empty;

        [Required]
        [MaxLength(100)]
        public string EntityType { get; set; } = string.Empty;

        public Guid? EntityId { get; set; }

        [MaxLength(200)]
        public string? Reference { get; set; }

        public string? MetadataJson { get; set; }

        public string? OldStateJson { get; set; }

        public string? NewStateJson { get; set; }

        public Guid? ChangedByUserId { get; set; }

        [MaxLength(500)]
        public string? Reason { get; set; }

        [MaxLength(30)]
        public string? Result { get; set; }
    }
}
