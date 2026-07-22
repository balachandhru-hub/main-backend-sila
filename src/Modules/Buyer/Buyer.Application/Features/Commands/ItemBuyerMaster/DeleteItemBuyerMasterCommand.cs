using MediatR;

namespace Buyer.Application.Features.Commands.ItemBuyerMaster
{
    public record DeleteItemBuyerMasterCommand(Guid Id) : IRequest<bool>;
}