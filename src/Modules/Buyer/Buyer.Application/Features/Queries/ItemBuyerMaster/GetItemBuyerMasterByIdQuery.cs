using Buyer.Domain.Dtos;
using MediatR;

namespace Buyer.Application.Features.Queries.ItemBuyerMaster
{
    public class GetItemBuyerMasterByIdQuery : IRequest<ItemBuyerMasterDetailDto>
    {
        public Guid Id { get; }

        public GetItemBuyerMasterByIdQuery(Guid id)
        {
            Id = id;
        }
    }
}
