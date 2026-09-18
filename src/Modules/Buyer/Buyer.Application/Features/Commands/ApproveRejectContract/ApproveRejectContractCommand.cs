using Buyer.Domain.Dtos;
using MediatR;

namespace Buyer.Application.Features.Commands.ApproveRejectContract
{
    public class ApproveRejectContractCommand : IRequest<Guid>
    {
        public Guid ContractId { get; }
        public Guid UserId { get; }
        public ApproveRejectContractDto Approval { get; }

        public ApproveRejectContractCommand(
            Guid contractId,
            Guid userId,
            ApproveRejectContractDto approval)
        {
            ContractId = contractId;
            UserId = userId;
            Approval = approval;
        }
    }
}
