using System.ComponentModel.DataAnnotations;
using MasterData.Domain.Common;

namespace MasterData.Domain.Entities
{
    public class EmailContent : BaseEntity
    {
        [Key]
        [Required]
        public Guid Id { get; set; }

        [Required]
        public string? Key { get; set; }

        [Required]
        public string? Subject { get; set; }

        [Required]
        public string? Body { get; set; }

        public EmailContent() { }
    }
}