using Buyer.Domain.Dto;
using MediatR;

namespace Buyer.Application.Features.Queries.GetBuyerNameById
{
    public class GetBuyerNameByIdQuery : IRequest<BuyerNameDto>
    {
        public Guid BuyerId { get; }

        public GetBuyerNameByIdQuery(Guid buyerId)
        {
            BuyerId = buyerId;
        }
    }
}
