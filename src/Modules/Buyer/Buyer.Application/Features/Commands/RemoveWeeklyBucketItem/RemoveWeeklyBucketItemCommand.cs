using MediatR;

namespace Buyer.Application.Features.Commands.RemoveWeeklyBucketItem
{
    public class RemoveWeeklyBucketItemCommand : IRequest<Unit>
    {
        public Guid OrganizationId { get; set; }
        public Guid UserId { get; set; }
        public Guid RoleId { get; set; }
        public Guid WeeklyBucketId { get; set; }
        public Guid ItemId { get; set; }
    }
}
