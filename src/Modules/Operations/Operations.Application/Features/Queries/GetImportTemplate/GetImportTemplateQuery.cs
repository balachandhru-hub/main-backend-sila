using MediatR;
using Operations.Domain.Dtos;
using Operations.Domain.Enums;

namespace Operations.Application.Features.Queries.GetImportTemplate
{
    /// <summary>
    /// Returns the empty Excel template of the purchase order or supplier import.
    /// </summary>
    public class GetImportTemplateQuery : IRequest<FileDownloadDto>
    {
        public Guid OrganizationId { get; set; }
        public Guid UserId { get; set; }
        public IntegrationImportKind Kind { get; set; } = IntegrationImportKind.PURCHASE_ORDERS;
    }
}
