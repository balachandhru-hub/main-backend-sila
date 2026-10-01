using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.EntityFrameworkCore;
using Operations.Domain.Entities;
using Operations.Infrastructure.Contracts.IRepository;
using Operations.Infrastructure.DbContext;

namespace Operations.Infrastructure.Repository
{
    public class PurchaseOrderItemRepository : RepositoryBase<PurchaseOrderItem>, IPurchaseOrderItemRepository
    {
        public PurchaseOrderItemRepository(RepositoryContext repositoryContext) : base(repositoryContext)
        {
        }

        public Task<List<PurchaseOrderItem>> GetTrackedByOrderAsync(Guid purchaseOrderId, CancellationToken cancellationToken)
        {
            return RepositoryContext.PurchaseOrderItem
                .Where(x => x.PurchaseOrderId == purchaseOrderId)
                .OrderBy(x => x.LineNumber)
                .ToListAsync(cancellationToken);
        }

        // Takes an update lock on the items of one purchase order for the rest of the current
        // transaction, so two goods receipts cannot consume the same open quantity.
        // Only identifiers taken from the EF model are concatenated; the value is a parameter.
        public Task LockForOrderAsync(Guid purchaseOrderId, CancellationToken cancellationToken)
        {
            IEntityType entity = RepositoryContext.Model.FindEntityType(typeof(PurchaseOrderItem))!;
            StoreObjectIdentifier table = StoreObjectIdentifier.Table(entity.GetTableName()!, entity.GetSchema());
            ISqlGenerationHelper sql = RepositoryContext.GetService<ISqlGenerationHelper>();
            string tableName = sql.DelimitIdentifier(entity.GetTableName()!, entity.GetSchema());
            string idColumn = sql.DelimitIdentifier(entity.FindProperty(nameof(PurchaseOrderItem.Id))!.GetColumnName(table)!);
            string orderColumn = sql.DelimitIdentifier(entity.FindProperty(nameof(PurchaseOrderItem.PurchaseOrderId))!.GetColumnName(table)!);
            string statement = "SELECT " + idColumn + " FROM " + tableName + " WITH (UPDLOCK, ROWLOCK) WHERE " + orderColumn + " = {0}";
            return RepositoryContext.Database.ExecuteSqlRawAsync(statement, new object[] { purchaseOrderId }, cancellationToken);
        }
    }
}
