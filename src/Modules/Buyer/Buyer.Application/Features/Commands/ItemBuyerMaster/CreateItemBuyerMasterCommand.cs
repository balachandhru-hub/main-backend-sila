using Buyer.Domain.Dtos;
using MediatR;

namespace Buyer.Application.Features.Commands.ItemBuyerMaster
{
    public record CreateItemBuyerMasterCommand(
        CreateItemBuyerMasterDto PredefinedMaterial,
        Guid OrganizationId
    ) : IRequest<Guid>;
}