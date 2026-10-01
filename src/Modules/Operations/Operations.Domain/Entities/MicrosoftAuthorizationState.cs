using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using SharedKernel.Models;

namespace Operations.Domain.Entities
{
    /// <summary>
    /// One-time OAuth state of a Microsoft sign-in started from the storage setup screen.
    /// </summary>
    public class MicrosoftAuthorizationState : BaseModel
    {
        [Key]
        [Required]
        public Guid Id { get; set; }

        [Required]
        [MaxLength(128)]
        public string StateHash { get; set; } = string.Empty;

        [Required]
        public Guid UserId { get; set; }

        [Required]
        public Guid OrganizationId { get; set; }

        [Required]
        [MaxLength(200)]
        public string ReturnUrl { get; set; } = string.Empty;

        [Required]
        public string DraftJson { get; set; } = string.Empty;

        public DateTime ExpiresAt { get; set; }

        public DateTime? UsedAt { get; set; }
    }
}
