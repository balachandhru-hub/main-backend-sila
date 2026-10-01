using MediatR;
using Operations.Domain.Dtos;

namespace Operations.Application.Features.Queries.GetDocumentStatus
{
    /// <summary>
    /// Returns the processing status of a document and of its invoice.
    /// </summary>
    public class GetDocumentStatusQuery : IRequest<DocumentStatusResponseDto>
    {
        public Guid OrganizationId { get; set; }
        public Guid UserId { get; set; }
        public Guid DocumentId { get; set; }
    }
}
