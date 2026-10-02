using Buyer.Domain.Dtos;
using MediatR;

namespace Buyer.Application.Features.Commands.DecideWeeklyBucketRecommendation
{
    public class DecideWeeklyBucketRecommendationCommand : IRequest<Unit>
    {
        public Guid OrganizationId { get; set; }
        public Guid UserId { get; set; }
        public Guid WeeklyBucketId { get; set; }
        public Guid RecommendationId { get; set; }
        public WeeklyBucketRecommendationDecisionDto Decision { get; set; } = new();
    }
}
