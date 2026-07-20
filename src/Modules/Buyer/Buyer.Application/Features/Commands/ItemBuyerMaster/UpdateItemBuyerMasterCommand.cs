using Buyer.Domain.Dtos;
using MediatR;

namespace Buyer.Application.Features.Commands.ItemBuyerMaster
{
    public record UpdateItemBuyerMasterCommand(
        UpdateItemBuyerMasterDto ItemBuyerMaster,
        Guid OrganizationId
    ) : IRequest<bool>;
}