using Buyer.Domain.Dtos;
using MediatR;

namespace Buyer.Application.Features.Commands.RefreshWeeklyBucketInventory
{
    public class RefreshWeeklyBucketInventoryCommand : IRequest<WeeklyBucketDetailDto>
    {
        public Guid OrganizationId { get; set; }
        public Guid UserId { get; set; }
        public Guid WeeklyBucketId { get; set; }
    }
}
