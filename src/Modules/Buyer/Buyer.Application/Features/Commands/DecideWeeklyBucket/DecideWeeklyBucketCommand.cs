using Buyer.Domain.Dtos;
using MediatR;

namespace Buyer.Application.Features.Commands.DecideWeeklyBucket
{
    public class DecideWeeklyBucketCommand : IRequest<Unit>
    {
        public Guid OrganizationId { get; set; }
        public Guid UserId { get; set; }
        public Guid WeeklyBucketId { get; set; }
        public WeeklyBucketDecisionDto Decision { get; set; } = new();
    }
}
