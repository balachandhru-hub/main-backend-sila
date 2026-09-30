using Buyer.Domain.Dtos;
using MediatR;

namespace Buyer.Application.Features.Commands.MasterApprovalFlows
{
    public record CreateMasterApprovalFlowCommand(
        CreateMasterApprovalFlowDto ApprovalFlow,
        Guid OrganizationId
    ) : IRequest<Guid>;
}   