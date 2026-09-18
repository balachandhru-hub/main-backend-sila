using Buyer.Domain.Common;
using Buyer.Domain.Entities;
using Buyer.Infrastructure.Contracts.IRepository;
using Buyer.Infrastructure.DbContext;
using Microsoft.EntityFrameworkCore;
using System.Runtime.CompilerServices;

namespace Buyer.Infrastructure.Repository
{
    public class ContractRepository
        : RepositoryBase<Contract>, IContractRepository
    {
        public ContractRepository(RepositoryContext repositoryContext)
            : base(repositoryContext)
        {
        }

        public async Task<long> GetNextContractNumberAsync(
            CancellationToken cancellationToken = default)
        {
            string schema = RepositoryContext.Model.GetDefaultSchema() ?? "dbo";

            var sql = FormattableStringFactory.Create(
                $"SELECT NEXT VALUE FOR [{schema}].[{Common.CONTRACT_NUMBER_SEQUENCE}] AS Value");

            return await RepositoryContext.Database
                .SqlQuery<long>(sql)
                .FirstAsync(cancellationToken);
        }

        public async Task<bool> CreateApprovalFlowForContractAsync(
            Guid contractId,
            Guid buyerId,
            decimal amount,
            CancellationToken cancellationToken = default)
        {
            var matchedApprovalFlow = await RepositoryContext.Set<MasterApprovalFlow>()
                .Where(x =>
                    x.BuyerId == buyerId &&
                    x.IsActive &&
                    amount >= x.TotalAmount)
                .OrderByDescending(x => x.TotalAmount)
                .FirstOrDefaultAsync(cancellationToken);

            if (matchedApprovalFlow == null)
            {
                return false;
            }

            var contractApprovalFlow = new ContractApprovalFlow
            {
                Id = Guid.NewGuid(),
                ApprovalCode = matchedApprovalFlow.ApprovalCode,
                ApprovalName = matchedApprovalFlow.ApprovalName,
                ContractId = contractId,
                Type = matchedApprovalFlow.Type,
                TotalAmount = matchedApprovalFlow.TotalAmount,
                Currency = matchedApprovalFlow.Currency
            };

            await RepositoryContext.Set<ContractApprovalFlow>()
                .AddAsync(contractApprovalFlow, cancellationToken);

            var approvalFlowUsers = await RepositoryContext.Set<ApprovalFlowUserMapping>()
                .Where(x =>
                    x.ApprovalFlowId == matchedApprovalFlow.Id &&
                    x.IsActive)
                .ToListAsync(cancellationToken);

            if (approvalFlowUsers.Any())
            {
                var contractApprovalUserMappings = approvalFlowUsers
                    .Select(user => new ContractApprovalUserMapping
                    {
                        Id = Guid.NewGuid(),
                        ContractApprovalFlowId = contractApprovalFlow.Id,
                        UserId = user.UserId,
                        Order = user.Order
                    })
                    .ToList();

                await RepositoryContext.Set<ContractApprovalUserMapping>()
                    .AddRangeAsync(contractApprovalUserMappings, cancellationToken);
            }

            return true;
        }
    }
}
