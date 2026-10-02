using Buyer.Domain.Dtos;
using MediatR;

namespace Buyer.Application.Features.Commands.UpdateWeeklyBucketItemQuantity
{
    public class UpdateWeeklyBucketItemQuantityCommand : IRequest<Unit>
    {
        public Guid OrganizationId { get; set; }
        public Guid UserId { get; set; }
        public Guid WeeklyBucketId { get; set; }
        public Guid ItemId { get; set; }
        public QuantityWriteDto Request { get; set; } = new();
    }
}
