using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using SharedKernel.Models;

namespace Operations.Domain.Entities
{
    /// <summary>
    /// Page of a stored document.
    /// </summary>
    public class DocumentPage : BaseModel
    {
        [Key]
        [Required]
        public Guid Id { get; set; }

        [Required]
        [ForeignKey("Document")]
        public Guid DocumentId { get; set; }

        public Document Document { get; set; } = null!;

        public int PageNumber { get; set; }

        [MaxLength(500)]
        public string? StorageReference { get; set; }

        public int RotationDegrees { get; set; }

        [MaxLength(50)]
        public string? OcrStatus { get; set; }
    }
}
