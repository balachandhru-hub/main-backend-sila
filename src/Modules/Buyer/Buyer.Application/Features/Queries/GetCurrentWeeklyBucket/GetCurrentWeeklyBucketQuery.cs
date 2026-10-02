using Buyer.Domain.Dtos;
using MediatR;

namespace Buyer.Application.Features.Queries.GetCurrentWeeklyBucket
{
    public class GetCurrentWeeklyBucketQuery : IRequest<WeeklyBucketDetailDto>
    {
        public Guid OrganizationId { get; set; }
        public Guid UserId { get; set; }
        public Guid? OutletId { get; set; }
    }
}
