using MediatR;
using Operations.Domain.Dtos;

namespace Operations.Application.Features.Queries.ExportIntegrationData
{
    /// <summary>
    /// Exports the purchase orders or suppliers of an integration's entity as an Excel file in the import layout.
    /// </summary>
    public class ExportIntegrationDataQuery : IRequest<FileDownloadDto>
    {
        public Guid OrganizationId { get; set; }
        public Guid UserId { get; set; }
        public Guid ConfigurationId { get; set; }
        public IntegrationDataUpdateRequestDto Request { get; set; } = new();
    }
}
