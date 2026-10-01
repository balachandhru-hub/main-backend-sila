namespace Operations.Domain.Dtos
{
    public class AuditEventResponseDto
    {
        public Guid Id { get; set; }
        public Guid? OperatingUnitId { get; set; }
        public Guid? UserId { get; set; }
        public string EventType { get; set; } = string.Empty;
        public string EntityType { get; set; } = string.Empty;
        public Guid? EntityId { get; set; }
        public string? Reference { get; set; }
        public string? Result { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}
