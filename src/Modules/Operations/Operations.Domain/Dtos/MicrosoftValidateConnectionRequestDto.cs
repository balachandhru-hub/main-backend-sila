using System.ComponentModel.DataAnnotations;

namespace Operations.Domain.Dtos
{
    public class MicrosoftValidateConnectionRequestDto
    {
        [Required, Url]
        public string SiteUrl { get; set; } = string.Empty;

        public string? DriveId { get; set; }
        public string? DriveName { get; set; }
        public string? FolderId { get; set; }
        public string? FolderPath { get; set; }
    }
}
