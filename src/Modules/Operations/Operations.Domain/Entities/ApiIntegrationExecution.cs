using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Operations.Domain.Enums;
using SharedKernel.Models;

namespace Operations.Domain.Entities
{
    /// <summary>
    /// One run (test, pull or spreadsheet import) of an integration configuration.
    /// </summary>
    public class ApiIntegrationExecution : BaseModel
    {
        [Key]
        [Required]
        public Guid Id { get; set; }

        [Required]
        [ForeignKey("Configuration")]
        public Guid ConfigurationId { get; set; }

        public ApiIntegrationConfiguration Configuration { get; set; } = null!;

        [Column(TypeName = "nvarchar(20)")]
        public IntegrationExecutionTrigger Trigger { get; set; }

        [Column(TypeName = "nvarchar(20)")]
        public IntegrationExecutionStatus Status { get; set; }

        public DateTime StartedAt { get; set; }

        public DateTime? CompletedAt { get; set; }

        public int RecordsRead { get; set; }

        public int RecordsCreated { get; set; }

        public int RecordsUpdated { get; set; }

        public int RecordsFailed { get; set; }

        public DateTime? WatermarkBefore { get; set; }

        public DateTime? WatermarkAfter { get; set; }

        [MaxLength(100)]
        public string? ErrorCode { get; set; }

        [MaxLength(2000)]
        public string? ErrorMessageSafe { get; set; }

        public string? DetailJson { get; set; }
    }
}
