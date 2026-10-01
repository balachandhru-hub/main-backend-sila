using Operations.Domain.Enums;

namespace Operations.Domain.Dtos
{
    public class IntegrationImportCommitInputDto
    {
        public IntegrationImportKind Kind { get; set; }
        public List<Dictionary<string, string?>> Rows { get; set; } = new();
    }
}
