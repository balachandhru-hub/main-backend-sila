using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Operations.Domain.Enums;
using SharedKernel.Models;

namespace Operations.Domain.Entities
{
    /// <summary>
    /// Transfer of a stored document to an external destination (SharePoint), processed by the transfer worker.
    /// </summary>
    public class DocumentTransferJob : BaseModel
    {
        [Key]
        [Required]
        public Guid Id { get; set; }

        [Required]
        [ForeignKey("Document")]
        public Guid DocumentId { get; set; }

        public Document Document { get; set; } = null!;

        [Required]
        public Guid DestinationId { get; set; }

        [Required]
        public Guid UserId { get; set; }

        [Required]
        public Guid OrganizationId { get; set; }

        public Guid? PropertyId { get; set; }

        public Guid? OperatingUnitId { get; set; }

        [Column(TypeName = "nvarchar(50)")]
        public DocumentType DocumentType { get; set; }

        [Column(TypeName = "nvarchar(20)")]
        public DocumentStorageProvider Provider { get; set; }

        [Column(TypeName = "nvarchar(30)")]
        public DocumentTransferStatus Status { get; set; }

        [MaxLength(30)]
        public string? ResolutionSource { get; set; }

        public int AttemptCount { get; set; }

        public DateTime? NextAttemptAt { get; set; }

        public DateTime? LastAttemptAt { get; set; }

        public DateTime? CompletedAt { get; set; }

        [MaxLength(500)]
        public string? ExternalFileId { get; set; }

        [MaxLength(1000)]
        public string? ExternalWebUrl { get; set; }

        [MaxLength(500)]
        public string? ExternalFileName { get; set; }

        [MaxLength(100)]
        public string? LastErrorCode { get; set; }

        [MaxLength(1000)]
        public string? LastErrorMessageSafe { get; set; }
    }
}
