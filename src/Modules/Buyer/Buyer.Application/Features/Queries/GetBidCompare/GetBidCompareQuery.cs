using Buyer.Domain.Dto;
using MediatR;

namespace Buyer.Application.Features.Queries.GetBidCompare
{
    public class GetBidCompareQuery : IRequest<BidCompareResponseDto>
    {
        public Guid RFQId { get; set; }
       
    }
}
