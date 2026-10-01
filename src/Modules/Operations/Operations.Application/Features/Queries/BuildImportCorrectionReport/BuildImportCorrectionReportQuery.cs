using MediatR;
using Operations.Domain.Dtos;

namespace Operations.Application.Features.Queries.BuildImportCorrectionReport
{
    /// <summary>
    /// Returns the invalid rows of an import preview as a CSV file with their errors.
    /// </summary>
    public class BuildImportCorrectionReportQuery : IRequest<FileDownloadDto>
    {
        public Guid OrganizationId { get; set; }
        public Guid UserId { get; set; }
        public Guid ConfigurationId { get; set; }
        public IntegrationImportCorrectionReportInputDto Request { get; set; } = new();
    }
}
