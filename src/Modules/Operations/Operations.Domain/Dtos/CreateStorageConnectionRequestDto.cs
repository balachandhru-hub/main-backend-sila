using System.ComponentModel.DataAnnotations;
using Operations.Domain.Enums;

namespace Operations.Domain.Dtos
{
    public class CreateStorageConnectionRequestDto
    {
        [Required]
        public DocumentStorageProvider Provider { get; set; }

        [Required, StringLength(200, MinimumLength = 2)]
        public string Name { get; set; } = string.Empty;

        public string? TenantIdentifier { get; set; }
        public string? SiteIdentifier { get; set; }
        public string? DriveIdentifier { get; set; }
        public string? FolderIdentifier { get; set; }
        public string? DisplayUrl { get; set; }
    }
}
