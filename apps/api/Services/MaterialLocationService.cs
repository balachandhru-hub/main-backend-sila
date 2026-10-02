using Microsoft.EntityFrameworkCore;
using SilaMe.Api.Auth;
using SilaMe.Api.Data;
using SilaMe.Api.DTOs;
using SilaMe.Api.Models;

namespace SilaMe.Api.Services;

public sealed class MaterialLocationService(SilaMeDbContext db)
{
    public async Task<IReadOnlyList<MaterialLocationRow>> ListForMaterialAsync(Guid organizationId, Guid materialId, CancellationToken cancellationToken)
    {
        await Material(organizationId, materialId, cancellationToken);
        var rows = await db.MaterialLocations.AsNoTracking()
            .Include(item => item.InventoryLocation)
            .Include(item => item.Material)
            .Where(item => item.OrganizationId == organizationId && item.MaterialId == materialId)
            .OrderBy(item => item.InventoryLocation.LocationCode)
            .ToListAsync(cancellationToken);
        return rows.Select(ToRow).ToList();
    }

    public async Task<IReadOnlyList<MaterialLocationRow>> ListForLocationAsync(Guid organizationId, Guid locationId, CancellationToken cancellationToken)
    {
        await Location(organizationId, locationId, cancellationToken);
        var rows = await db.MaterialLocations.AsNoTracking()
            .Include(item => item.InventoryLocation)
            .Include(item => item.Material)
            .Where(item => item.OrganizationId == organizationId && item.InventoryLocationId == locationId)
            .OrderBy(item => item.Material.MaterialCode)
            .ToListAsync(cancellationToken);
        return rows.Select(ToRow).ToList();
    }

    public async Task<MaterialLocationRow> UpsertAsync(Session session, Guid organizationId, MaterialLocationUpsertRequest request, CancellationToken cancellationToken)
    {
        var material = await Material(organizationId, request.MaterialId, cancellationToken);
        var location = await Location(organizationId, request.InventoryLocationId, cancellationToken);
        if (!MaterialNormalized.CanHoldStock(material))
            throw new RecipeManagementException("MATERIAL_NOT_INVENTORY", "Only inventory-item STOCK materials can be assigned to locations.");
        var now = DateTime.UtcNow;
        var row = await db.MaterialLocations.SingleOrDefaultAsync(item =>
            item.OrganizationId == organizationId && item.MaterialId == material.Id && item.InventoryLocationId == location.Id, cancellationToken);
        if (row is null)
        {
            row = new MaterialLocation
            {
                Id = Guid.NewGuid(),
                OrganizationId = organizationId,
                MaterialId = material.Id,
                InventoryLocationId = location.Id,
                CreatedAt = now,
                CreatedBy = session.UserId,
            };
            db.MaterialLocations.Add(row);
        }
        row.StockingStatus = Enum.TryParse<MaterialStockingStatus>(request.StockingStatus, true, out var status) ? status : MaterialStockingStatus.ACTIVE;
        row.StockingType = Enum.TryParse<MaterialStockingType>(request.StockingType, true, out var type) ? type : MaterialStockingType.REGULAR;
        row.MinimumStock = request.MinimumStock;
        row.MaximumStock = request.MaximumStock;
        row.ReorderPoint = request.ReorderPoint;
        row.SafetyStock = request.SafetyStock;
        row.ParLevel = request.ParLevel;
        row.PreferredSourceLocationId = request.PreferredSourceLocationId;
        row.ReplenishmentMethod = request.ReplenishmentMethod;
        row.Active = request.Active ?? true;
        if (!row.Active) row.StockingStatus = MaterialStockingStatus.INACTIVE;
        row.UpdatedAt = now;
        row.UpdatedBy = session.UserId;
        await db.SaveChangesAsync(cancellationToken);
        return (await ListForMaterialAsync(organizationId, material.Id, cancellationToken)).Single(item => item.InventoryLocationId == location.Id);
    }

    public async Task RemoveAsync(Guid organizationId, Guid materialId, Guid locationId, CancellationToken cancellationToken)
    {
        var row = await db.MaterialLocations.SingleOrDefaultAsync(item =>
            item.OrganizationId == organizationId && item.MaterialId == materialId && item.InventoryLocationId == locationId, cancellationToken);
        if (row is null) return;
        row.Active = false;
        row.StockingStatus = MaterialStockingStatus.INACTIVE;
        row.UpdatedAt = DateTime.UtcNow;
        await db.SaveChangesAsync(cancellationToken);
    }

    public async Task<InventoryBalance> PostOpeningStockAsync(Session session, Guid organizationId, OpeningStockRequest request, CancellationToken cancellationToken)
    {
        if (request.Quantity <= 0)
            throw new RecipeManagementException("INVALID_QUANTITY", "Opening quantity must be greater than zero.");
        var material = await Material(organizationId, request.MaterialId, cancellationToken);
        if (!MaterialNormalized.CanHoldStock(material) || material.InventoryType == InventoryItemType.SERVICE)
            throw new RecipeManagementException("SERVICE_NO_STOCK", "SERVICE and non-stock materials cannot receive physical inventory.");
        var location = await Location(organizationId, request.InventoryLocationId, cancellationToken);
        if (!location.InventoryEnabled)
            throw new RecipeManagementException("INVENTORY_DISABLED", "This location is not inventory-enabled.");
        var balance = await db.InventoryBalances.SingleOrDefaultAsync(item =>
            item.MaterialId == material.Id && item.InventoryLocationId == location.Id && item.BatchId == null, cancellationToken);
        if (balance is null)
        {
            balance = new InventoryBalance
            {
                Id = Guid.NewGuid(), OrganizationId = organizationId, MaterialId = material.Id, InventoryLocationId = location.Id,
                OnHandQty = 0, ReservedQty = 0, AvailableQty = 0, InTransitQty = 0, BaseUom = material.BaseUom, Currency = material.Currency, UpdatedAt = DateTime.UtcNow
            };
            db.InventoryBalances.Add(balance);
        }
        var cost = request.UnitCost ?? MaterialCosting.EffectiveUnitCost(material);
        balance.OnHandQty += request.Quantity;
        balance.AvailableQty = balance.OnHandQty - balance.ReservedQty;
        balance.InventoryValue = (cost ?? 0) * balance.OnHandQty;
        balance.LastMovementAt = DateTime.UtcNow;
        balance.UpdatedAt = DateTime.UtcNow;
        db.InventoryStockTransactions.Add(new InventoryStockTransaction
        {
            Id = Guid.NewGuid(), OrganizationId = organizationId,
            TransactionId = $"OS{DateTime.UtcNow:yyyyMMddHHmmss}{Guid.NewGuid().ToString("N")[..6].ToUpperInvariant()}",
            TransactionType = InventoryTxnType.OPENING_STOCK, MaterialId = material.Id, InventoryLocationId = location.Id,
            Quantity = request.Quantity, Uom = material.BaseUom, BaseQuantity = request.Quantity, BaseUom = material.BaseUom,
            Direction = InventoryDirection.IN, UnitCost = cost, TransactionValue = (cost ?? 0) * request.Quantity,
            Currency = material.Currency, ReferenceType = "OPENING_STOCK", BusinessDate = DateTime.UtcNow.Date, PostingDate = DateTime.UtcNow,
            Source = "OPENING_STOCK", CreatedBy = session.UserId ?? Guid.Empty, CreatedAt = DateTime.UtcNow
        });
        await db.SaveChangesAsync(cancellationToken);
        return balance;
    }

    private async Task<Material> Material(Guid organizationId, Guid id, CancellationToken cancellationToken) =>
        await db.Materials.SingleOrDefaultAsync(item => item.Id == id && item.OrganizationId == organizationId, cancellationToken)
        ?? throw new RecipeManagementException("MATERIAL_NOT_FOUND", "The material was not found.", 404);

    private async Task<InventoryLocation> Location(Guid organizationId, Guid id, CancellationToken cancellationToken) =>
        await db.InventoryLocations.SingleOrDefaultAsync(item => item.Id == id && item.OrganizationId == organizationId, cancellationToken)
        ?? throw new RecipeManagementException("LOCATION_NOT_FOUND", "The inventory location was not found.", 404);

    private static MaterialLocationRow ToRow(MaterialLocation item) => new(
        item.Id, item.MaterialId, item.Material.MaterialCode, item.Material.Description, item.InventoryLocationId,
        item.InventoryLocation.LocationCode, item.InventoryLocation.LocationName, item.StockingStatus.ToString(),
        item.StockingType.ToString(), item.MinimumStock, item.MaximumStock, item.ReorderPoint, item.SafetyStock,
        item.ParLevel, item.PreferredSourceLocationId, item.ReplenishmentMethod, item.Active);
}
