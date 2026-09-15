using Buyer.Domain.Dtos;
using MediatR;

namespace Buyer.Application.Features.Commands.MasterApprovalFlows
{
    public record UpdateMasterApprovalFlowByUserMappingCommand(
        Guid ApprovalFlowUserMappingId,
        UpdateMasterApprovalFlowDto ApprovalFlow
    ) : IRequest<Guid>;
}
