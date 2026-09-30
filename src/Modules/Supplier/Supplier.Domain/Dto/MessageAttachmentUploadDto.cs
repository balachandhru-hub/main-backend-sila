using System.ComponentModel.DataAnnotations;

namespace Supplier.Domain.Dto
{
    public class MessageAttachmentUploadDto
    {
        [Required]
        public byte[] FileBytes { get; set; }

        [Required]
        public string FileName { get; set; }

        public string? ContentType { get; set; }
    }
}
