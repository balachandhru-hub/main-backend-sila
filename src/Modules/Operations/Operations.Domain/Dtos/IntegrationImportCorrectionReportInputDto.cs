using Operations.Domain.Enums;

namespace Operations.Domain.Dtos
{
    public class IntegrationImportCorrectionReportInputDto
    {
        public IntegrationImportKind Kind { get; set; }
        public List<IntegrationImportRowResponseDto> Rows { get; set; } = new();
    }
}
