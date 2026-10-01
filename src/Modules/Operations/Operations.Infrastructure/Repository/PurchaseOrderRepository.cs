using Microsoft.EntityFrameworkCore;
using Operations.Domain.Dtos;
using Operations.Domain.Enums;
using Operations.Domain.Entities;
using Operations.Infrastructure.Contracts.IRepository;
using Operations.Infrastructure.DbContext;

namespace Operations.Infrastructure.Repository
{
    public class PurchaseOrderRepository : RepositoryBase<PurchaseOrder>, IPurchaseOrderRepository
    {
        public PurchaseOrderRepository(RepositoryContext repositoryContext) : base(repositoryContext)
        {
        }

        public Task<List<PurchaseOrder>> SearchAsync(Guid organizationId, PurchaseOrderSearchRequestDto request, CancellationToken cancellationToken)
        {
            IQueryable<PurchaseOrder> orders = RepositoryContext.PurchaseOrder
                .AsNoTracking()
                .Where(x => x.OrganizationId == organizationId);
            if (!string.IsNullOrWhiteSpace(request.EntityCode))
            {
                orders = orders.Where(x => x.EntityCode == request.EntityCode);
            }

            if (!string.IsNullOrWhiteSpace(request.Query))
            {
                string term = request.Query.Trim();
                orders = orders.Where(x => x.PoNumber.Contains(term)
                    || (x.SupplierName != null && x.SupplierName.Contains(term))
                    || RepositoryContext.SupplierMaster.Any(supplier => supplier.Id == x.SupplierId && supplier.Name.Contains(term)));
            }

            if (request.OperatingUnitId != null)
            {
                orders = orders.Where(x => x.OperatingUnitId == request.OperatingUnitId);
            }

            if (request.SupplierId != null)
            {
                orders = orders.Where(x => x.SupplierId == request.SupplierId);
            }

            if (request.OpenOnly)
            {
                // Open for receiving: an open order of an active supplier with at least one receivable item.
                orders = orders.Where(x => (x.Status == PurchaseOrderStatus.OPEN || x.Status == PurchaseOrderStatus.PARTIALLY_RECEIVED)
                    && RepositoryContext.SupplierMaster.Any(supplier => supplier.Id == x.SupplierId
                        && supplier.Status == StatusKind.ACTIVE && !supplier.IsBlocked && !supplier.IsDeleted)
                    && RepositoryContext.PurchaseOrderItem.Any(item => item.PurchaseOrderId == x.Id
                        && item.OpenQuantity > 0
                        && item.Status != PurchaseOrderItemStatus.CLOSED
                        && item.Status != PurchaseOrderItemStatus.CANCELLED
                        && !item.DeletionIndicator
                        && item.GoodsReceiptExpected
                        && !item.DeliveryCompleted));
            }

            return orders.OrderByDescending(x => x.PoDate).Take(50).ToListAsync(cancellationToken);
        }

        // The purchase order an invoice is matched to: by number when the invoice names one,
        // otherwise the oldest open order of the supplier for the same operating unit.
        public async Task<Guid?> FindOpenIdAsync(Guid organizationId, Guid supplierId, string? poNumber, Guid? operatingUnitId, CancellationToken cancellationToken)
        {
            IQueryable<PurchaseOrder> orders = RepositoryContext.PurchaseOrder
                .AsNoTracking()
                .Where(x => x.OrganizationId == organizationId && x.SupplierId == supplierId
                    && (x.Status == PurchaseOrderStatus.OPEN || x.Status == PurchaseOrderStatus.PARTIALLY_RECEIVED)
                    && RepositoryContext.PurchaseOrderItem.Any(item => item.PurchaseOrderId == x.Id
                        && item.OpenQuantity > 0 && item.Status != PurchaseOrderItemStatus.CANCELLED));

            Guid? purchaseOrderId = null;
            if (!string.IsNullOrWhiteSpace(poNumber))
            {
                purchaseOrderId = await orders
                    .Where(x => x.PoNumber == poNumber)
                    .Select(x => (Guid?)x.Id)
                    .FirstOrDefaultAsync(cancellationToken);
            }

            if (purchaseOrderId == null && operatingUnitId != null)
            {
                purchaseOrderId = await orders
                    .Where(x => x.OperatingUnitId == operatingUnitId && x.Status == PurchaseOrderStatus.OPEN)
                    .OrderBy(x => x.PoDate)
                    .Select(x => (Guid?)x.Id)
                    .FirstOrDefaultAsync(cancellationToken);
            }

            return purchaseOrderId;
        }

        public Task<int> CountIntegrationRowsAsync(Guid organizationId, Guid configurationId, IntegrationDataUpdateRequestDto request, CancellationToken cancellationToken)
        {
            return IntegrationRows(organizationId, configurationId, request).CountAsync(cancellationToken);
        }

        public Task<List<IntegrationPurchaseOrderRowDto>> ListIntegrationRowsAsync(Guid organizationId, Guid configurationId, IntegrationDataUpdateRequestDto request, CancellationToken cancellationToken)
        {
            IQueryable<IntegrationPurchaseOrderRowDto> rows = IntegrationRows(organizationId, configurationId, request);
            bool descending = request.Descending;
            rows = (request.SortBy ?? string.Empty).ToLowerInvariant() switch
            {
                "ponumber" => descending ? rows.OrderByDescending(x => x.PoNumber) : rows.OrderBy(x => x.PoNumber),
                "supplier" => descending ? rows.OrderByDescending(x => x.SupplierName) : rows.OrderBy(x => x.SupplierName),
                "status" => descending ? rows.OrderByDescending(x => x.Status) : rows.OrderBy(x => x.Status),
                "currency" => descending ? rows.OrderByDescending(x => x.Currency) : rows.OrderBy(x => x.Currency),
                "podate" => descending ? rows.OrderByDescending(x => x.PoDate) : rows.OrderBy(x => x.PoDate),
                _ => descending ? rows.OrderByDescending(x => x.LastSyncedAt) : rows.OrderBy(x => x.LastSyncedAt),
            };
            int page = Math.Max(1, request.Page);
            int pageSize = Math.Clamp(request.PageSize, 10, 100);
            return rows.Skip((page - 1) * pageSize).Take(pageSize).ToListAsync(cancellationToken);
        }

        private IQueryable<IntegrationPurchaseOrderRowDto> IntegrationRows(Guid organizationId, Guid configurationId, IntegrationDataUpdateRequestDto request)
        {
            IQueryable<IntegrationPurchaseOrderRowDto> rows =
                from order in RepositoryContext.PurchaseOrder.AsNoTracking()
                join supplier in RepositoryContext.SupplierMaster.AsNoTracking() on order.SupplierId equals supplier.Id
                where order.OrganizationId == organizationId && order.SourceConfigurationId == configurationId
                select new IntegrationPurchaseOrderRowDto
                {
                    Id = order.Id,
                    PoNumber = order.PoNumber,
                    SupplierName = supplier.Name,
                    Status = order.Status,
                    Currency = order.Currency,
                    PoDate = order.PoDate,
                    LastSyncedAt = order.LastSyncedAt
                };
            if (!string.IsNullOrWhiteSpace(request.Search))
            {
                string term = request.Search.Trim();
                rows = rows.Where(x => x.PoNumber.Contains(term) || x.SupplierName.Contains(term) || x.Currency.Contains(term));
            }

            if (!string.IsNullOrWhiteSpace(request.Status) && Enum.TryParse(request.Status, true, out PurchaseOrderStatus status))
            {
                rows = rows.Where(x => x.Status == status);
            }

            return rows;
        }
    }
}
