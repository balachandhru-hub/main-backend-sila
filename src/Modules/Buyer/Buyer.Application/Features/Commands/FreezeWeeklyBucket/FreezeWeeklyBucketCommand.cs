using MediatR;

namespace Buyer.Application.Features.Commands.FreezeWeeklyBucket
{
    public class FreezeWeeklyBucketCommand : IRequest<Unit>
    {
        public Guid OrganizationId { get; set; }
        public Guid UserId { get; set; }
        public Guid WeeklyBucketId { get; set; }
    }
}
