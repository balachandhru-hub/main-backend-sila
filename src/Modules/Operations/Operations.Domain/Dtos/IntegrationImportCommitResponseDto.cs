namespace Operations.Domain.Dtos
{
    public class IntegrationImportCommitResponseDto
    {
        public IntegrationExecutionResponseDto Execution { get; set; } = new();
        public int RecordsCommitted { get; set; }
    }
}
