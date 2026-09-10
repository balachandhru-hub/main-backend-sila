using System.ComponentModel.DataAnnotations;

namespace Buyer.Domain.Dto
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
