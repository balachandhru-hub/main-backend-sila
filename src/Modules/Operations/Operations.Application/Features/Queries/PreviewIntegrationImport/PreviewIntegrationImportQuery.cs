using MediatR;
using Microsoft.AspNetCore.Http;
using Operations.Domain.Dtos;
using Operations.Domain.Enums;

namespace Operations.Application.Features.Queries.PreviewIntegrationImport
{
    /// <summary>
    /// Reads an .xlsx or .csv upload and returns its normalized rows with their validation errors. Nothing is written.
    /// </summary>
    public class PreviewIntegrationImportQuery : IRequest<IntegrationImportPreviewResponseDto>
    {
        public Guid OrganizationId { get; set; }
        public Guid UserId { get; set; }
        public Guid ConfigurationId { get; set; }
        public IntegrationImportKind Kind { get; set; } = IntegrationImportKind.PURCHASE_ORDERS;
        public IFormFile? File { get; set; }
    }
}
