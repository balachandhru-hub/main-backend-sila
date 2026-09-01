using Buyer.Domain.Dto;
using MediatR;

namespace Buyer.Application.Features.Queries.Invitation
{
    public class BuyerInvitationQuery : IRequest<List<RFQListDto>>
    {
         public Guid OrganizationId { get; set; }
        public int Index { get; set; }
        public int Limit { get; set; }
        public string? Search { get; set; }
         public string? Status { get; set; }
    }
}