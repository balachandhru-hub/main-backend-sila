using Buyer.Domain.Dtos;
using MediatR;

namespace Buyer.Application.Features.Commands.AddWeeklyBucketItems
{
    public class AddWeeklyBucketItemsCommand : IRequest<WeeklyBucketAddItemsResultDto>
    {
        public Guid OrganizationId { get; set; }
        public Guid UserId { get; set; }
        public WeeklyBucketItemsWriteDto Request { get; set; } = new();
    }
}
