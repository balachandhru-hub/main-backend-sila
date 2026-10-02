using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using SilaMe.Api.Auth;
using SilaMe.Api.Data;
using SilaMe.Api.DTOs;
using SilaMe.Api.Models;

namespace SilaMe.Api.Services;

public sealed class PosSourceService(SilaMeDbContext db, ProtectedIntegrationCredentialStore secrets, RecipeCatalogService recipes)
{
    public async Task<IReadOnlyList<PosSourceRow>> ListAsync(Guid organizationId, CancellationToken cancellationToken) =>
        (await db.PosSources.AsNoTracking().Where(item => item.OrganizationId == organizationId).OrderBy(item => item.Name).ToListAsync(cancellationToken))
        .Select(ToRow).ToList();

    public async Task<PosSourceRow> UpsertAsync(Guid organizationId, Guid? id, PosSourceUpsertRequest request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.Name) || string.IsNullOrWhiteSpace(request.PosSystem))
            throw new RecipeManagementException("POS_SOURCE_REQUIRED", "Integration name and POS system are required.");
        if (!Enum.TryParse<PosIntegrationKind>(request.IntegrationKind, true, out var kind))
            throw new RecipeManagementException("POS_INTEGRATION_TYPE_INVALID", "Integration type must be API or DATABASE.");
        var source = id is null ? null : await db.PosSources.SingleOrDefaultAsync(item => item.Id == id && item.OrganizationId == organizationId, cancellationToken);
        if (id is not null && source is null) throw new RecipeManagementException("POS_SOURCE_NOT_FOUND", "The POS integration was not found.", 404);
        var now = DateTime.UtcNow;
        source ??= new PosSource { Id = Guid.NewGuid(), OrganizationId = organizationId, Name = request.Name.Trim(), PosSystem = request.PosSystem.Trim(), CreatedAt = now };
        if (id is null) db.PosSources.Add(source);
        source.Name = request.Name.Trim();
        source.PosSystem = request.PosSystem.Trim();
        source.IntegrationKind = kind;
        source.Status = Enum.TryParse<PosSourceStatus>(request.Status, true, out var status) ? status : PosSourceStatus.DRAFT;
        source.ApiIntegrationConfigurationId = kind == PosIntegrationKind.API ? request.ApiIntegrationConfigurationId : null;
        source.DatabaseType = kind == PosIntegrationKind.DATABASE ? request.DatabaseType : null;
        source.Host = request.Host;
        source.Port = request.Port;
        source.DatabaseName = request.DatabaseName;
        source.SchemaName = request.SchemaName;
        source.TableOrView = request.TableOrView;
        source.CredentialReference = request.CredentialReference;
        source.TransactionIdField = request.TransactionIdField;
        source.BusinessDateField = request.BusinessDateField;
        source.OutletField = request.OutletField;
        source.ItemCodeField = request.ItemCodeField;
        source.QuantityField = request.QuantityField;
        source.StatusField = request.StatusField;
        if (!string.IsNullOrWhiteSpace(request.Password))
            source.ProtectedPassword = secrets.Protect(request.Password);
        source.UpdatedAt = now;
        await db.SaveChangesAsync(cancellationToken);
        return ToRow(source);
    }

    public async Task<IReadOnlyList<PosOutletMappingRow>> ListOutletsAsync(Guid organizationId, Guid sourceId, CancellationToken cancellationToken)
    {
        var mappings = await db.PosOutletMappings.AsNoTracking().Where(item => item.OrganizationId == organizationId && item.PosSourceId == sourceId)
            .OrderBy(item => item.PosOutletCode).ToListAsync(cancellationToken);
        var locations = await db.InventoryLocations.AsNoTracking().Include(item => item.PropertyLocation)
            .Where(item => item.OrganizationId == organizationId).ToListAsync(cancellationToken);
        return mappings.Select(item => ToOutletRow(item, RecipePosSales.FindStockingLocation(locations, item.OutletCode, item.PosOutletCode))).ToList();
    }

    public async Task<PosOutletMappingRow> UpsertOutletAsync(Guid organizationId, Guid sourceId, Guid? id, PosOutletMappingUpsertRequest request, CancellationToken cancellationToken)
    {
        await EnsureSourceAsync(organizationId, sourceId, cancellationToken);
        var locations = await db.InventoryLocations.Include(item => item.PropertyLocation)
            .Where(item => item.OrganizationId == organizationId).ToListAsync(cancellationToken);
        var location = RecipePosSales.FindStockingLocation(locations, request.OutletCode, request.PosOutletCode)
            ?? throw new RecipeManagementException("OUTLET_LOCATION_MISSING", "Map the POS outlet to a Location Master outlet, store, or venue.", 400);
        var propertyCode = location.PropertyLocation?.LocationCode ?? request.PropertyCode?.Trim() ?? location.LocationCode;
        var mapping = id is null ? null : await db.PosOutletMappings.SingleOrDefaultAsync(item => item.Id == id && item.PosSourceId == sourceId, cancellationToken);
        mapping ??= new PosOutletMapping
        {
            Id = Guid.NewGuid(), OrganizationId = organizationId, PosSourceId = sourceId, PosOutletCode = request.PosOutletCode.Trim(),
            PropertyCode = propertyCode, OutletCode = location.LocationCode,
            PlantCode = request.PlantCode?.Trim() ?? string.Empty,
            StorageLocationCode = request.StorageLocationCode?.Trim() ?? string.Empty, CreatedAt = DateTime.UtcNow,
        };
        if (id is null) db.PosOutletMappings.Add(mapping);
        mapping.PosOutletCode = request.PosOutletCode.Trim();
        mapping.PosOutletName = request.PosOutletName?.Trim() ?? location.LocationName;
        mapping.PropertyCode = propertyCode;
        mapping.OutletCode = location.LocationCode;
        if (!string.IsNullOrWhiteSpace(request.PlantCode)) mapping.PlantCode = request.PlantCode.Trim();
        if (!string.IsNullOrWhiteSpace(request.StorageLocationCode)) mapping.StorageLocationCode = request.StorageLocationCode.Trim();
        mapping.CompanyCode = request.CompanyCode?.Trim() ?? location.CompanyCode;
        mapping.UpdatedAt = DateTime.UtcNow;
        await db.SaveChangesAsync(cancellationToken);
        return ToOutletRow(mapping, location);
    }

    private static PosOutletMappingRow ToOutletRow(PosOutletMapping mapping, InventoryLocation? location) => new(
        mapping.Id, mapping.PosSourceId, mapping.PosOutletCode, mapping.PosOutletName, mapping.PropertyCode, mapping.OutletCode,
        mapping.PlantCode, mapping.StorageLocationCode, mapping.CompanyCode,
        location is null ? null : RecipePosSales.LocationKindLabel(location.LocationType), location?.LocationName);

    public async Task<IReadOnlyList<PosItemRecipeMappingRow>> ListItemsAsync(Guid organizationId, Guid sourceId, CancellationToken cancellationToken) =>
        await db.PosItemRecipeMappings.AsNoTracking().Include(item => item.Recipe)
            .Where(item => item.OrganizationId == organizationId && item.PosSourceId == sourceId)
            .OrderBy(item => item.PosItemCode)
            .Select(item => new PosItemRecipeMappingRow(item.Id, item.PosSourceId, item.PosItemCode, item.PosItemDescription, item.RecipeId, item.Recipe.RecipeCode, item.Recipe.Name))
            .ToListAsync(cancellationToken);

    public async Task<PosItemRecipeMappingRow> UpsertItemAsync(Session session, Guid organizationId, Guid sourceId, Guid? id, PosItemRecipeMappingUpsertRequest request, CancellationToken cancellationToken)
    {
        await EnsureSourceAsync(organizationId, sourceId, cancellationToken);
        if (!await db.Recipes.AnyAsync(item => item.Id == request.RecipeId && item.OrganizationId == organizationId, cancellationToken))
            throw new RecipeManagementException("RECIPE_NOT_FOUND", "The mapped recipe was not found.", 404);
        var mapping = id is null ? null : await db.PosItemRecipeMappings.Include(item => item.Recipe).SingleOrDefaultAsync(item => item.Id == id && item.PosSourceId == sourceId, cancellationToken);
        mapping ??= new PosItemRecipeMapping { Id = Guid.NewGuid(), OrganizationId = organizationId, PosSourceId = sourceId, PosItemCode = request.PosItemCode.Trim(), RecipeId = request.RecipeId, CreatedAt = DateTime.UtcNow };
        if (id is null) db.PosItemRecipeMappings.Add(mapping);
        mapping.PosItemCode = request.PosItemCode.Trim();
        mapping.PosItemDescription = request.PosItemDescription?.Trim();
        mapping.RecipeId = request.RecipeId;
        mapping.UpdatedAt = DateTime.UtcNow;
        await db.SaveChangesAsync(cancellationToken);
        await recipes.ApplyPosItemAsync(organizationId, request.RecipeId, mapping.PosItemCode, mapping.PosItemDescription, null, null, cancellationToken, session, true);
        await db.Entry(mapping).Reference(item => item.Recipe).LoadAsync(cancellationToken);
        return new PosItemRecipeMappingRow(mapping.Id, mapping.PosSourceId, mapping.PosItemCode, mapping.PosItemDescription, mapping.RecipeId, mapping.Recipe.RecipeCode, mapping.Recipe.Name);
    }

    public async Task<IReadOnlyList<OutletMenuItemRow>> ListMenuItemsAsync(Guid organizationId, Guid sourceId, Guid outletId, CancellationToken cancellationToken)
    {
        await EnsureOutletAsync(organizationId, sourceId, outletId, cancellationToken);
        return await db.OutletMenuItems.AsNoTracking().Include(item => item.Recipe).Include(item => item.Outlet)
            .Where(item => item.OrganizationId == organizationId && item.OutletMappingId == outletId)
            .OrderBy(item => item.Recipe.RecipeCode)
            .Select(item => new OutletMenuItemRow(item.Id, item.OutletMappingId, item.Outlet.OutletCode, item.Outlet.PosOutletName, item.RecipeId,
                item.Recipe.RecipeCode, item.Recipe.Name, item.PosCode ?? item.Recipe.PosCode, item.PosItem ?? item.Recipe.PosItem, item.Recipe.PosItemMenuPrice))
            .ToListAsync(cancellationToken);
    }

    public async Task<OutletMenuItemRow> AddMenuItemAsync(Guid organizationId, Guid sourceId, Guid outletId, OutletMenuItemUpsertRequest request, CancellationToken cancellationToken)
    {
        await EnsureOutletAsync(organizationId, sourceId, outletId, cancellationToken);
        var recipe = await db.Recipes.SingleOrDefaultAsync(item => item.Id == request.RecipeId && item.OrganizationId == organizationId, cancellationToken)
            ?? throw new RecipeManagementException("RECIPE_NOT_FOUND", "The recipe was not found.", 404);
        if (string.IsNullOrWhiteSpace(recipe.PosItem))
            throw new RecipeManagementException("POS_ITEM_REQUIRED", "Choose a recipe that already has POS Item updated.");
        if (await db.OutletMenuItems.AnyAsync(item => item.OutletMappingId == outletId && item.RecipeId == recipe.Id, cancellationToken))
            throw new RecipeManagementException("MENU_ITEM_EXISTS", "This recipe is already on the outlet menu.");
        var now = DateTime.UtcNow;
        var row = new OutletMenuItem
        {
            Id = Guid.NewGuid(), OrganizationId = organizationId, OutletMappingId = outletId, RecipeId = recipe.Id,
            PosCode = recipe.PosCode, PosItem = recipe.PosItem, CreatedAt = now, UpdatedAt = now,
        };
        db.OutletMenuItems.Add(row);
        await db.SaveChangesAsync(cancellationToken);
        var outlet = await db.PosOutletMappings.AsNoTracking().SingleAsync(item => item.Id == outletId, cancellationToken);
        return new OutletMenuItemRow(row.Id, outletId, outlet.OutletCode, outlet.PosOutletName, recipe.Id, recipe.RecipeCode, recipe.Name, row.PosCode, row.PosItem, recipe.PosItemMenuPrice);
    }

    public async Task RemoveMenuItemAsync(Guid organizationId, Guid sourceId, Guid outletId, Guid id, CancellationToken cancellationToken)
    {
        await EnsureOutletAsync(organizationId, sourceId, outletId, cancellationToken);
        var row = await db.OutletMenuItems.SingleOrDefaultAsync(item => item.Id == id && item.OutletMappingId == outletId && item.OrganizationId == organizationId, cancellationToken)
            ?? throw new RecipeManagementException("MENU_ITEM_NOT_FOUND", "The menu item was not found.", 404);
        db.OutletMenuItems.Remove(row);
        await db.SaveChangesAsync(cancellationToken);
    }

    private async Task EnsureOutletAsync(Guid organizationId, Guid sourceId, Guid outletId, CancellationToken cancellationToken)
    {
        await EnsureSourceAsync(organizationId, sourceId, cancellationToken);
        if (!await db.PosOutletMappings.AnyAsync(item => item.Id == outletId && item.PosSourceId == sourceId && item.OrganizationId == organizationId, cancellationToken))
            throw new RecipeManagementException("OUTLET_NOT_FOUND", "The outlet mapping was not found.", 404);
    }

    private async Task EnsureSourceAsync(Guid organizationId, Guid sourceId, CancellationToken cancellationToken)
    {
        if (!await db.PosSources.AnyAsync(item => item.Id == sourceId && item.OrganizationId == organizationId, cancellationToken))
            throw new RecipeManagementException("POS_SOURCE_NOT_FOUND", "The POS integration was not found.", 404);
    }

    private static PosSourceRow ToRow(PosSource item) => new(
        item.Id, item.Name, item.PosSystem, item.IntegrationKind.ToString(), item.Status.ToString(), item.ApiIntegrationConfigurationId,
        item.DatabaseType, item.Host, item.Port, item.DatabaseName, item.SchemaName, item.TableOrView, item.CredentialReference,
        !string.IsNullOrWhiteSpace(item.ProtectedPassword), item.TransactionIdField, item.BusinessDateField, item.OutletField,
        item.ItemCodeField, item.QuantityField, item.StatusField);
}

public sealed class RecipeExplosionService(SilaMeDbContext db)
{
    public async Task ExplodeAsync(RecipeConsumptionTransaction transaction, CancellationToken cancellationToken)
    {
        if (transaction.RecipeVersionId is null)
            throw new RecipeManagementException("RECIPE_VERSION_MISSING", "An approved recipe version is required before explosion.");
        var version = await db.RecipeVersions.Include(item => item.Ingredients).ThenInclude(item => item.Material)
            .Include(item => item.Recipe)
            .SingleAsync(item => item.Id == transaction.RecipeVersionId, cancellationToken);
        db.RecipeConsumptionLines.RemoveRange(db.RecipeConsumptionLines.Where(item => item.TransactionId == transaction.Id));
        var sequence = 1;
        var sold = Math.Abs(transaction.QuantitySold);
        var serving = version.Recipe.ServingSize is > 0 ? version.Recipe.ServingSize.Value : 1m;
        foreach (var ingredient in version.Ingredients.OrderBy(item => item.Sequence))
        {
            db.RecipeConsumptionLines.Add(new RecipeConsumptionLine
            {
                Id = Guid.NewGuid(), TransactionId = transaction.Id, MaterialId = ingredient.MaterialId,
                MaterialCode = ingredient.Material?.MaterialCode, Description = ingredient.MaterialDescription ?? ingredient.UnmappedIngredientName,
                ConsumedQuantity = RecipeCosting.ConsumedQuantity(ingredient, sold, serving),
                Uom = ingredient.ConsumptionUom ?? ingredient.Material?.BaseUom ?? ingredient.Uom, UnitCost = ingredient.UnitCost, Sequence = sequence++,
            });
        }
        transaction.Status = RecipeTransactionStatus.RECIPE_EXPLODED;
        transaction.RecipeVersionNumber = version.VersionNumber;
        await AddEventAsync(db, transaction, "RECIPE_EXPLOSION", "RECIPE_EXPLODED", $"Version {version.VersionNumber} × {transaction.QuantitySold}", cancellationToken);
    }

    public void AddDirectLine(RecipeConsumptionTransaction transaction, Material material, decimal quantity, string uom)
    {
        db.RecipeConsumptionLines.RemoveRange(db.RecipeConsumptionLines.Where(item => item.TransactionId == transaction.Id));
        db.RecipeConsumptionLines.Add(new RecipeConsumptionLine
        {
            Id = Guid.NewGuid(), TransactionId = transaction.Id, MaterialId = material.Id, MaterialCode = material.MaterialCode,
            Description = material.Description, ConsumedQuantity = quantity, Uom = RecipeUom.Normalize(uom),
            UnitCost = material.UnitCost ?? material.MovingAveragePrice ?? material.StandardPrice, Sequence = 1,
        });
        transaction.Status = RecipeTransactionStatus.RECIPE_EXPLODED;
    }

    public static void QueueEvent(SilaMeDbContext db, Guid transactionId, string step, string status, string? detail)
    {
        db.Set<RecipeTransactionEvent>().Add(new RecipeTransactionEvent
        {
            Id = Guid.NewGuid(), TransactionId = transactionId, Step = step, Status = status, Detail = detail, CreatedAt = DateTime.UtcNow,
        });
    }

    public static Task AddEventAsync(SilaMeDbContext db, RecipeConsumptionTransaction transaction, string step, string status, string? detail, CancellationToken cancellationToken)
    {
        QueueEvent(db, transaction.Id, step, status, detail);
        return Task.CompletedTask;
    }
}

public sealed class RecipeInventoryPostingService(SilaMeDbContext db, IntegrationService integrations)
{
    public async Task TryPostAsync(RecipeConsumptionTransaction transaction, CancellationToken cancellationToken, bool postExternal = true)
    {
        if (transaction.Status == RecipeTransactionStatus.POSTED)
            return;
        if (transaction.Status == RecipeTransactionStatus.POSTING_UNKNOWN)
            throw Fail(transaction, "POSTING_UNKNOWN", RecipeSapUpdateStock.Friendly("POSTING_UNKNOWN", null), "S4_RESPONSE");

        var existing = await db.RecipeInventoryPostings.SingleOrDefaultAsync(item => item.TransactionId == transaction.Id, cancellationToken);
        if (existing?.Status == RecipeTransactionStatus.POSTED)
        {
            transaction.Status = RecipeTransactionStatus.POSTED;
            return;
        }

        var unmapped = await db.RecipeConsumptionLines.AnyAsync(item => item.TransactionId == transaction.Id && item.MaterialId == null, cancellationToken);
        if (unmapped)
            throw Fail(transaction, "MATERIAL_NOT_MAPPED", "An exploded ingredient is not linked to Material Master.", "RECIPE_EXPLOSION");

        var location = await ResolveLocationAsync(transaction, cancellationToken)
            ?? throw Fail(transaction, "OUTLET_LOCATION_MISSING", RecipeSapUpdateStock.Friendly("OUTLET_LOCATION_MISSING", null), "OUTLET_MAPPING");
        if (location.Status != StatusKind.ACTIVE)
            throw Fail(transaction, "OUTLET_INACTIVE", RecipeSapUpdateStock.Friendly("OUTLET_INACTIVE", null), "OUTLET_MAPPING");
        if (!location.ConsumptionEnabled)
            throw Fail(transaction, "CONSUMPTION_DISABLED", RecipeSapUpdateStock.Friendly("CONSUMPTION_DISABLED", null), "OUTLET_MAPPING");

        await ApplySilaStockAsync(transaction, location, cancellationToken);
        transaction.Status = RecipeTransactionStatus.READY_TO_POST;
            await RecipeExplosionService.AddEventAsync(db, transaction, "INVENTORY_CONSUMPTION", "READY_TO_POST",
            $"{RecipePosSales.LocationKindLabel(location.LocationType)} {location.LocationName} ({location.LocationCode})", cancellationToken);
        if (postExternal)
            await PostUpdateStockAsync([transaction], transaction.UploadBatchId, cancellationToken);
    }

    public async Task PostUpdateStockAsync(IReadOnlyList<RecipeConsumptionTransaction> transactions, Guid? uploadBatchId, CancellationToken cancellationToken)
    {
        var pending = transactions.Where(item => item.Status is RecipeTransactionStatus.READY_TO_POST or RecipeTransactionStatus.FAILED or RecipeTransactionStatus.REPROCESSING).ToList();
        if (pending.Count == 0) return;
        var items = new List<RecipeSapStockItem>();
        foreach (var transaction in pending)
        {
            if (transaction.Status == RecipeTransactionStatus.POSTED) continue;
            var built = await BuildSapItemsAsync(transaction, cancellationToken);
            items.AddRange(built);
        }
        foreach (var group in RecipeSapUpdateStock.Aggregate(items).GroupBy(item => (item.BusinessDate, item.Plant, item.StorageLocation)))
        {
            var groupItems = group.ToList();
            var firstTxn = pending.First(item => groupItems.SelectMany(row => row.TransactionIds).Contains(item.Id));
            await SendGroupAsync(firstTxn.OrganizationId, uploadBatchId, firstTxn.CompanyCode, group.Key.BusinessDate, group.Key.Plant, group.Key.StorageLocation, groupItems, pending, cancellationToken);
        }
    }

    private async Task<InventoryLocation?> ResolveLocationAsync(RecipeConsumptionTransaction transaction, CancellationToken cancellationToken)
    {
        var locations = await db.InventoryLocations.Include(item => item.PropertyLocation)
            .Where(item => item.OrganizationId == transaction.OrganizationId).ToListAsync(cancellationToken);
        return RecipePosSales.LocationForTransaction(locations, transaction);
    }

    private async Task ApplySilaStockAsync(RecipeConsumptionTransaction transaction, InventoryLocation location, CancellationToken cancellationToken)
    {
        if (await db.InventoryStockTransactions.AnyAsync(item => item.ReferenceId == transaction.Id && item.ReferenceType == "POS_SALE", cancellationToken))
            return;
        var lines = await db.RecipeConsumptionLines.Where(item => item.TransactionId == transaction.Id).ToListAsync(cancellationToken);
        if (lines.Count == 0)
            throw Fail(transaction, "NO_CONSUMPTION_LINES", "The sale did not explode to any inventory materials.", "RECIPE_EXPLOSION");
        var recipe = transaction.RecipeId is { } recipeId
            ? await db.Recipes.AsNoTracking().SingleOrDefaultAsync(item => item.Id == recipeId, cancellationToken)
            : null;
        var txnType = recipe?.ItemMode == RecipeItemMode.DIRECT ? InventoryTxnType.DIRECT_CONSUMPTION : InventoryTxnType.RECIPE_CONSUMPTION;
        var posted = 0;
        foreach (var line in lines)
        {
            if (line.MaterialId is null) continue;
            var material = await db.Materials.SingleAsync(item => item.Id == line.MaterialId, cancellationToken);
            if (material.Status != StatusKind.ACTIVE)
                throw Fail(transaction, "MATERIAL_INACTIVE", RecipeSapUpdateStock.Friendly("MATERIAL_INACTIVE", null), "RECIPE_EXPLOSION");
            if (!MaterialNormalized.CanHoldStock(material))
            {
                await RecipeExplosionService.AddEventAsync(db, transaction, "SILA_STOCK_SKIPPED", "READY_TO_POST", $"{material.MaterialCode} is not a STOCK inventory item.", cancellationToken);
                continue;
            }
            var conversions = await db.MaterialUomConversions.AsNoTracking().Where(item => item.MaterialId == material.Id).ToListAsync(cancellationToken);
            var unitCost = line.UnitCost ?? material.UnitCost ?? material.MovingAveragePrice ?? material.StandardPrice;
            var qty = RecipeUom.Convert(line.ConsumedQuantity, line.Uom, material.BaseUom, conversions)
                ?? throw Fail(transaction, "UOM_CONVERSION_MISSING", RecipeSapUpdateStock.Friendly("UOM_CONVERSION_MISSING", null), "RECIPE_EXPLOSION");
            qty = Math.Abs(qty);
            var balance = await db.InventoryBalances.SingleOrDefaultAsync(item =>
                item.MaterialId == material.Id && item.InventoryLocationId == location.Id && item.BatchId == null, cancellationToken);
            if (balance is null)
            {
                balance = new InventoryBalance
                {
                    Id = Guid.NewGuid(), OrganizationId = transaction.OrganizationId, MaterialId = material.Id, InventoryLocationId = location.Id,
                    OnHandQty = 0, ReservedQty = 0, AvailableQty = 0, InTransitQty = 0, BaseUom = material.BaseUom, Currency = material.Currency, UpdatedAt = DateTime.UtcNow,
                };
                db.InventoryBalances.Add(balance);
            }
            var direction = transaction.QuantitySold < 0 ? InventoryDirection.IN : InventoryDirection.OUT;
            if (direction == InventoryDirection.OUT) balance.OnHandQty -= qty;
            else balance.OnHandQty += qty;
            balance.AvailableQty = balance.OnHandQty - balance.ReservedQty;
            balance.InventoryValue = (unitCost ?? 0) * balance.OnHandQty;
            balance.LastMovementAt = DateTime.UtcNow;
            balance.UpdatedAt = DateTime.UtcNow;
            var currency = RecipePosSales.Currency(transaction.RawReference) ?? location.Currency ?? material.Currency;
            var businessDate = transaction.BusinessDate?.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc) ?? DateTime.UtcNow.Date;
            db.InventoryStockTransactions.Add(new InventoryStockTransaction
            {
                Id = Guid.NewGuid(), OrganizationId = transaction.OrganizationId,
                TransactionId = $"POS{DateTime.UtcNow:yyyyMMddHHmmss}{Guid.NewGuid().ToString("N")[..6].ToUpperInvariant()}",
                TransactionType = txnType, MaterialId = material.Id, InventoryLocationId = location.Id,
                Quantity = Math.Abs(line.ConsumedQuantity), Uom = line.Uom, BaseQuantity = qty, BaseUom = material.BaseUom,
                Direction = direction, UnitCost = unitCost,
                TransactionValue = (unitCost ?? 0) * qty, Currency = currency,
                ReferenceType = "POS_SALE", ReferenceId = transaction.Id, BusinessDate = businessDate, PostingDate = DateTime.UtcNow,
                Source = "POS_SALES", CreatedBy = Guid.Empty, CreatedAt = DateTime.UtcNow,
            });
            posted++;
        }
        if (posted == 0)
            throw Fail(transaction, "NO_STOCK_ITEMS", "None of the exploded materials are STOCK inventory items.", "RECIPE_EXPLOSION");
    }

    private async Task<IReadOnlyList<RecipeSapStockItem>> BuildSapItemsAsync(RecipeConsumptionTransaction transaction, CancellationToken cancellationToken)
    {
        var location = await ResolveLocationAsync(transaction, cancellationToken)
            ?? throw Fail(transaction, "OUTLET_LOCATION_MISSING", RecipeSapUpdateStock.Friendly("OUTLET_LOCATION_MISSING", null), "OUTLET_MAPPING");
        var wanted = RecipePosSales.NormalizeOutlet(transaction.PosOutletCode);
        var mapping = (await db.PosOutletMappings.Where(item => item.PosSourceId == transaction.PosSourceId).ToListAsync(cancellationToken))
            .FirstOrDefault(item => RecipePosSales.NormalizeOutlet(item.PosOutletCode) == wanted || RecipePosSales.NormalizeOutlet(item.OutletCode) == wanted);
        var resolved = await integrations.TryResolveUpdateStockAsync(transaction.OrganizationId, transaction.CompanyCode ?? location.CompanyCode, cancellationToken)
            ?? throw Fail(transaction, "UPDATE_STOCK_ROUTE_NOT_CONFIGURED", RecipeSapUpdateStock.Friendly("UPDATE_STOCK_ROUTE_NOT_CONFIGURED", null), "S4_POSTING");
        var (plant, storage) = RecipeSapUpdateStock.ResolvePlantStorage(mapping, location, null, resolved.Configuration.Plant);
        if (string.IsNullOrWhiteSpace(plant))
            throw Fail(transaction, "PLANT_MAPPING_MISSING", RecipeSapUpdateStock.Friendly("PLANT_MAPPING_MISSING", null), "OUTLET_MAPPING");
        if (string.IsNullOrWhiteSpace(storage))
            throw Fail(transaction, "STORAGE_LOCATION_MAPPING_MISSING", RecipeSapUpdateStock.Friendly("STORAGE_LOCATION_MAPPING_MISSING", null), "OUTLET_MAPPING");
        transaction.PlantCode = plant;
        transaction.StorageLocationCode = storage;
        var date = transaction.BusinessDate ?? DateOnly.FromDateTime(DateTime.UtcNow);
        var movement = RecipeSapUpdateStock.MovementType(transaction.QuantitySold < 0 ? 1 : -1);
        var lines = await db.RecipeConsumptionLines.Where(item => item.TransactionId == transaction.Id).ToListAsync(cancellationToken);
        var items = new List<RecipeSapStockItem>();
        foreach (var line in lines)
        {
            if (line.MaterialId is null) continue;
            var material = await db.Materials.SingleAsync(item => item.Id == line.MaterialId, cancellationToken);
            if (!MaterialNormalized.CanHoldStock(material)) continue;
            var conversions = await db.MaterialUomConversions.AsNoTracking().Where(item => item.MaterialId == material.Id).ToListAsync(cancellationToken);
            var qty = RecipeUom.Convert(Math.Abs(line.ConsumedQuantity), line.Uom, material.BaseUom, conversions)
                ?? throw Fail(transaction, "UOM_CONVERSION_MISSING", RecipeSapUpdateStock.Friendly("UOM_CONVERSION_MISSING", null), "RECIPE_EXPLOSION");
            items.Add(new RecipeSapStockItem(date, plant, storage, material.MaterialCode, movement, Math.Abs(qty), material.BaseUom, [transaction.Id]));
        }
        if (items.Count == 0)
            throw Fail(transaction, "NO_STOCK_ITEMS", RecipeSapUpdateStock.Friendly("NO_STOCK_ITEMS", null), "RECIPE_EXPLOSION");
        return items;
    }

    private async Task SendGroupAsync(
        Guid organizationId, Guid? uploadBatchId, string? companyCode, DateOnly businessDate, string plant, string storage,
        IReadOnlyList<RecipeSapStockItem> items, IReadOnlyList<RecipeConsumptionTransaction> pending, CancellationToken cancellationToken)
    {
        var txnIds = items.SelectMany(item => item.TransactionIds).Distinct().ToHashSet();
        var groupTxns = pending.Where(item => txnIds.Contains(item.Id)).ToList();
        if (groupTxns.Count == 0 || groupTxns.All(item => item.Status == RecipeTransactionStatus.POSTED)) return;
        if (groupTxns.Any(item => item.Status == RecipeTransactionStatus.POSTING_UNKNOWN)) return;

        var existingPosting = await db.RecipeSapPostings.FirstOrDefaultAsync(item =>
            item.OrganizationId == organizationId && item.UploadBatchId == uploadBatchId &&
            item.BusinessDate == businessDate && item.Plant == plant && item.StorageLocation == storage &&
            item.Status == RecipeTransactionStatus.POSTED, cancellationToken);
        if (existingPosting is not null)
        {
            foreach (var transaction in groupTxns) MarkPosted(transaction, existingPosting);
            return;
        }

        var resolved = await integrations.TryResolveUpdateStockAsync(organizationId, companyCode, cancellationToken)
            ?? throw Fail(groupTxns[0], "UPDATE_STOCK_ROUTE_NOT_CONFIGURED", RecipeSapUpdateStock.Friendly("UPDATE_STOCK_ROUTE_NOT_CONFIGURED", null), "S4_POSTING");
        var requestJson = RecipeSapUpdateStock.BuildRequestJson(businessDate, items);
        var sap = new RecipeSapPosting
        {
            Id = Guid.NewGuid(), OrganizationId = organizationId, UploadBatchId = uploadBatchId, BusinessDate = businessDate,
            Plant = plant, StorageLocation = storage, CompanyCode = companyCode, Status = RecipeTransactionStatus.POSTING,
            IntegrationRouteId = resolved.RouteId, IntegrationConfigurationId = resolved.Configuration.Id,
            RequestJson = requestJson, CreatedAt = DateTime.UtcNow,
        };
        db.RecipeSapPostings.Add(sap);
        foreach (var transaction in groupTxns)
        {
            transaction.Status = RecipeTransactionStatus.POSTING;
            transaction.IntegrationRouteId = resolved.RouteId;
            transaction.IntegrationSystem = resolved.Configuration.Name;
            await RecipeExplosionService.AddEventAsync(db, transaction, "SAP_POSTING", "POSTING",
                $"{RecipeSapUpdateStock.ProcessType}/{resolved.Configuration.Name}", cancellationToken);
            var posting = await db.RecipeInventoryPostings.SingleOrDefaultAsync(item => item.TransactionId == transaction.Id, cancellationToken);
            if (posting is null)
            {
                posting = new RecipeInventoryPosting { Id = Guid.NewGuid(), TransactionId = transaction.Id, OrganizationId = organizationId, CreatedAt = DateTime.UtcNow };
                db.RecipeInventoryPostings.Add(posting);
            }
            posting.SapPostingId = sap.Id;
            posting.RequestJson = requestJson;
            posting.IntegrationRouteId = resolved.RouteId;
            posting.IntegrationConfigurationId = resolved.Configuration.Id;
            posting.Status = RecipeTransactionStatus.POSTING;
        }
        await db.SaveChangesAsync(cancellationToken);

        var result = await integrations.PostJsonOnceAsync(resolved.Configuration, requestJson, cancellationToken);
        sap.HttpStatus = result.HttpStatus;
        sap.ResponseJson = result.ResponseJson;
        sap.MaterialDocument = result.MaterialDocument;
        sap.DocumentYear = result.DocumentYear;
        sap.ErrorCode = result.ErrorCode;
        sap.ErrorMessage = result.ErrorMessage is null ? null : RecipeSapUpdateStock.Friendly(result.ErrorCode, result.ErrorMessage);
        sap.PostedAt = DateTime.UtcNow;
        foreach (var transaction in groupTxns)
        {
            var posting = await db.RecipeInventoryPostings.SingleAsync(item => item.TransactionId == transaction.Id, cancellationToken);
            posting.HttpStatus = result.HttpStatus;
            posting.ResponseJson = result.ResponseJson;
            posting.MaterialDocument = result.MaterialDocument;
            posting.DocumentYear = result.DocumentYear;
            posting.ErrorCode = result.ErrorCode;
            posting.ErrorMessage = sap.ErrorMessage;
            posting.CompletedAt = DateTime.UtcNow;
            posting.Status = result.Unknown ? RecipeTransactionStatus.POSTING_UNKNOWN : result.Success ? RecipeTransactionStatus.POSTED : RecipeTransactionStatus.FAILED;
        }
        if (result.Unknown)
        {
            sap.Status = RecipeTransactionStatus.POSTING_UNKNOWN;
            foreach (var transaction in groupTxns) MarkUnknown(transaction, sap);
            return;
        }
        if (!result.Success)
        {
            sap.Status = RecipeTransactionStatus.FAILED;
            foreach (var transaction in groupTxns) MarkFailed(transaction, sap);
            return;
        }
        sap.Status = RecipeTransactionStatus.POSTED;
        foreach (var transaction in groupTxns) MarkPosted(transaction, sap);
    }

    private void MarkPosted(RecipeConsumptionTransaction transaction, RecipeSapPosting sap)
    {
        transaction.Status = RecipeTransactionStatus.POSTED;
        transaction.ProcessedAt = DateTime.UtcNow;
        transaction.ExternalReference = sap.MaterialDocument;
        transaction.FailureCode = transaction.FailureMessage = transaction.FailedStep = null;
        RecipeExplosionService.QueueEvent(db, transaction.Id, "SAP_POSTING", "POSTED",
            string.IsNullOrWhiteSpace(sap.MaterialDocument) ? "SAP accepted the goods movement." : $"Material document {sap.MaterialDocument}");
    }

    private void MarkUnknown(RecipeConsumptionTransaction transaction, RecipeSapPosting sap)
    {
        transaction.Status = RecipeTransactionStatus.POSTING_UNKNOWN;
        transaction.FailureCode = "POSTING_UNKNOWN";
        transaction.FailureMessage = RecipeSapUpdateStock.Friendly("POSTING_UNKNOWN", sap.ErrorMessage);
        transaction.FailedStep = "S4_RESPONSE";
        transaction.FailureAt = DateTime.UtcNow;
        RecipeExplosionService.QueueEvent(db, transaction.Id, "S4_RESPONSE", "POSTING_UNKNOWN", transaction.FailureMessage);
    }

    private void MarkFailed(RecipeConsumptionTransaction transaction, RecipeSapPosting sap)
    {
        transaction.Status = RecipeTransactionStatus.FAILED;
        transaction.FailureCode = sap.ErrorCode ?? "SAP_POST_FAILED";
        transaction.FailureMessage = RecipeSapUpdateStock.Friendly("SAP_POST_FAILED", sap.ErrorMessage);
        transaction.FailedStep = "S4_POSTING";
        transaction.FailureAt = DateTime.UtcNow;
        RecipeExplosionService.QueueEvent(db, transaction.Id, "S4_POSTING", "FAILED", transaction.FailureMessage);
    }

    private RecipeManagementException Fail(RecipeConsumptionTransaction transaction, string code, string message, string step)
    {
        transaction.Status = RecipeTransactionStatus.FAILED;
        transaction.FailureCode = code;
        transaction.FailureMessage = message;
        transaction.FailedStep = step;
        transaction.FailureAt = DateTime.UtcNow;
        RecipeExplosionService.QueueEvent(db, transaction.Id, step, "FAILED", message);
        return new RecipeManagementException(code, message);
    }
}

public sealed class RecipePosIntakeService(
    SilaMeDbContext db,
    RecipeCatalogService recipes,
    RecipeExplosionService explosion,
    RecipeInventoryPostingService posting)
{
    public async Task<RecipeTransactionDetail> ReceiveAsync(
        Guid organizationId, PosSaleIntakeRequest request, bool reprocess, CancellationToken cancellationToken,
        Guid? uploadBatchId = null, bool postExternal = true)
    {
        var source = await db.PosSources.SingleOrDefaultAsync(item => item.Id == request.PosSourceId && item.OrganizationId == organizationId, cancellationToken)
            ?? throw new RecipeManagementException("POS_SOURCE_NOT_FOUND", "The POS integration was not found.", 404);
        var lineNumber = request.SourceLineNumber ?? 1;
        var existingQuery = db.RecipeConsumptionTransactions
            .Include(item => item.Lines)
            .Where(item => item.OrganizationId == organizationId && item.PosSourceId == source.Id &&
                item.SourceTransactionId == request.SourceTransactionId.Trim() && item.SourceLineNumber == lineNumber);
        var existing = request.BusinessDate is { } date
            ? await existingQuery.SingleOrDefaultAsync(item => item.BusinessDate == date, cancellationToken)
            : await existingQuery.SingleOrDefaultAsync(cancellationToken);
        if (existing is not null && existing.Status == RecipeTransactionStatus.POSTED)
            return await DetailAsync(organizationId, existing.Id, cancellationToken);
        if (existing is not null && existing.Status == RecipeTransactionStatus.POSTING_UNKNOWN && !reprocess)
            throw new RecipeManagementException("POSTING_UNKNOWN", RecipeSapUpdateStock.Friendly("POSTING_UNKNOWN", null));
        if (existing is not null && !reprocess && existing.Status is not RecipeTransactionStatus.FAILED)
            return await DetailAsync(organizationId, existing.Id, cancellationToken);

        var transaction = existing ?? new RecipeConsumptionTransaction
        {
            Id = Guid.NewGuid(), OrganizationId = organizationId, PosSourceId = source.Id, SourceSystem = source.PosSystem,
            SourceTransactionId = request.SourceTransactionId.Trim(), SourceLineNumber = lineNumber,
            PosOutletCode = request.PosOutletCode.Trim(), PosItemCode = request.PosItemCode.Trim(), CreatedAt = DateTime.UtcNow,
        };
        if (existing is null) db.RecipeConsumptionTransactions.Add(transaction);
        transaction.BusinessDate = request.BusinessDate;
        transaction.TransactionAt = request.TransactionAt ?? DateTime.UtcNow;
        transaction.PosOutletCode = request.PosOutletCode.Trim();
        transaction.PosItemCode = request.PosItemCode.Trim();
        transaction.PosItemDescription = request.PosItemDescription?.Trim();
        transaction.QuantitySold = request.QuantitySold;
        transaction.Amount = request.Amount;
        transaction.UploadBatchId = uploadBatchId ?? transaction.UploadBatchId;
        transaction.RawReference = RecipePosSales.PackRaw(request.RawReference, null, request.SaleUom, request.Currency, request.PosOutletCode);
        transaction.Status = reprocess ? RecipeTransactionStatus.REPROCESSING : RecipeTransactionStatus.RECEIVED;
        transaction.FailureCode = transaction.FailureMessage = transaction.FailedStep = null;
        transaction.FailureAt = null;
        await RecipeExplosionService.AddEventAsync(db, transaction, "POS_TRANSACTION_RECEIVED", transaction.Status.ToString(), $"{source.PosSystem} {transaction.SourceTransactionId}/{lineNumber}", cancellationToken);
        await db.SaveChangesAsync(cancellationToken);

        try
        {
            await ProcessAsync(transaction, cancellationToken, postExternal, applyPosItem: !reprocess);
            await PersistTrackerAsync(cancellationToken);
        }
        catch (RecipeManagementException)
        {
            await PersistTrackerAsync(cancellationToken);
            throw;
        }
        return await DetailAsync(organizationId, transaction.Id, cancellationToken);
    }

    private async Task PersistTrackerAsync(CancellationToken cancellationToken)
    {
        try
        {
            await db.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateConcurrencyException)
        {
            foreach (var entry in db.ChangeTracker.Entries<RecipeTransactionEvent>().Where(item => item.State == EntityState.Modified))
                entry.State = EntityState.Detached;
            foreach (var entry in db.ChangeTracker.Entries<RecipeConsumptionLine>().Where(item => item.State == EntityState.Modified))
                entry.State = EntityState.Detached;
            await db.SaveChangesAsync(cancellationToken);
        }
    }

    public async Task ProcessAsync(RecipeConsumptionTransaction transaction, CancellationToken cancellationToken, bool postExternal = true, bool applyPosItem = true)
    {
        transaction.Status = RecipeTransactionStatus.VALIDATING;
        var mapping = await MapOutletAsync(transaction, cancellationToken);
        var posCode = transaction.PosItemCode.Trim();
        var posName = transaction.PosItemDescription;
        OutletMenuItem? menuMatch = null;
        if (mapping is not null)
        {
            menuMatch = await db.OutletMenuItems.Include(item => item.Recipe)
                .Where(item => item.OutletMappingId == mapping.Id)
                .Where(item =>
                    (item.PosCode != null && item.PosCode == posCode) ||
                    (item.PosItem != null && (item.PosItem == posCode || (posName != null && item.PosItem == posName))) ||
                    (item.Recipe.PosCode != null && item.Recipe.PosCode == posCode) ||
                    (item.Recipe.PosItem != null && (item.Recipe.PosItem == posCode || (posName != null && item.Recipe.PosItem == posName))))
                .FirstOrDefaultAsync(cancellationToken);
        }
        var itemMap = menuMatch is null
            ? await db.PosItemRecipeMappings.Include(item => item.Recipe)
                .FirstOrDefaultAsync(item => item.PosSourceId == transaction.PosSourceId && item.PosItemCode == transaction.PosItemCode, cancellationToken)
            : null;
        var matchedRecipe = menuMatch?.Recipe ?? itemMap?.Recipe ?? await MatchRecipeAsync(transaction, posCode, cancellationToken);
        if (matchedRecipe is null)
            throw Fail(transaction, "UNMAPPED_POS_CODE", RecipeSapUpdateStock.Friendly("UNMAPPED_POS_CODE", null), "RECIPE_MAPPING");

        if (matchedRecipe is not null)
        {
            transaction.RecipeId = matchedRecipe.Id;
            var at = (transaction.BusinessDate ?? DateOnly.FromDateTime((transaction.TransactionAt ?? DateTime.UtcNow).ToUniversalTime()))
                .ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc);
            var version = await recipes.FindOperationalVersionAsync(matchedRecipe.Id, at, cancellationToken);
            if (version is null && matchedRecipe.ItemMode != RecipeItemMode.DIRECT)
                throw Fail(transaction, "NO_ACTIVE_RECIPE_VERSION", RecipeSapUpdateStock.Friendly("NO_ACTIVE_RECIPE_VERSION", null), "RECIPE_VERSION");
            if (version is not null)
            {
                if (version.Status is not RecipeStatus.ACTIVE and not RecipeStatus.APPROVED)
                    throw Fail(transaction, "RECIPE_NOT_APPROVED", "The mapped recipe version is not approved for operational consumption.", "RECIPE_VERSION");
                transaction.RecipeVersionId = version.Id;
                transaction.RecipeVersionNumber = version.VersionNumber;
            }
            transaction.Status = RecipeTransactionStatus.RECIPE_MATCHED;
            await RecipeExplosionService.AddEventAsync(db, transaction, "RECIPE_MAPPING", "RECIPE_MATCHED", matchedRecipe.RecipeCode, cancellationToken);
            if (version is not null)
                await RecipeExplosionService.AddEventAsync(db, transaction, "RECIPE_VERSION", "RECIPE_MATCHED", $"V{version.VersionNumber}", cancellationToken);
            var saleDate = transaction.BusinessDate ?? DateOnly.FromDateTime((transaction.TransactionAt ?? DateTime.UtcNow).ToUniversalTime());
            decimal? menuPrice = transaction.Amount is > 0 && transaction.QuantitySold > 0
                ? decimal.Round(transaction.Amount.Value / transaction.QuantitySold, 4, MidpointRounding.AwayFromZero)
                : null;
            if (applyPosItem)
                await recipes.ApplyPosItemAsync(transaction.OrganizationId, matchedRecipe.Id, transaction.PosItemCode, transaction.PosItemDescription, menuPrice, saleDate, cancellationToken);
            if (transaction.RecipeVersionId is not null)
                await explosion.ExplodeAsync(transaction, cancellationToken);
            var hasLines = await db.RecipeConsumptionLines.AnyAsync(item => item.TransactionId == transaction.Id, cancellationToken);
            if (!hasLines)
            {
                if (matchedRecipe.ItemMode != RecipeItemMode.DIRECT)
                    throw Fail(transaction, "NO_CONSUMPTION_LINES", "The recipe has no ingredients.", "RECIPE_EXPLOSION");
                var directMaterial = await MatchMaterialAsync(transaction.OrganizationId, posCode, cancellationToken)
                    ?? await MatchMaterialAsync(transaction.OrganizationId, matchedRecipe.RecipeCode, cancellationToken);
                if (directMaterial is null)
                    throw Fail(transaction, "NO_CONSUMPTION_LINES", "The DIRECT menu item is not linked to a Material ID.", "RECIPE_EXPLOSION");
                explosion.AddDirectLine(transaction, directMaterial, Math.Abs(transaction.QuantitySold), RecipePosSales.ReadUom(transaction.RawReference) ?? directMaterial.BaseUom);
                await RecipeExplosionService.AddEventAsync(db, transaction, "DIRECT_CONSUMPTION", "RECIPE_EXPLODED", directMaterial.MaterialCode, cancellationToken);
            }
        }

        await posting.TryPostAsync(transaction, cancellationToken, postExternal);
        transaction.ProcessedAt = DateTime.UtcNow;
    }

    public PosSalesImportPreview PreviewSales(Stream file, string fileName) => RecipePosSales.Preview(file, fileName);

    public byte[] SalesTemplate() => RecipePosSales.Template();

    public async Task<PosSalesImportPreview> PreviewSalesAsync(Guid organizationId, Stream file, string fileName, Guid? posSourceId, Guid? uploadedBy, CancellationToken cancellationToken)
    {
        var source = await EnsureSalesSourceAsync(organizationId, posSourceId, cancellationToken);
        using var memory = new MemoryStream();
        await file.CopyToAsync(memory, cancellationToken);
        memory.Position = 0;
        var parsed = RecipePosSales.Parse(memory);
        var classified = await ClassifyRowsAsync(organizationId, source.Id, parsed, cancellationToken);
        var dates = classified.Where(item => item.BusinessDate is not null).Select(item => item.BusinessDate!.Value).ToList();
        var batch = new PosSalesUploadBatch
        {
            Id = Guid.NewGuid(), OrganizationId = organizationId, PosSourceId = source.Id, FileName = fileName,
            UploadedByUserId = uploadedBy, UploadedAt = DateTime.UtcNow,
            BusinessDateFrom = dates.Count == 0 ? null : dates.Min(), BusinessDateTo = dates.Count == 0 ? null : dates.Max(),
            Rows = classified.Count, Status = "READY",
        };
        foreach (var row in classified)
        {
            db.PosSalesUploadLines.Add(new PosSalesUploadLine
            {
                Id = Guid.NewGuid(), BatchId = batch.Id, RowNumber = row.RowNumber, BusinessDate = row.BusinessDate,
                TransactionId = row.TransactionId, LineId = row.LineId, PosCode = row.PosCode, Qty = row.Qty, Uom = row.Uom,
                OutletId = row.OutletId, Currency = row.Currency, Status = row.Status, ErrorCode = row.ErrorCode, ErrorMessage = row.Message,
            });
        }
        Summarize(batch, classified);
        db.PosSalesUploadBatches.Add(batch);
        await db.SaveChangesAsync(cancellationToken);
        return ToPreview(batch.Id, fileName, classified);
    }

    public async Task<IReadOnlyList<PosSalesUploadListRow>> ListUploadsAsync(Guid organizationId, CancellationToken cancellationToken) =>
        (await db.PosSalesUploadBatches.AsNoTracking().Where(item => item.OrganizationId == organizationId)
            .OrderByDescending(item => item.UploadedAt).Take(100).ToListAsync(cancellationToken))
        .Select(ToUploadRow).ToList();

    public async Task<PosSalesUploadDetail> GetUploadAsync(Guid organizationId, Guid id, CancellationToken cancellationToken)
    {
        var batch = await db.PosSalesUploadBatches.AsNoTracking().Include(item => item.Lines)
            .SingleOrDefaultAsync(item => item.Id == id && item.OrganizationId == organizationId, cancellationToken)
            ?? throw new RecipeManagementException("UPLOAD_NOT_FOUND", "The sales upload was not found.", 404);
        var recipeCodes = await RecipeTitlesAsync(organizationId, batch.Lines.Select(item => item.ConsumptionTransactionId).Where(item => item is not null).Select(item => item!.Value).ToList(), cancellationToken);
        return new PosSalesUploadDetail(ToUploadRow(batch), batch.Lines.OrderBy(item => item.RowNumber).Select(item => new PosSalesUploadLineRow(
            item.Id, item.RowNumber, item.BusinessDate, item.TransactionId, item.LineId, item.PosCode,
            recipeCodes.GetValueOrDefault(item.ConsumptionTransactionId ?? Guid.Empty), item.Qty, item.Uom, item.OutletId, null, item.Status,
            item.ErrorMessage, item.ConsumptionTransactionId)).ToList());
    }

    public async Task<PosSalesImportResult> ProcessUploadAsync(Guid organizationId, Guid batchId, CancellationToken cancellationToken)
    {
        var batch = await db.PosSalesUploadBatches.Include(item => item.Lines)
            .SingleOrDefaultAsync(item => item.Id == batchId && item.OrganizationId == organizationId, cancellationToken)
            ?? throw new RecipeManagementException("UPLOAD_NOT_FOUND", "The sales upload was not found.", 404);
        batch.Status = "PROCESSING";
        var posted = 0;
        var failed = 0;
        var already = 0;
        var unknown = 0;
        var duplicates = 0;
        var failures = new List<string>();
        var processed = new List<RecipeConsumptionTransaction>();
        foreach (var line in batch.Lines.OrderBy(item => item.RowNumber))
        {
            if (line.Status == "DUPLICATE") { duplicates++; already++; continue; }
            if (line.Status != "READY") { failed++; continue; }
            try
            {
                var detail = await ReceiveAsync(organizationId, new PosSaleIntakeRequest(
                    batch.PosSourceId, line.TransactionId!, line.LineId, line.BusinessDate, null, line.OutletId!, line.PosCode!, null,
                    line.Qty!.Value, null, null, line.Uom, line.Currency), false, cancellationToken, batch.Id, false);
                line.ConsumptionTransactionId = detail.Header.Id;
                var txn = await db.RecipeConsumptionTransactions.SingleAsync(item => item.Id == detail.Header.Id, cancellationToken);
                if (detail.Header.Status == "POSTED") { already++; line.Status = "DUPLICATE"; line.ErrorCode = "DUPLICATE"; line.ErrorMessage = RecipeSapUpdateStock.Friendly("DUPLICATE", null); }
                else { processed.Add(txn); line.Status = "PROCESSED"; }
            }
            catch (RecipeManagementException exception)
            {
                failed++;
                line.Status = "FAILED";
                line.ErrorCode = exception.Code;
                line.ErrorMessage = RecipeSapUpdateStock.Friendly(exception.Code, exception.Message);
                failures.Add($"Row {line.RowNumber} {line.TransactionId}: {line.ErrorMessage}");
            }
        }

        if (processed.Count > 0)
        {
            try { await posting.PostUpdateStockAsync(processed, batch.Id, cancellationToken); }
            catch (RecipeManagementException exception) { failures.Add(RecipeSapUpdateStock.Friendly(exception.Code, exception.Message)); }
        }
        foreach (var txn in processed)
        {
            if (txn.Status == RecipeTransactionStatus.POSTED) posted++;
            else if (txn.Status == RecipeTransactionStatus.POSTING_UNKNOWN) unknown++;
            else failed++;
            var line = batch.Lines.FirstOrDefault(item => item.ConsumptionTransactionId == txn.Id);
            if (line is null) continue;
            line.Status = txn.Status.ToString();
            line.ErrorCode = txn.FailureCode;
            line.ErrorMessage = txn.FailureMessage;
        }
        batch.Processed = posted;
        batch.Failed = failed;
        batch.PostingUnknown = unknown;
        batch.Duplicates = duplicates;
        batch.Status = unknown > 0 ? "POSTING_UNKNOWN" : failed > 0 && posted == 0 ? "FAILED" : failed > 0 ? "PARTIAL" : "POSTED";
        await db.SaveChangesAsync(cancellationToken);
        return new PosSalesImportResult(batch.Rows, posted, failed, already, failures.Take(40).ToList(), batch.Id, duplicates, unknown);
    }

    public async Task<PosSalesImportResult> ImportSalesAsync(Guid organizationId, Stream file, Guid? posSourceId, Guid? uploadedBy, CancellationToken cancellationToken, string fileName = "upload.xlsx")
    {
        var preview = await PreviewSalesAsync(organizationId, file, fileName, posSourceId, uploadedBy, cancellationToken);
        return await ProcessUploadAsync(organizationId, preview.BatchId!.Value, cancellationToken);
    }

    private async Task<IReadOnlyList<PosSalesImportRow>> ClassifyRowsAsync(Guid organizationId, Guid posSourceId, IReadOnlyList<PosSalesImportRow> rows, CancellationToken cancellationToken)
    {
        var locations = await db.InventoryLocations.Include(item => item.PropertyLocation).Where(item => item.OrganizationId == organizationId).ToListAsync(cancellationToken);
        var mappings = await db.PosOutletMappings.Where(item => item.PosSourceId == posSourceId).ToListAsync(cancellationToken);
        var itemMaps = await db.PosItemRecipeMappings.Include(item => item.Recipe).Where(item => item.PosSourceId == posSourceId).ToListAsync(cancellationToken);
        var menuItems = await db.OutletMenuItems.Include(item => item.Recipe).Include(item => item.Outlet).Where(item => item.OrganizationId == organizationId).ToListAsync(cancellationToken);
        var recipesForOrg = await db.Recipes.AsNoTracking().Where(item => item.OrganizationId == organizationId).ToListAsync(cancellationToken);
        var existing = await db.RecipeConsumptionTransactions.AsNoTracking()
            .Where(item => item.OrganizationId == organizationId && item.PosSourceId == posSourceId)
            .Select(item => new { item.BusinessDate, item.SourceTransactionId, item.SourceLineNumber, item.Status })
            .ToListAsync(cancellationToken);
        var posted = existing
            .Where(item => item.Status is RecipeTransactionStatus.POSTED or RecipeTransactionStatus.POSTING_UNKNOWN or RecipeTransactionStatus.POSTING or RecipeTransactionStatus.READY_TO_POST)
            .Select(item => (item.BusinessDate, item.SourceTransactionId, item.SourceLineNumber))
            .ToHashSet();
        var result = new List<PosSalesImportRow>();
        foreach (var row in rows)
        {
            if (!row.IsValid)
            {
                result.Add(row with { Status = "INVALID", ErrorCode = "INVALID", Message = string.Join("; ", row.Errors) });
                continue;
            }
            if (posted.Contains((row.BusinessDate, row.TransactionId ?? string.Empty, row.LineId ?? 0)))
            {
                result.Add(row with { IsValid = false, Status = "DUPLICATE", ErrorCode = "DUPLICATE", Message = RecipeSapUpdateStock.Friendly("DUPLICATE", null) });
                continue;
            }
            var wanted = RecipePosSales.NormalizeOutlet(row.OutletId);
            var mapping = mappings.FirstOrDefault(item => RecipePosSales.NormalizeOutlet(item.PosOutletCode) == wanted || RecipePosSales.NormalizeOutlet(item.OutletCode) == wanted);
            var location = RecipePosSales.FindStockingLocation(locations, row.OutletId, mapping?.OutletCode);
            if (location is null)
            {
                result.Add(row with { IsValid = false, Status = "INVALID_OUTLET", ErrorCode = "OUTLET_MAPPING_MISSING", Message = RecipeSapUpdateStock.Friendly("OUTLET_MAPPING_MISSING", null) });
                continue;
            }
            if (location.Status != StatusKind.ACTIVE || !location.ConsumptionEnabled)
            {
                var code = location.Status != StatusKind.ACTIVE ? "OUTLET_INACTIVE" : "CONSUMPTION_DISABLED";
                result.Add(row with { IsValid = false, Status = "INVALID_OUTLET", ErrorCode = code, Message = RecipeSapUpdateStock.Friendly(code, null) });
                continue;
            }
            var pos = row.PosCode!.Trim();
            var mapped = itemMaps.FirstOrDefault(item => string.Equals(item.PosItemCode, pos, StringComparison.OrdinalIgnoreCase))?.Recipe
                ?? menuItems.FirstOrDefault(item => mapping is not null && item.OutletMappingId == mapping.Id &&
                    (string.Equals(item.PosCode, pos, StringComparison.OrdinalIgnoreCase) || string.Equals(item.PosItem, pos, StringComparison.OrdinalIgnoreCase)))?.Recipe
                ?? recipesForOrg.FirstOrDefault(item => string.Equals(item.PosCode, pos, StringComparison.OrdinalIgnoreCase) || string.Equals(item.PosItem, pos, StringComparison.OrdinalIgnoreCase));
            if (mapped is null)
            {
                result.Add(row with { IsValid = false, Status = "UNMAPPED_POS_CODE", ErrorCode = "UNMAPPED_POS_CODE", Message = RecipeSapUpdateStock.Friendly("UNMAPPED_POS_CODE", null) });
                continue;
            }
            if (string.IsNullOrWhiteSpace(row.Uom))
            {
                result.Add(row with { IsValid = false, Status = "INVALID_UOM", ErrorCode = "INVALID_UOM", Message = "UOM is required." });
                continue;
            }
            var at = row.BusinessDate!.Value.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc);
            if (mapped.ItemMode != RecipeItemMode.DIRECT)
            {
                var version = await recipes.FindOperationalVersionAsync(mapped.Id, at, cancellationToken);
                if (version is null)
                {
                    result.Add(row with { IsValid = false, Status = "RECIPE_NOT_READY", ErrorCode = "NO_ACTIVE_RECIPE_VERSION", Message = RecipeSapUpdateStock.Friendly("NO_ACTIVE_RECIPE_VERSION", null) });
                    continue;
                }
            }
            result.Add(row with { Status = "READY", Message = null });
        }
        return result;
    }

    private static void Summarize(PosSalesUploadBatch batch, IReadOnlyList<PosSalesImportRow> rows)
    {
        batch.Invalid = rows.Count(item => item.Status == "INVALID");
        batch.Duplicates = rows.Count(item => item.Status == "DUPLICATE");
        batch.UnmappedPosCodes = rows.Count(item => item.Status == "UNMAPPED_POS_CODE");
        batch.InvalidOutlets = rows.Count(item => item.Status == "INVALID_OUTLET");
        batch.InvalidUom = rows.Count(item => item.Status == "INVALID_UOM");
        batch.RecipeNotReady = rows.Count(item => item.Status == "RECIPE_NOT_READY");
        batch.ReadyToProcess = rows.Count(item => item.Status == "READY");
        batch.Valid = batch.ReadyToProcess;
    }

    private static PosSalesImportPreview ToPreview(Guid batchId, string fileName, IReadOnlyList<PosSalesImportRow> rows) =>
        new(fileName, rows.Count, rows.Count(item => item.Status == "READY"), rows.Count(item => item.Status != "READY"), rows, batchId,
            rows.Count(item => item.Status == "DUPLICATE"), rows.Count(item => item.Status == "UNMAPPED_POS_CODE"),
            rows.Count(item => item.Status == "INVALID_OUTLET"), rows.Count(item => item.Status == "INVALID_UOM"),
            rows.Count(item => item.Status == "RECIPE_NOT_READY"), rows.Count(item => item.Status == "READY"));

    private static PosSalesUploadListRow ToUploadRow(PosSalesUploadBatch item) =>
        new(item.Id, item.FileName,
            item.BusinessDateFrom is null ? null : item.BusinessDateFrom == item.BusinessDateTo ? item.BusinessDateFrom.Value.ToString("yyyy-MM-dd") : $"{item.BusinessDateFrom:yyyy-MM-dd} – {item.BusinessDateTo:yyyy-MM-dd}",
            item.UploadedByUserId, item.UploadedAt, item.Rows, item.Valid, item.Processed, item.Duplicates, item.Failed, item.PostingUnknown, item.Status);

    private async Task<Dictionary<Guid, string?>> RecipeTitlesAsync(Guid organizationId, IReadOnlyList<Guid> transactionIds, CancellationToken cancellationToken)
    {
        if (transactionIds.Count == 0) return [];
        var rows = await db.RecipeConsumptionTransactions.AsNoTracking()
            .Where(item => item.OrganizationId == organizationId && transactionIds.Contains(item.Id))
            .Select(item => new { item.Id, item.RecipeId }).ToListAsync(cancellationToken);
        var recipeIds = rows.Where(item => item.RecipeId is not null).Select(item => item.RecipeId!.Value).Distinct().ToList();
        var names = await db.Recipes.AsNoTracking().Where(item => recipeIds.Contains(item.Id)).ToDictionaryAsync(item => item.Id, item => item.Name, cancellationToken);
        return rows.ToDictionary(item => item.Id, item => item.RecipeId is { } id && names.TryGetValue(id, out var name) ? name : null);
    }

    private async Task<PosSource> EnsureSalesSourceAsync(Guid organizationId, Guid? posSourceId, CancellationToken cancellationToken)
    {
        if (posSourceId is { } id)
            return await db.PosSources.SingleOrDefaultAsync(item => item.Id == id && item.OrganizationId == organizationId, cancellationToken)
                ?? throw new RecipeManagementException("POS_SOURCE_NOT_FOUND", "The POS integration was not found.", 404);
        var existing = await db.PosSources.FirstOrDefaultAsync(item => item.OrganizationId == organizationId, cancellationToken);
        if (existing is not null) return existing;
        var source = new PosSource
        {
            Id = Guid.NewGuid(), OrganizationId = organizationId, Name = "POS Sales Excel", PosSystem = "EXCEL",
            IntegrationKind = PosIntegrationKind.API, Status = PosSourceStatus.ACTIVE, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow,
        };
        db.PosSources.Add(source);
        await db.SaveChangesAsync(cancellationToken);
        return source;
    }

    private async Task<PosOutletMapping?> MapOutletAsync(RecipeConsumptionTransaction transaction, CancellationToken cancellationToken)
    {
        var wanted = RecipePosSales.NormalizeOutlet(transaction.PosOutletCode);
        var mappings = await db.PosOutletMappings.Where(item => item.PosSourceId == transaction.PosSourceId).ToListAsync(cancellationToken);
        var mapping = mappings.FirstOrDefault(item =>
            RecipePosSales.NormalizeOutlet(item.PosOutletCode) == wanted || RecipePosSales.NormalizeOutlet(item.OutletCode) == wanted);
        var locations = await db.InventoryLocations.Include(item => item.PropertyLocation)
            .Where(item => item.OrganizationId == transaction.OrganizationId)
            .ToListAsync(cancellationToken);
        var location = RecipePosSales.FindStockingLocation(locations, transaction.PosOutletCode, mapping?.OutletCode);
        if (location is null)
            throw Fail(transaction, "OUTLET_MAPPING_MISSING", RecipeSapUpdateStock.Friendly("OUTLET_MAPPING_MISSING", null), "OUTLET_MAPPING");
        if (location.Status != StatusKind.ACTIVE)
            throw Fail(transaction, "OUTLET_INACTIVE", RecipeSapUpdateStock.Friendly("OUTLET_INACTIVE", null), "OUTLET_MAPPING");
        if (!location.ConsumptionEnabled)
            throw Fail(transaction, "CONSUMPTION_DISABLED", RecipeSapUpdateStock.Friendly("CONSUMPTION_DISABLED", null), "OUTLET_MAPPING");
        transaction.OutletCode = location.LocationCode;
        var (plant, storage) = RecipeSapUpdateStock.ResolvePlantStorage(mapping, location, null, null);
        transaction.PlantCode = plant;
        transaction.StorageLocationCode = storage;
        transaction.CompanyCode = mapping?.CompanyCode ?? location.CompanyCode;
        transaction.PropertyCode = location.PropertyLocation?.LocationCode ?? mapping?.PropertyCode;
        if (string.IsNullOrWhiteSpace(transaction.CompanyCode) && !string.IsNullOrWhiteSpace(transaction.PropertyCode))
        {
            var property = await db.Properties.AsNoTracking().FirstOrDefaultAsync(item =>
                item.OrganizationId == transaction.OrganizationId && !item.IsDeleted && item.PropertyCode == transaction.PropertyCode, cancellationToken);
            transaction.CompanyCode = property?.CompanyCode;
        }
        transaction.RawReference = RecipePosSales.PackRaw(transaction.RawReference, location.Id, null, null, transaction.PosOutletCode);
        await RecipeExplosionService.AddEventAsync(db, transaction, "OUTLET_MAPPING", "VALIDATING",
            $"{RecipePosSales.LocationKindLabel(location.LocationType)} {location.LocationName} ({location.LocationCode})", cancellationToken);
        return mapping;
    }

    private async Task<Recipe?> MatchRecipeAsync(RecipeConsumptionTransaction transaction, string posCode, CancellationToken cancellationToken)
    {
        var recipesForOrg = await db.Recipes.AsNoTracking()
            .Include(item => item.LocationAssignments).ThenInclude(item => item.Location)
            .Where(item => item.OrganizationId == transaction.OrganizationId)
            .ToListAsync(cancellationToken);
        var candidates = recipesForOrg.Where(item =>
            string.Equals(item.PosCode, posCode, StringComparison.OrdinalIgnoreCase) ||
            string.Equals(item.PosItem, posCode, StringComparison.OrdinalIgnoreCase)).ToList();
        if (candidates.Count == 0) return null;
        var outlet = RecipePosSales.NormalizeOutlet(transaction.OutletCode ?? transaction.PosOutletCode);
        var assigned = candidates.Where(item => item.LocationAssignments.Any(row => RecipePosSales.NormalizeOutlet(row.Location.Code) == outlet)).ToList();
        return assigned.FirstOrDefault() ?? candidates[0];
    }

    private async Task<Material?> MatchMaterialAsync(Guid organizationId, string code, CancellationToken cancellationToken) =>
        await db.Materials.FirstOrDefaultAsync(item => item.OrganizationId == organizationId && item.MaterialCode == code, cancellationToken)
        ?? (await db.Materials.Where(item => item.OrganizationId == organizationId).ToListAsync(cancellationToken))
            .FirstOrDefault(item => string.Equals(item.MaterialCode, code, StringComparison.OrdinalIgnoreCase));

    public async Task<IReadOnlyList<RecipeTransactionListRow>> ListAsync(
        Guid organizationId, string? status, DateOnly? businessDate, string? outlet, string? posCode, string? material, string? transactionId, CancellationToken cancellationToken)
    {
        var rows = db.RecipeConsumptionTransactions.AsNoTracking().Include(item => item.Lines)
            .Where(item => item.OrganizationId == organizationId);
        if (Enum.TryParse<RecipeTransactionStatus>(status, true, out var parsed))
            rows = rows.Where(item => item.Status == parsed);
        if (businessDate is { } date) rows = rows.Where(item => item.BusinessDate == date);
        if (!string.IsNullOrWhiteSpace(outlet))
            rows = rows.Where(item => item.PosOutletCode == outlet || item.OutletCode == outlet);
        if (!string.IsNullOrWhiteSpace(posCode)) rows = rows.Where(item => item.PosItemCode == posCode);
        if (!string.IsNullOrWhiteSpace(transactionId)) rows = rows.Where(item => item.SourceTransactionId == transactionId);
        if (!string.IsNullOrWhiteSpace(material))
            rows = rows.Where(item => item.Lines.Any(line => line.MaterialCode == material));
        var list = await rows.OrderByDescending(item => item.CreatedAt).Take(200).ToListAsync(cancellationToken);
        var recipesById = await db.Recipes.AsNoTracking().Where(item => item.OrganizationId == organizationId)
            .ToDictionaryAsync(item => item.Id, cancellationToken);
        var locations = await db.InventoryLocations.AsNoTracking().Include(item => item.PropertyLocation)
            .Where(item => item.OrganizationId == organizationId).ToListAsync(cancellationToken);
        var ids = list.Select(item => item.Id).ToList();
        var postings = await db.RecipeInventoryPostings.AsNoTracking()
            .Where(item => ids.Contains(item.TransactionId))
            .ToDictionaryAsync(item => item.TransactionId, cancellationToken);
        return list.Select(item => ToListRow(item,
            item.RecipeId is { } recipeId && recipesById.TryGetValue(recipeId, out var recipe) ? recipe.RecipeCode : null,
            RecipePosSales.LocationForTransaction(locations, item),
            postings.GetValueOrDefault(item.Id))).ToList();
    }

    public async Task<RecipeTransactionDetail> DetailAsync(Guid organizationId, Guid id, CancellationToken cancellationToken)
    {
        var item = await db.RecipeConsumptionTransactions.AsNoTracking().Include(item => item.Lines).Include(item => item.Events)
            .SingleOrDefaultAsync(row => row.Id == id && row.OrganizationId == organizationId, cancellationToken)
            ?? throw new RecipeManagementException("TRANSACTION_NOT_FOUND", "The tracker entry was not found.", 404);
        var recipeCode = item.RecipeId is null ? null : await db.Recipes.AsNoTracking().Where(row => row.Id == item.RecipeId).Select(row => row.RecipeCode).FirstOrDefaultAsync(cancellationToken);
        var locations = await db.InventoryLocations.AsNoTracking().Include(row => row.PropertyLocation)
            .Where(row => row.OrganizationId == organizationId).ToListAsync(cancellationToken);
        var posting = await db.RecipeInventoryPostings.AsNoTracking().SingleOrDefaultAsync(row => row.TransactionId == item.Id, cancellationToken);
        return new RecipeTransactionDetail(
            ToListRow(item, recipeCode, RecipePosSales.LocationForTransaction(locations, item), posting),
            item.Lines.OrderBy(line => line.Sequence).Select(line => new RecipeConsumptionLineRow(line.Id, line.MaterialCode, line.Description, line.ConsumedQuantity, line.Uom, line.UnitCost)).ToList(),
            item.Events.OrderBy(evt => evt.CreatedAt).Select(evt => new RecipeTransactionEventRow(evt.Id, evt.Step, evt.Status, evt.Detail, evt.CreatedAt)).ToList(),
            item.FailureCode, item.FailureMessage, item.FailedStep, item.FailureAt,
            posting?.RequestJson, posting?.ResponseJson, posting?.HttpStatus);
    }

    public async Task<RecipeTransactionDetail> ReprocessAsync(Guid organizationId, Guid id, CancellationToken cancellationToken)
    {
        var item = await db.RecipeConsumptionTransactions.AsNoTracking()
            .SingleOrDefaultAsync(row => row.Id == id && row.OrganizationId == organizationId, cancellationToken)
            ?? throw new RecipeManagementException("TRANSACTION_NOT_FOUND", "The tracker entry was not found.", 404);
        if (item.Status is RecipeTransactionStatus.POSTED)
            throw new RecipeManagementException("ALREADY_POSTED", "Posted transactions cannot be reprocessed.");
        if (item.Status is RecipeTransactionStatus.POSTING)
            throw new RecipeManagementException("POSTING_IN_PROGRESS", "This transaction is currently posting to ERP.");
        if (!RecipeSapUpdateStock.CanReprocess(item.Status))
            throw new RecipeManagementException("CANNOT_REPROCESS", "Only failed or not-updated transactions can be reprocessed.");
        if (await db.InventoryStockTransactions.AnyAsync(row => row.ReferenceId == item.Id && row.ReferenceType == "POS_SALE", cancellationToken)
            && item.Status is RecipeTransactionStatus.FAILED or RecipeTransactionStatus.READY_TO_POST or RecipeTransactionStatus.POSTING_UNKNOWN or RecipeTransactionStatus.REPROCESSING)
        {
            var tracked = await db.RecipeConsumptionTransactions.SingleAsync(row => row.Id == item.Id, cancellationToken);
            tracked.Status = RecipeTransactionStatus.READY_TO_POST;
            tracked.FailureCode = tracked.FailureMessage = tracked.FailedStep = null;
            await posting.PostUpdateStockAsync([tracked], tracked.UploadBatchId, cancellationToken);
            await db.SaveChangesAsync(cancellationToken);
            return await DetailAsync(organizationId, tracked.Id, cancellationToken);
        }
        return await ReceiveAsync(organizationId, new PosSaleIntakeRequest(item.PosSourceId, item.SourceTransactionId, item.SourceLineNumber, item.BusinessDate, item.TransactionAt, item.PosOutletCode, item.PosItemCode, item.PosItemDescription, item.QuantitySold, item.Amount, item.RawReference), true, cancellationToken, item.UploadBatchId);
    }

    private static RecipeTransactionListRow ToListRow(
        RecipeConsumptionTransaction item, string? recipeCode, InventoryLocation? location, RecipeInventoryPosting? posting)
    {
        var steps = RecipeSapUpdateStock.TrackerSteps(item, posting, location);
        return new(
        item.Id, item.SourceSystem, item.SourceTransactionId, item.BusinessDate,
        location?.PropertyLocation?.LocationCode ?? item.PropertyCode,
        location?.LocationName ?? item.OutletCode ?? item.PosOutletCode,
        recipeCode, item.RecipeVersionNumber, item.QuantitySold, item.Lines.Count, item.CompanyCode ?? location?.CompanyCode,
        item.PlantCode, item.StorageLocationCode,
        item.IntegrationSystem, item.Status.ToString(), item.ExternalReference, item.CreatedAt, item.ProcessedAt,
        RecipeSapUpdateStock.CanReprocess(item.Status),
        location is null ? null : RecipePosSales.LocationKindLabel(location.LocationType), location?.LocationName, location?.LocationCode,
        item.PosItemCode, item.SourceLineNumber, RecipeSapUpdateStock.MovementType(item.QuantitySold < 0 ? 1 : -1),
        posting?.MaterialDocument ?? item.ExternalReference,
        steps.Step1Status, steps.Step1Message, steps.Step2Status, steps.Step2Message,
        posting?.HttpStatus, posting?.ResponseJson, item.FailureMessage);
    }

    private RecipeManagementException Fail(RecipeConsumptionTransaction transaction, string code, string message, string step)
    {
        transaction.Status = RecipeTransactionStatus.FAILED;
        transaction.FailureCode = code;
        transaction.FailureMessage = message;
        transaction.FailedStep = step;
        transaction.FailureAt = DateTime.UtcNow;
        RecipeExplosionService.QueueEvent(db, transaction.Id, step, "FAILED", message);
        return new RecipeManagementException(code, message);
    }
}
