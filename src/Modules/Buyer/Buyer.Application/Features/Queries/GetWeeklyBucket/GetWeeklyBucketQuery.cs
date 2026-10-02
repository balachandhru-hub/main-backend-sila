using Buyer.Domain.Dtos;
using MediatR;

namespace Buyer.Application.Features.Queries.GetWeeklyBucket
{
    public class GetWeeklyBucketQuery : IRequest<WeeklyBucketDetailDto>
    {
        public Guid OrganizationId { get; set; }
        public Guid WeeklyBucketId { get; set; }
    }
}
