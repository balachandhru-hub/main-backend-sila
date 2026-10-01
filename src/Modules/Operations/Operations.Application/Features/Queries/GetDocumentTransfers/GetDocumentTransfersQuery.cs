using MediatR;
using Operations.Domain.Dtos;

namespace Operations.Application.Features.Queries.GetDocumentTransfers
{
    /// <summary>
    /// Lists the external transfers (SharePoint) of a document.
    /// </summary>
    public class GetDocumentTransfersQuery : IRequest<DocumentTransfersResponseDto>
    {
        public Guid OrganizationId { get; set; }
        public Guid UserId { get; set; }
        public Guid DocumentId { get; set; }
    }
}
