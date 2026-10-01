using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Operations.Domain.Enums;
using SharedKernel.Models;

namespace Operations.Domain.Entities
{
    /// <summary>
    /// Idempotency key of a write request (invoice save, GRN post).
    /// </summary>
    public class IdempotencyRecord : BaseModel
    {
        [Key]
        [Required]
        public Guid Id { get; set; }

        [Required]
        public Guid OrganizationId { get; set; }

        [Required]
        public Guid UserId { get; set; }

        [Required]
        [MaxLength(200)]
        public string IdempotencyKey { get; set; } = string.Empty;

        [Required]
        [MaxLength(100)]
        public string Operation { get; set; } = string.Empty;

        [MaxLength(200)]
        public string? RequestHash { get; set; }

        [MaxLength(200)]
        public string? ResponseReference { get; set; }

        [Column(TypeName = "nvarchar(30)")]
        public IdempotencyStatus Status { get; set; }

        public DateTime? ExpiresAt { get; set; }
    }
}
