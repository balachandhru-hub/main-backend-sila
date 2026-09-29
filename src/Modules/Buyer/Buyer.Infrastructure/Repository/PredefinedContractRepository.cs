using Buyer.Domain.Common;
using Buyer.Domain.Entities;
using Buyer.Infrastructure.Contracts.IRepository;
using Buyer.Infrastructure.DbContext;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using System.Data;

namespace Buyer.Infrastructure.Repository
{
    public class PredefinedContractRepository
        : RepositoryBase<PredefinedContract>, IPredefinedContractRepository
    {
        public PredefinedContractRepository(RepositoryContext repositoryContext)
            : base(repositoryContext)
        {
        }

        public async Task<long> GetNextContractNumberAsync(
            CancellationToken cancellationToken = default)
        {
            string schema = RepositoryContext.Model.GetDefaultSchema() ?? "dbo";

            // NEXT VALUE FOR is not allowed inside a sub-query, and SqlQuery<T>().FirstAsync()
            // wraps the SQL in one, so run it as a plain scalar command instead.
            var connection = RepositoryContext.Database.GetDbConnection();
            bool wasClosed = connection.State != ConnectionState.Open;

            if (wasClosed)
            {
                await RepositoryContext.Database.OpenConnectionAsync(cancellationToken);
            }

            try
            {
                using var command = connection.CreateCommand();
                command.CommandText =
                    $"SELECT NEXT VALUE FOR [{schema}].[{Common.PREDEFINED_CONTRACT_NUMBER_SEQUENCE}]";
                command.Transaction = RepositoryContext.Database.CurrentTransaction?.GetDbTransaction();

                object? value = await command.ExecuteScalarAsync(cancellationToken);

                return Convert.ToInt64(value);
            }
            finally
            {
                if (wasClosed)
                {
                    await RepositoryContext.Database.CloseConnectionAsync();
                }
            }
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
                    amount <= x.TotalAmount)
                .OrderByDescending(x => x.TotalAmount)
                .FirstOrDefaultAsync(cancellationToken);

            if (matchedApprovalFlow == null)
            {
                return false;
            }

            var contractApprovalFlow = new PredefinedContractApprovalFlow
            {
                Id = Guid.NewGuid(),
                ApprovalCode = matchedApprovalFlow.ApprovalCode,
                ApprovalName = matchedApprovalFlow.ApprovalName,
                ContractId = contractId,
                Type = matchedApprovalFlow.Type,
                TotalAmount = matchedApprovalFlow.TotalAmount,
                Currency = matchedApprovalFlow.Currency
            };

            await RepositoryContext.Set<PredefinedContractApprovalFlow>()
                .AddAsync(contractApprovalFlow, cancellationToken);

            var approvalFlowUsers = await RepositoryContext.Set<ApprovalFlowUserMapping>()
                .Where(x =>
                    x.ApprovalFlowId == matchedApprovalFlow.Id &&
                    x.IsActive)
                .ToListAsync(cancellationToken);

            if (approvalFlowUsers.Any())
            {
                var contractApprovalUserMappings = approvalFlowUsers
                    .Select(user => new PredefinedContractApprovalUserMapping
                    {
                        Id = Guid.NewGuid(),
                        ContractApprovalFlowId = contractApprovalFlow.Id,
                        UserId = user.UserId,
                        Order = user.Order,
                        // Both columns are NOT NULL; a new approver starts as pending.
                        Status = Common.PENDING,
                        Comment = string.Empty
                    })
                    .ToList();

                await RepositoryContext.Set<PredefinedContractApprovalUserMapping>()
                    .AddRangeAsync(contractApprovalUserMappings, cancellationToken);
            }

            return true;
        }
    }
}
