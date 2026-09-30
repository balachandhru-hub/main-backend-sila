using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using SharedKernel.Models;

namespace Buyer.Domain.Entities
{
    public class MessageAttachment : BaseModel
    {
        [Key]
        public Guid Id { get; set; }

        [Required]
        [ForeignKey("Message")]
        public Guid MessageId { get; set; }

        public Message Message { get; set; }

        public string FileName { get; set; }

        public string? ContentType { get; set; }

        public long FileSizeBytes { get; set; }

        public string StoragePath { get; set; }

        public MessageAttachment()
        {
        }
    }
}
