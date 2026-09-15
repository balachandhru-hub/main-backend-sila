using Buyer.Domain.Dtos;
using MediatR;

namespace Buyer.Application.Features.Commands.PredefinedMaterialMaster
{
    public record CreatePredefinedMaterialCommand(
        CreateItemBuyerMasterDto PredefinedMaterial,
        Guid OrganizationId
    ) : IRequest<Guid>;
}