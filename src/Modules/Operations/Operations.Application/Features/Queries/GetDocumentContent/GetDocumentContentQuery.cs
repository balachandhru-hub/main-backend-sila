using MediatR;
using Operations.Domain.Dtos;

namespace Operations.Application.Features.Queries.GetDocumentContent
{
    /// <summary>
    /// Returns the stored file of a document.
    /// </summary>
    public class GetDocumentContentQuery : IRequest<FileDownloadDto>
    {
        public Guid OrganizationId { get; set; }
        public Guid UserId { get; set; }
        public Guid DocumentId { get; set; }
    }
}
