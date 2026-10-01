using MediatR;
using Operations.Domain.Dtos;

namespace Operations.Application.Features.Queries.GetDocument
{
    /// <summary>
    /// Returns one stored document of the organization.
    /// </summary>
    public class GetDocumentQuery : IRequest<DocumentResponseDto>
    {
        public Guid OrganizationId { get; set; }
        public Guid UserId { get; set; }
        public Guid DocumentId { get; set; }
    }
}
