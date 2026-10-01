using Operations.Domain.Enums;

namespace Operations.Domain.Dtos
{
    public class IntegrationExecutionResponseDto
    {
        public Guid Id { get; set; }
        public Guid ConfigurationId { get; set; }
        public IntegrationExecutionTrigger Trigger { get; set; }
        public IntegrationExecutionStatus Status { get; set; }
        public DateTime StartedAt { get; set; }
        public DateTime? CompletedAt { get; set; }
        public int RecordsRead { get; set; }
        public int RecordsCreated { get; set; }
        public int RecordsUpdated { get; set; }
        public int RecordsFailed { get; set; }
        public DateTime? WatermarkBefore { get; set; }
        public DateTime? WatermarkAfter { get; set; }
        public string? ErrorCode { get; set; }
        public string? ErrorMessageSafe { get; set; }
    }
}
