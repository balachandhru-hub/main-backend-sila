using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;
using Operations.Domain.Enums;
using SharedKernel.Models;

namespace Operations.Domain.Entities
{
    /// <summary>
    /// One extraction run (embedded text, OCR or external agent) of a document. Rows are history and are never updated by a re-read.
    /// </summary>
    public class DocumentExtraction : BaseModel
    {
        [Key]
        [Required]
        public Guid Id { get; set; }

        [Required]
        [ForeignKey("Document")]
        public Guid DocumentId { get; set; }

        public Document Document { get; set; } = null!;

        [Column(TypeName = "nvarchar(50)")]
        public ExtractionType ExtractionType { get; set; }

        [Required]
        [MaxLength(100)]
        public string Provider { get; set; } = string.Empty;

        public string? RawText { get; set; }

        [MaxLength(500)]
        public string? ProviderReference { get; set; }

        [Precision(5, 4)]
        public decimal? Confidence { get; set; }

        public DateTime? ProcessingStartedAt { get; set; }

        public DateTime? ProcessingCompletedAt { get; set; }

        [Column(TypeName = "nvarchar(30)")]
        public ProcessingStatus Status { get; set; }

        [MaxLength(100)]
        public string? ErrorCode { get; set; }

        public string? ErrorMessage { get; set; }

        [Column(TypeName = "nvarchar(50)")]
        public ExtractionMethod ExtractionMethod { get; set; } = ExtractionMethod.PDF_TEXT;

        public bool FallbackUsed { get; set; }

        public long? ProcessingDurationMs { get; set; }

        [MaxLength(100)]
        public string? ErrorCategory { get; set; }

        [Column(TypeName = "nvarchar(40)")]
        public ExtractionTrigger Trigger { get; set; } = ExtractionTrigger.LEGACY;

        [MaxLength(100)]
        public string? OcrRequestId { get; set; }

        [MaxLength(128)]
        public string? ContentHash { get; set; }

        public string? ConfigurationSnapshotJson { get; set; }

        public string? StructuredPayloadJson { get; set; }
    }
}
