using MediatR;

namespace Buyer.Application.Features.Commands.RetryWeeklyBucketPurchaseOrders
{
    public class RetryWeeklyBucketPurchaseOrdersCommand : IRequest<Unit>
    {
        public Guid OrganizationId { get; set; }
        public Guid UserId { get; set; }
        public Guid WeeklyBucketId { get; set; }
    }
}
