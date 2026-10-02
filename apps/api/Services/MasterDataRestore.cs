using Microsoft.EntityFrameworkCore;
using SilaMe.Api.Data;
using SilaMe.Api.Models;
using SilaMe.Api.Tenancy;

namespace SilaMe.Api.Services;

public static class MasterDataRestore
{
    public static async Task RestoreAsync(
        SilaMeDbContext db,
        OperationalDatabaseOptions? operational = null,
        CancellationToken cancellationToken = default)
    {
        await RepairInPlaceAsync(db, cancellationToken);
        await CopyIntoEmptyOrganizationsAsync(db, cancellationToken);
        await CopyFromOperationalIfEmptyAsync(db, operational, cancellationToken);
        await db.SaveChangesAsync(cancellationToken);
    }

    public static async Task RepairInPlaceAsync(SilaMeDbContext db, CancellationToken cancellationToken)
    {
        var suppliers = await db.Suppliers.Include(item => item.Aliases).Include(item => item.PurchaseOrders).ToListAsync(cancellationToken);
        foreach (var supplier in suppliers)
        {
            if (NeedsNameRestore(supplier.Name))
            {
                var restored = FirstRealName(
                    supplier.LegalName,
                    supplier.SearchName,
                    supplier.Aliases.Select(alias => alias.Alias),
                    supplier.PurchaseOrders.Select(po => po.SupplierName));
                supplier.Name = restored ?? InvoiceOcrFieldReader.NormalizeSupplierCandidate(supplier.Name) ?? supplier.SupplierCode;
                supplier.NormalizedName = SupplierSearch.Normalize(supplier.Name);
            }

            if (supplier.IsDeleted && supplier.PurchaseOrders.Count > 0)
                supplier.IsDeleted = false;
            if (!supplier.IsActive && supplier.Status == StatusKind.ACTIVE && !supplier.IsBlocked && !supplier.IsDeleted)
                supplier.IsActive = true;
            supplier.UpdatedAt = DateTime.UtcNow;
        }

        var purchaseOrders = await db.PurchaseOrders.Include(item => item.Supplier).ToListAsync(cancellationToken);
        foreach (var po in purchaseOrders)
        {
            if (NeedsNameRestore(po.SupplierName))
                po.SupplierName = FirstRealName(po.Supplier?.Name, po.SupplierName) ?? po.Supplier?.SupplierCode ?? po.ErpSupplierId;
            po.UpdatedAt = DateTime.UtcNow;
        }
    }

    public static async Task CopyIntoEmptyOrganizationsAsync(SilaMeDbContext db, CancellationToken cancellationToken)
    {
        var organizations = await db.Organizations.Where(item => item.Status == StatusKind.ACTIVE).ToListAsync(cancellationToken);
        if (organizations.Count == 0) return;

        var donorId = (await db.Suppliers.AsNoTracking()
            .Where(item => !item.IsDeleted)
            .Select(item => new { item.OrganizationId, item.Name })
            .ToListAsync(cancellationToken))
            .Where(item => !NeedsNameRestore(item.Name))
            .GroupBy(item => item.OrganizationId)
            .OrderByDescending(group => group.Count())
            .Select(group => (Guid?)group.Key)
            .FirstOrDefault();
        if (donorId is null) return;

        foreach (var organization in organizations.Where(item => item.Id != donorId))
        {
            if (await db.Suppliers.AnyAsync(item => item.OrganizationId == organization.Id && !item.IsDeleted, cancellationToken) &&
                await db.PurchaseOrders.AnyAsync(item => item.OrganizationId == organization.Id, cancellationToken))
                continue;
            await CopyOrganizationMasterAsync(db, donorId.Value, organization, cancellationToken);
        }
    }

    private static async Task CopyFromOperationalIfEmptyAsync(
        SilaMeDbContext db,
        OperationalDatabaseOptions? operational,
        CancellationToken cancellationToken)
    {
        if (operational is null) return;
        var existingNames = await db.Suppliers.AsNoTracking()
            .Where(item => !item.IsDeleted)
            .Select(item => item.Name)
            .ToListAsync(cancellationToken);
        if (existingNames.Any(name => !NeedsNameRestore(name)) && await db.PurchaseOrders.AnyAsync(cancellationToken))
            return;

        var current = db.Database.GetConnectionString();
        var operationalConnection = DatabaseUrl.Normalize(operational.ConnectionString);
        if (string.IsNullOrWhiteSpace(current) || SameDatabase(current, operationalConnection))
            return;

        var options = new DbContextOptionsBuilder<SilaMeDbContext>().UseNpgsql(operationalConnection).Options;
        await using var source = new SilaMeDbContext(options);
        var donorId = (await source.Suppliers.AsNoTracking()
            .Where(item => !item.IsDeleted)
            .Select(item => new { item.OrganizationId, item.Name })
            .ToListAsync(cancellationToken))
            .Where(item => !NeedsNameRestore(item.Name))
            .GroupBy(item => item.OrganizationId)
            .OrderByDescending(group => group.Count())
            .Select(group => (Guid?)group.Key)
            .FirstOrDefault();
        if (donorId is null) return;

        var target = await db.Organizations.Where(item => item.Status == StatusKind.ACTIVE).OrderBy(item => item.CreatedAt).FirstOrDefaultAsync(cancellationToken);
        if (target is null) return;
        await CopyOrganizationMasterAsync(source, donorId.Value, target, cancellationToken, db);
    }

    private static async Task CopyOrganizationMasterAsync(
        SilaMeDbContext source,
        Guid sourceOrganizationId,
        Organization targetOrganization,
        CancellationToken cancellationToken,
        SilaMeDbContext? destination = null)
    {
        var db = destination ?? source;
        var now = DateTime.UtcNow;
        var unit = await db.OrganizationUnits.FirstOrDefaultAsync(item => item.OrganizationId == targetOrganization.Id && item.Status == StatusKind.ACTIVE, cancellationToken);
        if (unit is null)
        {
            unit = new OrganizationUnit
            {
                Id = Guid.NewGuid(),
                OrganizationId = targetOrganization.Id,
                Code = "MAIN",
                Name = "Main store",
                Kind = OrganizationUnitKind.STORE,
                CreatedAt = now,
                UpdatedAt = now,
            };
            db.OrganizationUnits.Add(unit);
        }

        var sourceSuppliers = await source.Suppliers.AsNoTracking().Include(item => item.Aliases)
            .Where(item => item.OrganizationId == sourceOrganizationId && !item.IsDeleted)
            .ToListAsync(cancellationToken);
        var supplierMap = new Dictionary<Guid, Guid>();
        foreach (var sourceSupplier in sourceSuppliers)
        {
            var existing = await db.Suppliers.Include(item => item.Aliases).FirstOrDefaultAsync(item =>
                item.OrganizationId == targetOrganization.Id && item.SupplierCode == sourceSupplier.SupplierCode, cancellationToken);
            if (existing is null)
            {
                existing = new Supplier
                {
                    Id = Guid.NewGuid(),
                    OrganizationId = targetOrganization.Id,
                    CreatedAt = now,
                    SourceSystem = sourceSupplier.SourceSystem ?? "MASTER_RESTORE",
                    Status = StatusKind.ACTIVE,
                    IsActive = true,
                    SupplierCode = sourceSupplier.SupplierCode,
                    Name = FirstRealName(sourceSupplier.Name, sourceSupplier.LegalName, sourceSupplier.SearchName) ?? sourceSupplier.SupplierCode,
                    NormalizedName = SupplierSearch.Normalize(FirstRealName(sourceSupplier.Name, sourceSupplier.LegalName, sourceSupplier.SearchName) ?? sourceSupplier.SupplierCode),
                };
                db.Suppliers.Add(existing);
            }
            else if (NeedsNameRestore(existing.Name))
            {
                existing.Name = FirstRealName(sourceSupplier.Name, sourceSupplier.LegalName, existing.LegalName) ?? existing.SupplierCode;
                existing.NormalizedName = SupplierSearch.Normalize(existing.Name);
                existing.IsDeleted = false;
                existing.IsActive = true;
                existing.Status = StatusKind.ACTIVE;
            }
            existing.EntityCode = string.IsNullOrWhiteSpace(existing.EntityCode) ? sourceSupplier.EntityCode : existing.EntityCode;
            existing.LegalName = existing.LegalName ?? sourceSupplier.LegalName;
            existing.SearchName = existing.SearchName ?? sourceSupplier.SearchName;
            existing.TaxNumber = existing.TaxNumber ?? sourceSupplier.TaxNumber;
            existing.Trn = existing.Trn ?? sourceSupplier.Trn;
            existing.Currency = existing.Currency ?? sourceSupplier.Currency;
            existing.CompanyCode = existing.CompanyCode ?? sourceSupplier.CompanyCode;
            existing.UpdatedAt = now;
            supplierMap[sourceSupplier.Id] = existing.Id;
            foreach (var alias in sourceSupplier.Aliases.Where(item => !NeedsNameRestore(item.Alias)))
            {
                if (existing.Aliases.Any(item => item.NormalizedAlias == alias.NormalizedAlias)) continue;
                existing.Aliases.Add(new SupplierAlias
                {
                    Id = Guid.NewGuid(),
                    OrganizationId = targetOrganization.Id,
                    SupplierId = existing.Id,
                    EntityCode = existing.EntityCode,
                    Alias = alias.Alias,
                    NormalizedAlias = alias.NormalizedAlias,
                    SourceSystem = "MASTER_RESTORE",
                    IsConfirmed = true,
                    CreatedAt = now,
                });
            }
        }

        var sourcePos = await source.PurchaseOrders.AsNoTracking().Include(item => item.Items)
            .Where(item => item.OrganizationId == sourceOrganizationId)
            .ToListAsync(cancellationToken);
        foreach (var sourcePo in sourcePos)
        {
            if (!supplierMap.TryGetValue(sourcePo.SupplierId, out var supplierId)) continue;
            if (await db.PurchaseOrders.AnyAsync(item => item.OrganizationId == targetOrganization.Id && item.PoNumber == sourcePo.PoNumber, cancellationToken))
                continue;
            var po = new PurchaseOrder
            {
                Id = Guid.NewGuid(),
                OrganizationId = targetOrganization.Id,
                OperatingUnitId = unit.Id,
                SupplierId = supplierId,
                PoNumber = sourcePo.PoNumber,
                PurchaseOrderType = sourcePo.PurchaseOrderType,
                CompanyCode = sourcePo.CompanyCode,
                ErpSupplierId = sourcePo.ErpSupplierId,
                SupplierName = FirstRealName(sourcePo.SupplierName) ?? sourceSuppliers.FirstOrDefault(item => item.Id == sourcePo.SupplierId)?.Name,
                PurchasingOrganization = sourcePo.PurchasingOrganization,
                PurchasingGroup = sourcePo.PurchasingGroup,
                PaymentTerms = sourcePo.PaymentTerms,
                PoCategory = sourcePo.PoCategory,
                PoDate = sourcePo.PoDate,
                DeliveryDate = sourcePo.DeliveryDate,
                Currency = sourcePo.Currency,
                TotalNetAmount = sourcePo.TotalNetAmount,
                TotalTaxAmount = sourcePo.TotalTaxAmount,
                TotalAmount = sourcePo.TotalAmount,
                TotalOrderedQuantity = sourcePo.TotalOrderedQuantity,
                TotalReceivedQuantity = sourcePo.TotalReceivedQuantity,
                Status = sourcePo.Status,
                SourceSystem = sourcePo.SourceSystem,
                EntityCode = sourcePo.EntityCode,
                CreatedAt = now,
                UpdatedAt = now,
            };
            foreach (var item in sourcePo.Items)
            {
                po.Items.Add(new PurchaseOrderItem
                {
                    Id = Guid.NewGuid(),
                    PurchaseOrderId = po.Id,
                    LineNumber = item.LineNumber,
                    ItemNumber = item.ItemNumber,
                    MaterialCode = item.MaterialCode,
                    Description = item.Description,
                    OrderedQuantity = item.OrderedQuantity,
                    ReceivedQuantity = item.ReceivedQuantity,
                    OpenQuantity = item.OpenQuantity,
                    Uom = item.Uom,
                    UnitPrice = item.UnitPrice,
                    PriceQuantity = item.PriceQuantity,
                    ItemAmount = item.ItemAmount,
                    TaxCode = item.TaxCode,
                    TaxAmount = item.TaxAmount,
                    GrossItemAmount = item.GrossItemAmount,
                    Currency = item.Currency,
                    MaterialGroup = item.MaterialGroup,
                    Plant = item.Plant,
                    StorageLocation = item.StorageLocation,
                    ItemCategory = item.ItemCategory,
                    AccountAssignmentCategory = item.AccountAssignmentCategory,
                    GoodsReceiptExpected = item.GoodsReceiptExpected,
                    InvoiceExpected = item.InvoiceExpected,
                    DeliveryCompleted = item.DeliveryCompleted,
                    DeletionIndicator = item.DeletionIndicator,
                    Status = item.Status,
                    CreatedAt = now,
                    UpdatedAt = now,
                });
            }
            db.PurchaseOrders.Add(po);
        }
    }

    public static bool NeedsNameRestore(string? value) =>
        string.IsNullOrWhiteSpace(value) || InvoiceOcrFieldReader.IsSupplierIdLabel(value);

    private static string? FirstRealName(params object?[] sources)
    {
        foreach (var source in sources)
        {
            if (source is string text && !NeedsNameRestore(text))
                return text.Trim();
            if (source is IEnumerable<string?> list)
            {
                foreach (var item in list)
                {
                    if (!NeedsNameRestore(item))
                        return item!.Trim();
                }
            }
        }
        return null;
    }

    private static bool SameDatabase(string left, string right)
    {
        var a = new Npgsql.NpgsqlConnectionStringBuilder(DatabaseUrl.Normalize(left));
        var b = new Npgsql.NpgsqlConnectionStringBuilder(DatabaseUrl.Normalize(right));
        return string.Equals(a.Host, b.Host, StringComparison.OrdinalIgnoreCase)
            && a.Port == b.Port
            && string.Equals(a.Database, b.Database, StringComparison.OrdinalIgnoreCase);
    }
}
