using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Operations.Domain.Enums;
using SharedKernel.Models;

namespace Operations.Domain.Entities
{
    /// <summary>
    /// Per-user document destination. UserId is an Identity user id.
    /// </summary>
    public class UserDocumentStorageAssignment : BaseModel
    {
        [Key]
        [Required]
        public Guid Id { get; set; }

        [Required]
        public Guid UserId { get; set; }

        public Guid? DocumentStorageDestinationId { get; set; }

        [Column(TypeName = "nvarchar(20)")]
        public DocumentStorageProvider Provider { get; set; } = DocumentStorageProvider.NONE;

        public Guid? StorageConnectionId { get; set; }

        [MaxLength(250)]
        public string? SiteIdentifier { get; set; }

        [MaxLength(250)]
        public string? DriveIdentifier { get; set; }

        [MaxLength(250)]
        public string? FolderIdentifier { get; set; }

        [MaxLength(1000)]
        public string? DestinationUrl { get; set; }

        public bool ExternalTransferEnabled { get; set; }

        [Column(TypeName = "nvarchar(30)")]
        public StorageAssignmentStatus Status { get; set; } = StorageAssignmentStatus.NOT_CONFIGURED;

        public DateTime? ValidatedAt { get; set; }
    }
}
