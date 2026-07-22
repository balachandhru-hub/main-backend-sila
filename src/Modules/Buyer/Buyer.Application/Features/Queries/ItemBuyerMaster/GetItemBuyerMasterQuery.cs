using Buyer.Domain.Dtos;
using MediatR;

namespace Buyer.Application.Features.Queries.ItemBuyerMaster
{
    public class GetItemBuyerMasterQuery : IRequest<List<ItemBuyerMasterDto>>
    {
        public int Index { get; set; }

        public int Limit { get; set; }

        public Guid? BuyerId { get; set; }

        public string? SearchTerm { get; set; }
    }
}