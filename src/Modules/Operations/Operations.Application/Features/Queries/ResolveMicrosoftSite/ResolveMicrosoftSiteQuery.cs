using MediatR;
using Operations.Domain.Dtos;

namespace Operations.Application.Features.Queries.ResolveMicrosoftSite
{
    /// <summary>
    /// Resolves a SharePoint site URL with the connection's Microsoft sign-in.
    /// </summary>
    public class ResolveMicrosoftSiteQuery : IRequest<MicrosoftSiteResponseDto>
    {
        public Guid OrganizationId { get; set; }
        public Guid UserId { get; set; }
        public Guid ConnectionId { get; set; }
        public MicrosoftSiteRequestDto Request { get; set; } = new();
    }
}
