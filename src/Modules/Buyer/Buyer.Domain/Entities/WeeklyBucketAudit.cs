using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using SharedKernel.Models;

namespace Buyer.Domain.Entities
{
    public class WeeklyBucketAudit : BaseModel
    {
        [Key]
        [Required]
        public Guid Id { get; set; }

        [Required]
        [ForeignKey("WeeklyBucket")]
        public Guid WeeklyBucketId { get; set; }

        public WeeklyBucket WeeklyBucket { get; set; } = null!;

        [Required]
        public string Action { get; set; } = string.Empty;

        public string? Detail { get; set; }

        public Guid? ActorUserId { get; set; }
    }
}
