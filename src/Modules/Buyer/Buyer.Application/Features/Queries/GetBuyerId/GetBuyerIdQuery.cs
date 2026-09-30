using MediatR;

namespace Buyer.Application.Features.Profile.Queries.GetBuyerId
{
    public class GetBuyerIdQuery : IRequest<Guid>
    {
        public Guid OrganizationId { get; }

        public GetBuyerIdQuery(Guid organizationId)
        {
            OrganizationId = organizationId;
        }
    }
}