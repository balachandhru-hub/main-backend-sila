using Buyer.Domain.Dtos;
using MediatR;

namespace Buyer.Application.Features.Queries.GetWeeklyBuckets
{
    public class GetWeeklyBucketsQuery : IRequest<List<WeeklyBucketListItemDto>>
    {
        public Guid OrganizationId { get; set; }
        public int Index { get; set; }
        public int Limit { get; set; }
    }
}
