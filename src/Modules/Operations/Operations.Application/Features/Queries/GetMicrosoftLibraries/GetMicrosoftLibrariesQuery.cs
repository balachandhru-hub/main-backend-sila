using MediatR;
using Operations.Domain.Dtos;

namespace Operations.Application.Features.Queries.GetMicrosoftLibraries
{
    /// <summary>
    /// Lists the document libraries of a SharePoint site.
    /// </summary>
    public class GetMicrosoftLibrariesQuery : IRequest<List<MicrosoftLibraryResponseDto>>
    {
        public Guid OrganizationId { get; set; }
        public Guid UserId { get; set; }
        public Guid ConnectionId { get; set; }
        public string SiteId { get; set; } = string.Empty;
    }
}
