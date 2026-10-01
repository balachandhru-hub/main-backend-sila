using System.ComponentModel.DataAnnotations;

namespace Operations.Domain.Dtos
{
    public class MicrosoftFolderListRequestDto
    {
        [Required]
        public string DriveId { get; set; } = string.Empty;

        public string? FolderPath { get; set; }
    }
}
