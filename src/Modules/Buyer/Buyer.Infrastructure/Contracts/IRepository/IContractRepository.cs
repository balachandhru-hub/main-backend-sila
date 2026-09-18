using Buyer.Domain.Entities;

namespace Buyer.Infrastructure.Contracts.IRepository
{
    public interface IContractRepository : IRepositoryBase<Contract>
    {
        Task<long> GetNextContractNumberAsync(CancellationToken cancellationToken = default);

        /// <summary>
        /// Matches the contract amount against the buyer's master approval flow thresholds
        /// (contractAmount >= MasterApprovalFlow.TotalAmount) and, if a match is found, stores
        /// a ContractApprovalFlow (with its approval user mappings) copied from the matched flow.
        /// Returns true if a matching approval flow was found and staged.
        /// </summary>
        Task<bool> CreateApprovalFlowForContractAsync(
            Guid contractId,
            Guid buyerId,
            decimal amount,
            CancellationToken cancellationToken = default);
    }
}
