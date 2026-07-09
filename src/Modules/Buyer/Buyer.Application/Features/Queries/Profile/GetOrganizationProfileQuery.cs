using MediatR;
using Buyer.Domain.Dto;

namespace Buyer.Application.Features.Queries.GetOrganizationProfile
{
    public class GetOrganizationProfileQuery : IRequest<OrganizationDto>
    {
        public Guid OrganizationId { get; set; }

        public GetOrganizationProfileQuery(Guid organizationId)
        {
            OrganizationId = organizationId;
        }
    }
}