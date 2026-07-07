using Identity.Domain.Dto;
using MediatR;

namespace Identity.Application.Features.Queries.Onboarding
{
    public class GetOnboardingQuery : IRequest<OnboardingResponse>
    {
        public Guid OrganizationId { get; set; }
    }
}