using Buyer.Domain.Dtos;
using MediatR;

namespace Buyer.Application.Features.Commands.ApproveRejectMaster
{
    public record ApproveRejectMasterCommand(
        Guid PredefinedMaterialId,
        Guid UserId,
        ApproveRejectItemMasterDto Approval
    ) : IRequest<Guid>;
}