using Buyer.Domain.Dto;
using MediatR;

namespace Buyer.Application.Features.Queries.Invitation
{
    public class InvitationSummaryQuery
        : IRequest<SupplierInvitationSummaryDto>
    {
        public Guid OrganizationId { get; set; }
        public string OrganizationType { get; set; }
    }
}