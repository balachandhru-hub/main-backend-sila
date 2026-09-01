using MediatR;

namespace Buyer.Application.Features.Queries.Invitation
{
    public class InvitationSummaryCountQuery : IRequest<int>
    {
        public Guid OrganizationId { get; set; }

        public string OrganizationType { get; set; }
    }
}