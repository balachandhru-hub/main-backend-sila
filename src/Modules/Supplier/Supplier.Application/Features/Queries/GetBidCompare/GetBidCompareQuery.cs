using MediatR;
using Supplier.Domain.Dto;

namespace Supplier.Application.Features.Queries.GetBidCompare
{
    public class GetBidCompareQuery : IRequest<BidCompareResponseDto>
    {
        public Guid RFQId { get; }

        public GetBidCompareQuery(Guid rfqId)
        {
            RFQId = rfqId;
        }
    }
}
