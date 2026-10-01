using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Operations.Domain.Enums;
using SharedKernel.Models;

namespace Operations.Domain.Entities
{
    /// <summary>
    /// Stored document (invoice scan or upload). CreatedBy is the user who uploaded it.
    /// </summary>
    public class Document : BaseModel
    {
        [Key]
        [Required]
        public Guid Id { get; set; }

        [Required]
        public Guid OrganizationId { get; set; }

        public Guid? OperatingUnitId { get; set; }

        [Column(TypeName = "nvarchar(50)")]
        public DocumentType DocumentType { get; set; }

        [Required]
        [MaxLength(500)]
        public string OriginalFilename { get; set; } = string.Empty;

        [Required]
        [MaxLength(150)]
        public string ContentType { get; set; } = string.Empty;

        public long FileSizeBytes { get; set; }

        [Required]
        [MaxLength(50)]
        public string StorageProvider { get; set; } = string.Empty;

        [Required]
        [MaxLength(500)]
        public string StorageReference { get; set; } = string.Empty;

        [MaxLength(100)]
        public string? ScanSessionId { get; set; }

        [MaxLength(100)]
        public string? OcrRequestId { get; set; }

        [MaxLength(64)]
        public string? ContentHash { get; set; }

        public int? PageCount { get; set; }

        [Column(TypeName = "nvarchar(50)")]
        public DocumentStatus Status { get; set; }

        [Column(TypeName = "nvarchar(50)")]
        public DocumentSourceChannel SourceChannel { get; set; }
    }
}
