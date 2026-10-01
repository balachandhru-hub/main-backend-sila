using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Operations.Domain.Enums;
using SharedKernel.Models;

namespace Operations.Domain.Entities
{
    /// <summary>
    /// Connection of an organization to an external document store (Microsoft SharePoint).
    /// </summary>
    public class DocumentStorageConnection : BaseModel
    {
        [Key]
        [Required]
        public Guid Id { get; set; }

        [Required]
        public Guid OrganizationId { get; set; }

        [Column(TypeName = "nvarchar(20)")]
        public DocumentStorageProvider Provider { get; set; }

        [Required]
        [MaxLength(200)]
        public string Name { get; set; } = string.Empty;

        [Column(TypeName = "nvarchar(30)")]
        public StorageConnectionStatus ConnectionStatus { get; set; } = StorageConnectionStatus.NOT_CONNECTED;

        [MaxLength(250)]
        public string? TenantIdentifier { get; set; }

        [MaxLength(250)]
        public string? SiteIdentifier { get; set; }

        [MaxLength(250)]
        public string? DriveIdentifier { get; set; }

        [MaxLength(250)]
        public string? FolderIdentifier { get; set; }

        [MaxLength(1000)]
        public string? FolderPath { get; set; }

        [MaxLength(1000)]
        public string? DisplayUrl { get; set; }

        [MaxLength(250)]
        public string? DisplayName { get; set; }

        [MaxLength(200)]
        public string? DriveName { get; set; }

        public string? CredentialReference { get; set; }

        public DateTime? ValidatedAt { get; set; }

        public DateTime? ConnectedAt { get; set; }

        public DateTime? LastTestedAt { get; set; }

        [MaxLength(100)]
        public string? LastTestStatus { get; set; }

        public Guid? ValidatedByUserId { get; set; }
    }
}
