using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Operations.Domain.Enums;
using SharedKernel.Models;

namespace Operations.Domain.Entities
{
    /// <summary>
    /// Folder of a storage connection that receives the documents of an organization or of one operating unit.
    /// </summary>
    public class DocumentStorageDestination : BaseModel
    {
        [Key]
        [Required]
        public Guid Id { get; set; }

        [Required]
        public Guid OrganizationId { get; set; }

        public Guid? PropertyId { get; set; }

        public Guid? OperatingUnitId { get; set; }

        public Guid? LocationId { get; set; }

        [Required]
        [ForeignKey("StorageConnection")]
        public Guid StorageConnectionId { get; set; }

        public DocumentStorageConnection StorageConnection { get; set; } = null!;

        [Column(TypeName = "nvarchar(20)")]
        public DocumentStorageProvider Provider { get; set; }

        [Column(TypeName = "nvarchar(50)")]
        public DocumentType DocumentType { get; set; } = DocumentType.INVOICE;

        [MaxLength(250)]
        public string? SiteIdentifier { get; set; }

        [MaxLength(250)]
        public string? DriveIdentifier { get; set; }

        [MaxLength(250)]
        public string? FolderIdentifier { get; set; }

        [Required]
        [MaxLength(1000)]
        public string FolderPath { get; set; } = string.Empty;

        [MaxLength(1000)]
        public string? DisplayUrl { get; set; }

        public bool ExternalTransferEnabled { get; set; }

        [Column(TypeName = "nvarchar(30)")]
        public StorageDestinationStatus Status { get; set; } = StorageDestinationStatus.VALIDATION_REQUIRED;

        public DateTime? ValidatedAt { get; set; }
    }
}
