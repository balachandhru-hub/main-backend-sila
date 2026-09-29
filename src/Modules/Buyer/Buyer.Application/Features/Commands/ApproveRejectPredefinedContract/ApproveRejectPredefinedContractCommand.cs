using Buyer.Domain.Dtos;
using MediatR;

namespace Buyer.Application.Features.Commands.ApproveRejectPredefinedContract
{
    public class ApproveRejectPredefinedContractCommand : IRequest<Guid>
    {
        public Guid ContractId { get; }
        public Guid UserId { get; }
        public ApproveRejectPredefinedContractDto Approval { get; }

        public ApproveRejectPredefinedContractCommand(
            Guid contractId,
            Guid userId,
            ApproveRejectPredefinedContractDto approval)
        {
            ContractId = contractId;
            UserId = userId;
            Approval = approval;
        }
    }
}
