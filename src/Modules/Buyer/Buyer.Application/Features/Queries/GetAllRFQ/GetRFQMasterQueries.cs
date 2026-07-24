using Buyer.Domain.Dto;
using MediatR;

namespace Buyer.Application.Features.Queries.GetAllRFQ
{
    public class GetRFQListQuery : IRequest<List<RFQListDto>>
    {
        public Guid BuyerId { get; set; }

        public int Index { get; set; }

        public int Limit { get; set; }
    }
}