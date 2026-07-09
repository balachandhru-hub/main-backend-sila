using System.ComponentModel.DataAnnotations;

namespace SharedKernel.Dto
{
    public class AssetDownloadDto
    {
        [Required]
        public Guid AssetId { get; set; }

        [Required]
        public string? FileName { get; set; }

        [Required]
        public required string ContentType { get; set; }

        public required byte[] FileBytes { get; set; }
    }
}