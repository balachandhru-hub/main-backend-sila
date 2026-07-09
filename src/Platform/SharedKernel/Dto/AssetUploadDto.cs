using System.ComponentModel.DataAnnotations;

namespace SharedKernel.Dto
{
    public class AssetUploadDto
    {
        public string? EntityType { get; set; }
        [Required]
        public Guid EntityId { get; set; }
 
        public string? AssetType { get; set; }
        [Required]
        public byte[] FileBytes { get; set; }

        public string? FileName { get; set; }
        public string? ContentType { get; set; }
        public bool IsSingletonAsset { get; set; }

        public Guid? Id { get; set; }
    }
}