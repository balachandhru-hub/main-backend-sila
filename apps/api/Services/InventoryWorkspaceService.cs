using Microsoft.EntityFrameworkCore;
using SilaMe.Api.Auth;
using SilaMe.Api.Data;
using SilaMe.Api.DTOs;
using SilaMe.Api.Models;

namespace SilaMe.Api.Services;

public sealed class InventoryWorkspaceService(SilaMeDbContext db, MaterialMasterService materials)
{
    public async Task<InventoryDashboardResponse> DashboardAsync(Guid organizationId, DateTime? businessDate, Guid? propertyId, string? locationType, Guid? locationId, string? materialGroup, CancellationToken cancellationToken)
    {
        var locations = await db.InventoryLocations.AsNoTracking().Where(item => item.OrganizationId == organizationId).ToListAsync(cancellationToken);
        var names = locations.ToDictionary(item => item.Id);
        var locationQuery = locations.AsEnumerable();
        if (propertyId is Guid property) locationQuery = locationQuery.Where(item => item.Id == property || item.PropertyLocationId == property);
        if (Enum.TryParse<InventoryLocationType>(locationType, true, out var type)) locationQuery = locationQuery.Where(item => item.LocationType == type);
        if (locationId is Guid loc) locationQuery = locationQuery.Where(item => item.Id == loc);
        var scoped = locationQuery.ToList();
        var locationIds = scoped.Select(item => item.Id).ToHashSet();
        var balances = await db.InventoryBalances.AsNoTracking()
            .Where(item => item.OrganizationId == organizationId && locationIds.Contains(item.InventoryLocationId))
            .ToListAsync(cancellationToken);
        HashSet<Guid>? groupIds = null;
        if (!string.IsNullOrWhiteSpace(materialGroup))
        {
            groupIds = (await db.Materials.AsNoTracking()
                .Where(item => item.OrganizationId == organizationId && item.MaterialGroup != null && item.MaterialGroup.ToUpper().Contains(materialGroup.Trim().ToUpper()))
                .Select(item => item.Id).ToListAsync(cancellationToken)).ToHashSet();
            balances = balances.Where(item => groupIds.Contains(item.MaterialId)).ToList();
        }
        var assignments = await db.MaterialLocations.AsNoTracking()
            .Include(item => item.Material)
            .Include(item => item.InventoryLocation)
            .Include(item => item.PreferredSourceLocation)
            .Where(item => item.OrganizationId == organizationId && item.Active && item.StockingStatus == MaterialStockingStatus.ACTIVE && locationIds.Contains(item.InventoryLocationId))
            .ToListAsync(cancellationToken);
        if (groupIds is not null) assignments = assignments.Where(item => groupIds.Contains(item.MaterialId)).ToList();
        var stocked = assignments.Where(item => MaterialNormalized.CanHoldStock(item.Material)).ToList();
        var balanceMap = balances.GroupBy(item => (item.MaterialId, item.InventoryLocationId)).ToDictionary(item => item.Key, item => item.First());
        var sourceIds = stocked.Where(item => item.PreferredSourceLocationId is not null).Select(item => item.PreferredSourceLocationId!.Value).Distinct().ToList();
        var sourceBalances = sourceIds.Count == 0 ? new List<InventoryBalance>() : await db.InventoryBalances.AsNoTracking()
            .Where(item => item.OrganizationId == organizationId && sourceIds.Contains(item.InventoryLocationId))
            .ToListAsync(cancellationToken);
        var sourceMap = sourceBalances.GroupBy(item => (item.MaterialId, item.InventoryLocationId)).ToDictionary(item => item.Key, item => item.First());
        var sourceAssignments = sourceIds.Count == 0 ? new List<MaterialLocation>() : await db.MaterialLocations.AsNoTracking()
            .Where(item => item.OrganizationId == organizationId && sourceIds.Contains(item.InventoryLocationId) && item.Active)
            .ToListAsync(cancellationToken);
        var sourcePolicy = sourceAssignments.GroupBy(item => (item.MaterialId, item.InventoryLocationId)).ToDictionary(item => item.Key, item => item.First());

        int healthy = 0, low = 0, stockouts = 0, excess = 0;
        var replenishment = new List<ReplenishmentRecommendation>();
        var actions = new List<InventoryActionItem>();
        foreach (var row in stocked)
        {
            balanceMap.TryGetValue((row.MaterialId, row.InventoryLocationId), out var balance);
            var available = balance?.AvailableQty ?? 0;
            var status = InventoryControlMath.Classify(available, row.ReorderPoint, row.MaximumStock);
            if (status == InventoryControlMath.Healthy) healthy++;
            else if (status == InventoryControlMath.Low) low++;
            else if (status == InventoryControlMath.Out) stockouts++;
            else if (status == InventoryControlMath.Excess) excess++;
            var need = InventoryControlMath.RecommendedQty(available, row.ReorderPoint, row.ParLevel);
            if (need is not decimal qty || qty <= 0) continue;
            decimal? sourceAvail = null;
            decimal transferable = 0;
            string? sourceName = null;
            Guid? sourceId = row.PreferredSourceLocationId;
            if (sourceId is Guid sid)
            {
                sourceMap.TryGetValue((row.MaterialId, sid), out var sourceBalance);
                sourcePolicy.TryGetValue((row.MaterialId, sid), out var sourceLoc);
                sourceAvail = sourceBalance?.AvailableQty ?? 0;
                transferable = InventoryControlMath.TransferableQty(sourceAvail.Value, sourceLoc?.SafetyStock, sourceLoc?.MinimumStock);
                sourceName = row.PreferredSourceLocation?.LocationName ?? names.GetValueOrDefault(sid)?.LocationName;
            }
            var capped = sourceId is null ? qty : Math.Min(qty, transferable);
            var unavailable = sourceId is null || transferable <= 0;
            replenishment.Add(new ReplenishmentRecommendation(
                row.MaterialId, row.InventoryLocationId, row.InventoryLocation.LocationName, row.Material.MaterialCode, row.Material.Description,
                row.Material.BaseUom, available, row.ReorderPoint, row.ParLevel, unavailable ? qty : capped, sourceId, sourceName, sourceAvail,
                unavailable ? "Internal Stock Unavailable" : "Ready", unavailable ? "CREATE_PR" : "CREATE_TRANSFER"));
            if (actions.Count < 6 && (status == InventoryControlMath.Out || status == InventoryControlMath.Low))
            {
                actions.Add(new InventoryActionItem(
                    status == InventoryControlMath.Out ? "HIGH" : "MEDIUM",
                    status == InventoryControlMath.Out ? "Stockout" : "Low Stock",
                    $"{row.InventoryLocation.LocationName} · {row.Material.Description}",
                    row.InventoryLocation.LocationName, row.Material.MaterialCode, row.Material.Description,
                    $"Available {available:0.####} {row.Material.BaseUom}",
                    unavailable ? "No suitable internal source. Create an internal purchase request." : $"Transfer {capped:0.####} {row.Material.BaseUom} from {sourceName}",
                    unavailable ? "CREATE_PR" : "CREATE_TRANSFER", unavailable ? null : "/inventory/transfers/new",
                    null, row.MaterialId, row.InventoryLocationId, null, available, row.ParLevel, sourceAvail, sourceName, row.Material.BaseUom));
            }
        }

        var configured = locations.Count > 0;
        var day = (businessDate ?? DateTime.UtcNow).Date;
        var next = day.AddDays(1);
        var txns = await db.InventoryStockTransactions.AsNoTracking()
            .Where(item => item.OrganizationId == organizationId && locationIds.Contains(item.InventoryLocationId) && item.BusinessDate >= day && item.BusinessDate < next)
            .ToListAsync(cancellationToken);
        var currencies = txns.Select(item => item.Currency).Where(item => !string.IsNullOrWhiteSpace(item)).Distinct(StringComparer.OrdinalIgnoreCase).ToList();
        var txnCurrency = currencies.Count == 1 ? currencies[0] : null;
        MovementBucket Bucket(string key, string label, InventoryTxnType[] types)
        {
            var rows = txns.Where(item => types.Contains(item.TransactionType)).ToList();
            decimal? value = txnCurrency is null ? null : rows.Sum(item => item.TransactionValue ?? 0);
            var note = rows.Count == 0 ? "No activity" : txnCurrency is null && rows.Exists(item => !string.IsNullOrWhiteSpace(item.Currency)) ? "Multiple currencies — value not summed" : null;
            return new MovementBucket(key, label, rows.Count, rows.Count == 0 ? 0 : value, note, true);
        }
        var movement = new InventoryMovementToday(
        [
            Bucket("RECEIVED", "Received", [InventoryTxnType.GOODS_RECEIPT, InventoryTxnType.OPENING_STOCK]),
            Bucket("CONSUMED", "Consumed", [InventoryTxnType.RECIPE_CONSUMPTION, InventoryTxnType.DIRECT_CONSUMPTION, InventoryTxnType.GOODS_ISSUE]),
            Bucket("TRANSFER_OUT", "Transferred Out", [InventoryTxnType.TRANSFER_OUT, InventoryTxnType.QUICK_TRANSFER_OUT]),
            Bucket("TRANSFER_IN", "Transferred In", [InventoryTxnType.TRANSFER_IN, InventoryTxnType.QUICK_TRANSFER_IN]),
            Bucket("WASTE", "Waste / Damage", [InventoryTxnType.WASTE, InventoryTxnType.DAMAGE, InventoryTxnType.BREAKAGE, InventoryTxnType.SPOILAGE, InventoryTxnType.EXPIRED]),
            Bucket("ADJUSTMENTS", "Adjustments", [InventoryTxnType.STOCK_COUNT_ADJUSTMENT, InventoryTxnType.MANUAL_ADJUSTMENT]),
        ], txnCurrency);

        var itos = await db.InternalTransferOrders.AsNoTracking().Include(item => item.FromLocation).Include(item => item.ToLocation).Include(item => item.Lines)
            .Where(item => item.OrganizationId == organizationId)
            .Where(item => locationIds.Contains(item.FromInventoryLocationId) || locationIds.Contains(item.ToInventoryLocationId))
            .ToListAsync(cancellationToken);
        TransferPipelineCount Stage(string key, string label, Func<InternalTransferOrder, bool> match) =>
            new(key, label, itos.Count(match), $"/inventory/transfers?status={key}");
        var pipeline = new TransferPipeline(
        [
            Stage("AWAITING_APPROVAL", "Awaiting Approval", item => item.Status is ItoStatus.SUBMITTED or ItoStatus.PENDING_APPROVAL),
            Stage("READY_TO_DISPATCH", "Ready to Dispatch", item => item.Status is ItoStatus.APPROVED or ItoStatus.READY_TO_DISPATCH),
            Stage("IN_TRANSIT", "In Transit", item => item.Status is ItoStatus.IN_TRANSIT),
            Stage("AWAITING_RECEIPT", "Awaiting Receipt", item => item.Status is ItoStatus.DISPATCHED),
            Stage("DISCREPANCIES", "Discrepancies", item => item.Status == ItoStatus.DISCREPANCY || item.Lines.Any(line => line.DispatchedQty != line.ReceivedQty && line.DispatchedQty > 0)),
        ]);
        foreach (var ito in itos.Where(item => item.Status == ItoStatus.DISCREPANCY || item.Lines.Any(line => line.DispatchedQty != line.ReceivedQty && line.DispatchedQty > 0)).Take(4))
        {
            var line = ito.Lines.FirstOrDefault(item => item.DispatchedQty != item.ReceivedQty) ?? ito.Lines.FirstOrDefault();
            var variance = line is null ? 0 : line.DispatchedQty - line.ReceivedQty;
            actions.Add(new InventoryActionItem("HIGH", "Transfer Discrepancy", $"{ito.ItoNumber} · {ito.FromLocation.LocationName} → {ito.ToLocation.LocationName}",
                ito.ToLocation.LocationName, null, null,
                line is null ? "Transfer variance" : $"Dispatched {line.DispatchedQty:0.####} {line.Uom} · Received {line.ReceivedQty:0.####} {line.Uom} · Difference {variance:0.####} {line.Uom}",
                "Investigate the transfer before closing the discrepancy.", "INVESTIGATE", $"/inventory/transfers/{ito.Id}",
                null, line?.MaterialId, ito.ToInventoryLocationId, ito.Id, null, null, null, null, line?.Uom));
        }

        var valueByLocation = balances.GroupBy(item => item.InventoryLocationId).ToDictionary(item => item.Key, item => item.Sum(row => row.InventoryValue));
        var locationValues = new List<LocationValueShare>();
        foreach (var location in scoped.Where(item => item.InventoryEnabled || valueByLocation.GetValueOrDefault(item.Id) != 0))
            locationValues.Add(new LocationValueShare(location.Id, location.LocationCode, location.LocationName, location.LocationType.ToString(), valueByLocation.GetValueOrDefault(location.Id), 0, false));
        foreach (var parent in scoped.Where(item => item.LocationType is InventoryLocationType.PROPERTY or InventoryLocationType.VENUE && !item.InventoryEnabled))
        {
            var childIds = locations.Where(item => item.ParentLocationId == parent.Id || item.PropertyLocationId == parent.Id).Select(item => item.Id).ToHashSet();
            var value = valueByLocation.Where(item => childIds.Contains(item.Key)).Sum(item => item.Value);
            if (value == 0) continue;
            locationValues.Add(new LocationValueShare(parent.Id, parent.LocationCode, parent.LocationName, parent.LocationType.ToString(), value, 0, true));
        }
        var totalValue = balances.Sum(item => item.InventoryValue);
        locationValues = locationValues.GroupBy(item => item.Id).Select(item => item.First()).OrderByDescending(item => item.Value).Take(8).ToList();
        locationValues = locationValues.Select(item => item with { SharePercent = totalValue == 0 ? 0 : Math.Round(item.Value / totalValue * 100m, 1) }).ToList();

        var consumeTypes = new[] { InventoryTxnType.RECIPE_CONSUMPTION, InventoryTxnType.DIRECT_CONSUMPTION, InventoryTxnType.GOODS_ISSUE };
        var consumeRows = txns.Where(item => consumeTypes.Contains(item.TransactionType)).ToList();
        var consumeIds = consumeRows.Select(item => item.MaterialId).Distinct().ToList();
        var consumeMaterials = consumeIds.Count == 0
            ? new Dictionary<Guid, Material>()
            : await db.Materials.AsNoTracking().Where(item => consumeIds.Contains(item.Id)).ToDictionaryAsync(item => item.Id, cancellationToken);
        var topConsumption = consumeRows.GroupBy(item => item.MaterialId).Select(group =>
        {
            consumeMaterials.TryGetValue(group.Key, out var material);
            var primary = group.GroupBy(item => item.InventoryLocationId).OrderByDescending(item => item.Sum(row => row.BaseQuantity)).First().Key;
            names.TryGetValue(primary, out var loc);
            var uoms = group.Select(item => item.BaseUom).Distinct(StringComparer.OrdinalIgnoreCase).ToList();
            return new ConsumptionHighlight(group.Key, material?.MaterialCode ?? "", material?.Description ?? "", group.Sum(item => item.BaseQuantity),
                uoms.Count == 1 ? uoms[0] : material?.BaseUom ?? "EA", txnCurrency is null ? null : group.Sum(item => item.TransactionValue ?? 0), loc?.LocationName);
        }).OrderByDescending(item => item.Value ?? item.Quantity).Take(8).ToList();

        var wasteValue = movement.Buckets.First(item => item.Key == "WASTE").Value ?? 0;
        var currency = balances.Select(item => item.Currency).Concat(locations.Select(item => item.Currency)).FirstOrDefault(item => !string.IsNullOrWhiteSpace(item)) ?? "AED";
        var reorderConfigured = stocked.Any(item => item.ReorderPoint is not null);
        var kpis = new List<InventoryDashboardKpi>
        {
            new("INVENTORY_VALUE", "Inventory Value", configured ? totalValue : null, configured ? InventoryControlMath.CompactMoney(totalValue, currency) : "Not configured",
                configured ? $"Across {scoped.Count(item => item.InventoryEnabled)} inventory locations" : "Configure Location Master to begin", configured, "/inventory/live"),
            new("STOCKOUTS", "Stockouts", configured ? stockouts : null, configured ? stockouts.ToString("N0") : "Not configured",
                configured ? $"Across {stocked.Select(item => item.InventoryLocationId).Distinct().Count()} stocking locations" : "Not configured", configured, "/inventory/live"),
            new("LOW_STOCK", "Low Stock", configured ? low : null, configured ? low.ToString("N0") : "Not configured",
                reorderConfigured ? "Need replenishment" : "Reorder points are not configured", configured, "#replenishment"),
            new("INVENTORY_AT_RISK", "Inventory at Risk", wasteValue > 0 ? wasteValue : null, wasteValue > 0 ? InventoryControlMath.CompactMoney(wasteValue, currency) : "Not configured",
                wasteValue > 0 ? "Waste / damage today" : "Expiry tracking is not configured. No waste or variance postings for this period.", wasteValue > 0, "#action-center"),
        };

        await RefreshStockAlerts(organizationId, stocked, balanceMap, cancellationToken);
        var alerts = await AlertsAsync(organizationId, cancellationToken);
        return new InventoryDashboardResponse(kpis, alerts, locations.Select(item => InventoryLocationService.ToRow(item, names)).ToList(),
            new InventoryStockHealth(healthy, low, stockouts, excess, "Expiry tracking is not configured.", false),
            actions.Take(6).ToList(), replenishment.OrderBy(item => item.Destination).ThenBy(item => item.MaterialName).Take(40).ToList(),
            movement, pipeline, locationValues, topConsumption, currency);
    }

    public async Task<IReadOnlyList<InventoryAlertRow>> AlertsAsync(Guid organizationId, CancellationToken cancellationToken)
    {
        var alerts = await db.InventoryAlerts.AsNoTracking()
            .Where(item => item.OrganizationId == organizationId && item.Status != InventoryAlertStatus.CLOSED && item.Status != InventoryAlertStatus.DISMISSED)
            .OrderByDescending(item => item.CreatedAt)
            .Take(50)
            .ToListAsync(cancellationToken);
        var locationIds = alerts.Where(item => item.InventoryLocationId != null).Select(item => item.InventoryLocationId!.Value).Distinct().ToList();
        var locations = await db.InventoryLocations.AsNoTracking().Where(item => locationIds.Contains(item.Id)).ToDictionaryAsync(item => item.Id, cancellationToken);
        var materialIds = alerts.Where(item => item.MaterialId != null).Select(item => item.MaterialId!.Value).Distinct().ToList();
        var materials = await db.Materials.AsNoTracking().Where(item => materialIds.Contains(item.Id)).ToDictionaryAsync(item => item.Id, cancellationToken);
        return alerts.Select(item => new InventoryAlertRow(
            item.Id, item.Kind.ToString(), item.Severity.ToString(), item.Status.ToString(), item.Title, item.Message,
            item.InventoryLocationId, item.InventoryLocationId is Guid loc && locations.TryGetValue(loc, out var location) ? location.LocationName : null,
            item.MaterialId, item.MaterialId is Guid mat && materials.TryGetValue(mat, out var material) ? material.MaterialCode : null,
            item.RecommendedAction?.ToString(), item.CreatedAt)).ToList();
    }

    public async Task AlertActionAsync(Session session, Guid organizationId, Guid id, InventoryAlertActionRequest request, CancellationToken cancellationToken)
    {
        var alert = await db.InventoryAlerts.SingleOrDefaultAsync(item => item.Id == id && item.OrganizationId == organizationId, cancellationToken)
            ?? throw new RecipeManagementException("ALERT_NOT_FOUND", "The alert was not found.", 404);
        alert.Status = request.Action.ToUpperInvariant() switch
        {
            "ACKNOWLEDGE" => InventoryAlertStatus.ACKNOWLEDGED,
            "ACTION_INITIATED" => InventoryAlertStatus.ACTION_INITIATED,
            "IN_PROGRESS" => InventoryAlertStatus.IN_PROGRESS,
            "RESOLVE" => InventoryAlertStatus.RESOLVED,
            "CLOSE" => InventoryAlertStatus.CLOSED,
            "DISMISS" => InventoryAlertStatus.DISMISSED,
            _ => throw new RecipeManagementException("INVALID_ALERT_ACTION", "Unsupported alert action.")
        };
        alert.UpdatedAt = DateTime.UtcNow;
        db.InventoryWorkflowEvents.Add(new InventoryWorkflowEvent
        {
            Id = Guid.NewGuid(), OrganizationId = organizationId, ReferenceType = "ALERT", ReferenceId = alert.Id,
            Action = alert.Status.ToString(), Comment = request.Comment, ActorUserId = session.UserId ?? Guid.Empty, CreatedAt = DateTime.UtcNow
        });
        await db.SaveChangesAsync(cancellationToken);
    }

    public async Task<PageResult<LiveMaterialHit>> SearchMaterialsAsync(
        Guid organizationId, string? query, string? materialGroup, string? category, string? cursor, int pageSize, CancellationToken cancellationToken)
    {
        var page = await materials.SearchAsync(organizationId, query, "ACTIVE", materialGroup, null, category, null, cursor, Math.Clamp(pageSize <= 0 ? 20 : pageSize, 1, 40), cancellationToken);
        if (!string.IsNullOrWhiteSpace(query))
        {
            var term = query.Trim().ToUpperInvariant();
            var extras = await db.Materials.AsNoTracking()
                .Where(item => item.OrganizationId == organizationId && item.Status == StatusKind.ACTIVE &&
                    ((item.ExternalId != null && item.ExternalId.ToUpper().Contains(term)) || item.MaterialCode.ToUpper() == term))
                .Take(20)
                .ToListAsync(cancellationToken);
            if (extras.Count > 0)
            {
                var extraRows = extras
                    .Where(item => page.Items.All(row => row.Id != item.Id))
                    .Select(item => new LiveMaterialHit(item.Id, item.MaterialCode, item.Description, item.BaseUom, MaterialCosting.EffectiveUnitCost(item), item.Currency, MaterialCosting.PriceStatus(item, null), item.MaterialGroup, item.Category, 0))
                    .ToList();
                var extraIds = extras.Select(item => item.Id).Concat(page.Items.Select(item => item.Id)).Distinct().ToList();
                var totalsExtra = await db.InventoryBalances.AsNoTracking()
                    .Where(item => extraIds.Contains(item.MaterialId))
                    .GroupBy(item => item.MaterialId)
                    .Select(group => new { group.Key, Available = group.Sum(item => item.AvailableQty) })
                    .ToDictionaryAsync(item => item.Key, item => item.Available, cancellationToken);
                var merged = extraRows.Concat(page.Items.Select(item => new LiveMaterialHit(
                    item.Id, item.MaterialCode, item.Description, item.BaseUom, item.UnitCost, item.Currency, item.PriceStatus,
                    item.MaterialGroup, item.Category, totalsExtra.GetValueOrDefault(item.Id)))).Take(page.PageSize).ToList();
                return new PageResult<LiveMaterialHit>(merged, page.NextCursor, page.PageSize, merged.Count);
            }
        }
        var ids = page.Items.Select(item => item.Id).ToList();
        var totals = await db.InventoryBalances.AsNoTracking()
            .Where(item => ids.Contains(item.MaterialId))
            .GroupBy(item => item.MaterialId)
            .Select(group => new { group.Key, Available = group.Sum(item => item.AvailableQty) })
            .ToDictionaryAsync(item => item.Key, item => item.Available, cancellationToken);
        var hits = page.Items.Select(item => new LiveMaterialHit(
            item.Id, item.MaterialCode, item.Description, item.BaseUom, item.UnitCost, item.Currency, item.PriceStatus,
            item.MaterialGroup, item.Category, totals.GetValueOrDefault(item.Id))).ToList();
        return new PageResult<LiveMaterialHit>(hits, page.NextCursor, page.PageSize, hits.Count);
    }

    public async Task<LiveInventoryDetail> LiveAsync(Guid organizationId, Guid materialId, LiveDecisionRequest request, Guid? myLocationId, CancellationToken cancellationToken)
    {
        var material = await db.Materials.AsNoTracking().SingleOrDefaultAsync(item => item.Id == materialId && item.OrganizationId == organizationId, cancellationToken)
            ?? throw new RecipeManagementException("MATERIAL_NOT_FOUND", "The material was not found.", 404);
        var pending = await db.MaterialChangeRequests.AsNoTracking().FirstOrDefaultAsync(item => item.MaterialId == material.Id && item.Status == MaterialGovernanceStatus.PENDING_APPROVAL, cancellationToken);
        var locations = await db.InventoryLocations.AsNoTracking().Where(item => item.OrganizationId == organizationId && item.Status == StatusKind.ACTIVE).ToListAsync(cancellationToken);
        var names = locations.ToDictionary(item => item.Id);
        var balances = await db.InventoryBalances.AsNoTracking().Where(item => item.MaterialId == materialId).ToListAsync(cancellationToken);
        var byLocation = balances.ToDictionary(item => item.InventoryLocationId);
        var assignments = await db.MaterialLocations.AsNoTracking()
            .Where(item => item.OrganizationId == organizationId && item.MaterialId == materialId)
            .ToListAsync(cancellationToken);
        var assignmentByLocation = assignments.ToDictionary(item => item.InventoryLocationId);
        var currentId = request.CurrentLocationId ?? myLocationId;
        var availability = locations
            .Where(item => item.InventoryEnabled || byLocation.ContainsKey(item.Id) || assignmentByLocation.ContainsKey(item.Id))
            .Select(item =>
            {
                byLocation.TryGetValue(item.Id, out var balance);
                assignmentByLocation.TryGetValue(item.Id, out var assignment);
                var onHand = balance?.OnHandQty ?? 0;
                var reserved = balance?.ReservedQty ?? 0;
                var available = balance?.AvailableQty ?? Math.Max(0, onHand - reserved);
                var inTransit = balance?.InTransitQty ?? 0;
                var hold = assignment?.SafetyStock ?? 0;
                var transferable = item.TransferEnabled ? Math.Max(0, available - hold) : 0;
                names.TryGetValue(item.PropertyLocationId ?? item.Id, out var property);
                var status = onHand < 0 ? "NEGATIVE" : available <= 0 ? "OUT_OF_STOCK" : assignment?.ReorderPoint is decimal reorder && available <= reorder ? "LOW" : available <= 5 ? "LOW" : "IN_STOCK";
                var stocking = assignment is { Active: true, StockingStatus: MaterialStockingStatus.ACTIVE } ? assignment.StockingType.ToString() : "NOT_STOCKED";
                return new LiveAvailabilityRow(
                    item.Id, item.LocationCode, item.LocationName, item.LocationType.ToString(), property?.LocationCode,
                    onHand, reserved, available, inTransit, transferable,
                    item.TransferEnabled ? (hold > 0 ? $"Available minus safety stock {hold}" : "Available quantity; no safety stock configured") : "Transfers are disabled for this location",
                    status, item.TransferEnabled, stocking, assignment?.StockingType.ToString());
            })
            .OrderBy(item => currentId == item.InventoryLocationId ? 0 : 1)
            .ThenBy(item => item.AvailableQty > 0 ? 0 : 1)
            .ThenByDescending(item => item.TransferableQty)
            .ToList();
        var local = availability.FirstOrDefault(item => item.InventoryLocationId == currentId);
        var required = request.RequiredQty < 0 ? 0 : request.RequiredQty;
        var localAvailable = local?.AvailableQty ?? 0;
        var shortage = Math.Max(0, required - localAvailable);
        var notStocked = currentId is Guid && (local is null || local.StockingStatus == "NOT_STOCKED");
        var internalStock = availability.Where(item => item.InventoryLocationId != currentId && item.TransferEnabled && item.TransferableQty > 0).Sum(item => item.TransferableQty);
        string recommendation;
        string reason;
        var actions = new List<string> { "VIEW_TRANSACTIONS" };
        if (internalStock > 0)
        {
            actions.Insert(0, "REQUEST_TRANSFER");
            actions.Insert(1, "QUICK_TRANSFER");
            actions.Add("REDISTRIBUTE_STOCK");
        }
        if (!MaterialNormalized.CanHoldStock(material) || material.InventoryType == InventoryItemType.SERVICE)
        {
            recommendation = "NOT_INVENTORY_ITEM";
            reason = "This material is not an inventory STOCK item and cannot receive physical quantity.";
        }
        else if (shortage <= 0)
        {
            recommendation = "SUFFICIENT";
            reason = "Current location covers the required quantity.";
        }
        else if (internalStock >= shortage)
        {
            recommendation = "REQUEST_INTERNAL_TRANSFER";
            reason = "Internal transferable stock can cover the shortage. Internal inventory is preferred before procurement.";
            if (notStocked)
            {
                actions.Insert(0, "ADD_TO_LOCATION");
                actions.Insert(1, "ONE_TIME_TRANSFER");
            }
        }
        else
        {
            recommendation = "CREATE_PR";
            reason = "No suitable internal transferable stock covers the shortage. Create an internal purchase request for later procurement integration.";
            actions.Insert(0, "CREATE_PR");
            if (notStocked) actions.Insert(0, "REQUEST_ITEM");
        }
        actions.Add("REQUEST_PHYSICAL_INVENTORY");
        actions.Add("VIEW_INCOMING_TRANSFER");
        var hit = new LiveMaterialHit(material.Id, material.MaterialCode, material.Description, material.BaseUom,
            MaterialCosting.EffectiveUnitCost(material), material.Currency, MaterialCosting.PriceStatus(material, pending),
            material.MaterialGroup, material.Category, availability.Sum(item => item.AvailableQty));
        return new LiveInventoryDetail(hit, availability, required, localAvailable, shortage, recommendation, reason, actions.Distinct().ToList(),
            material.InventoryType.ToString(), material.InventoryItem, material.BatchManaged, material.ExpiryManaged, material.SerialManaged,
            local?.StockingStatus ?? "NOT_STOCKED", notStocked);
    }

    public async Task<MobileInventoryHome> MobileHomeAsync(Guid organizationId, Guid userId, CancellationToken cancellationToken)
    {
        var mine = await MyLocationAsync(organizationId, userId, cancellationToken);
        var locationId = mine?.Id;
        var balances = db.InventoryBalances.AsNoTracking().Where(item => item.OrganizationId == organizationId);
        if (locationId is Guid loc) balances = balances.Where(item => item.InventoryLocationId == loc);
        var list = await balances.ToListAsync(cancellationToken);
        var incoming = await db.InternalTransferOrders.CountAsync(item => item.OrganizationId == organizationId && item.ToInventoryLocationId == locationId && (item.Status == ItoStatus.IN_TRANSIT || item.Status == ItoStatus.DISPATCHED), cancellationToken);
        var approvals = await db.InternalTransferApprovals.CountAsync(item => item.Status == ItoApprovalStatus.PENDING && item.Ito.OrganizationId == organizationId, cancellationToken);
        var alerts = await db.InventoryAlerts.CountAsync(item => item.OrganizationId == organizationId && item.Severity == InventoryAlertSeverity.CRITICAL && item.Status == InventoryAlertStatus.NEW, cancellationToken);
        var myActions = incoming + approvals + alerts;
        return new MobileInventoryHome(locationId, mine?.LocationName, mine?.LocationType.ToString(),
            list.Count(item => item.AvailableQty > 0), list.Count(item => item.AvailableQty > 0 && item.AvailableQty <= 5),
            incoming, myActions, incoming, incoming, approvals, alerts);
    }

    public async Task<InventoryLocation?> MyLocationAsync(Guid organizationId, Guid userId, CancellationToken cancellationToken)
    {
        var assigned = await db.UserInventoryLocations.Include(item => item.InventoryLocation)
            .Where(item => item.OrganizationId == organizationId && item.UserId == userId)
            .OrderByDescending(item => item.IsDefault)
            .FirstOrDefaultAsync(cancellationToken);
        if (assigned is not null) return assigned.InventoryLocation;
        return null;
    }

    public async Task<IReadOnlyList<LocationUserAssignmentRow>> LocationUsersAsync(Guid organizationId, Guid? locationId, CancellationToken cancellationToken)
    {
        var rows = db.UserInventoryLocations.AsNoTracking().Include(item => item.InventoryLocation)
            .Where(item => item.OrganizationId == organizationId);
        if (locationId is Guid id) rows = rows.Where(item => item.InventoryLocationId == id);
        var list = await rows.ToListAsync(cancellationToken);
        var users = await db.Users.AsNoTracking().Where(item => list.Select(row => row.UserId).Contains(item.Id)).ToDictionaryAsync(item => item.Id, cancellationToken);
        return list.Select(item =>
        {
            users.TryGetValue(item.UserId, out var user);
            return new LocationUserAssignmentRow(item.UserId, user?.DisplayName ?? "", user?.Email ?? "", item.InventoryLocationId, item.InventoryLocation.LocationCode, item.InventoryLocation.LocationName, item.IsDefault);
        }).ToList();
    }

    public async Task AssignUserAsync(Guid organizationId, Guid locationId, AssignLocationUserRequest request, CancellationToken cancellationToken)
    {
        var location = await db.InventoryLocations.SingleOrDefaultAsync(item => item.Id == locationId && item.OrganizationId == organizationId, cancellationToken)
            ?? throw new RecipeManagementException("LOCATION_NOT_FOUND", "The inventory location was not found.", 404);
        var user = await db.Users.AsNoTracking().SingleOrDefaultAsync(item => item.Id == request.UserId, cancellationToken)
            ?? throw new RecipeManagementException("USER_NOT_FOUND", "The user was not found.", 404);
        var rows = await db.UserInventoryLocations.Where(item => item.OrganizationId == organizationId && item.UserId == request.UserId).ToListAsync(cancellationToken);
        if (request.Remove)
        {
            db.UserInventoryLocations.RemoveRange(rows.Where(item => item.InventoryLocationId == locationId));
            await db.SaveChangesAsync(cancellationToken);
            return;
        }
        if (request.IsDefault)
        {
            foreach (var row in rows) row.IsDefault = row.InventoryLocationId == locationId;
        }
        if (rows.All(item => item.InventoryLocationId != locationId))
        {
            db.UserInventoryLocations.Add(new UserInventoryLocation
            {
                Id = Guid.NewGuid(), OrganizationId = organizationId, UserId = user.Id, InventoryLocationId = location.Id,
                IsDefault = request.IsDefault || rows.Count == 0, CreatedAt = DateTime.UtcNow
            });
        }
        await db.SaveChangesAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<InventoryUserOption>> AssignableUsersAsync(Guid organizationId, CancellationToken cancellationToken)
    {
        var userIds = await db.UserOrganizationMemberships.AsNoTracking()
            .Where(item => item.OrganizationId == organizationId && item.Status == StatusKind.ACTIVE)
            .Select(item => item.UserId)
            .Distinct()
            .ToListAsync(cancellationToken);
        return await db.Users.AsNoTracking()
            .Where(item => userIds.Contains(item.Id) && item.Status == StatusKind.ACTIVE)
            .OrderBy(item => item.DisplayName)
            .Select(item => new InventoryUserOption(item.Id, item.DisplayName, item.Email))
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<MyLocationRow>> AssignmentsAsync(Guid organizationId, Guid userId, CancellationToken cancellationToken)
    {
        return await db.UserInventoryLocations.AsNoTracking().Include(item => item.InventoryLocation)
            .Where(item => item.OrganizationId == organizationId && item.UserId == userId)
            .Select(item => new MyLocationRow(item.InventoryLocationId, item.InventoryLocation.LocationCode, item.InventoryLocation.LocationName, item.InventoryLocation.LocationType.ToString(), item.IsDefault))
            .ToListAsync(cancellationToken);
    }

    public async Task AssignMyLocationAsync(Guid organizationId, Guid userId, Guid locationId, CancellationToken cancellationToken)
    {
        var location = await db.InventoryLocations.SingleOrDefaultAsync(item => item.Id == locationId && item.OrganizationId == organizationId, cancellationToken)
            ?? throw new RecipeManagementException("LOCATION_NOT_FOUND", "The inventory location was not found.", 404);
        var rows = await db.UserInventoryLocations.Where(item => item.OrganizationId == organizationId && item.UserId == userId).ToListAsync(cancellationToken);
        foreach (var row in rows) row.IsDefault = row.InventoryLocationId == locationId;
        if (rows.All(item => item.InventoryLocationId != locationId))
        {
            db.UserInventoryLocations.Add(new UserInventoryLocation
            {
                Id = Guid.NewGuid(), OrganizationId = organizationId, UserId = userId, InventoryLocationId = location.Id, IsDefault = true, CreatedAt = DateTime.UtcNow
            });
        }
        await db.SaveChangesAsync(cancellationToken);
    }

    public async Task<PhysicalInventoryRequestRow> RequestPhysicalInventoryAsync(Session session, Guid organizationId, PhysicalInventoryRequestInput request, bool allowSurprise, CancellationToken cancellationToken)
    {
        var location = await db.InventoryLocations.SingleOrDefaultAsync(item => item.Id == request.InventoryLocationId && item.OrganizationId == organizationId, cancellationToken)
            ?? throw new RecipeManagementException("LOCATION_NOT_FOUND", "The inventory location was not found.", 404);
        if (request.SurpriseCount && !allowSurprise)
            throw new RecipeManagementException("SURPRISE_COUNT_FORBIDDEN", "Surprise counts require APPROVE_INVENTORY_COUNT.", 403);
        var item = new PhysicalInventoryRequest
        {
            Id = Guid.NewGuid(), OrganizationId = organizationId, InventoryLocationId = location.Id, Reason = request.Reason,
            Priority = string.IsNullOrWhiteSpace(request.Priority) ? "MEDIUM" : request.Priority.Trim().ToUpperInvariant(),
            RequestedBy = session.UserId ?? Guid.Empty, AssignedGroup = request.AssignedGroup, ManagerVisibility = true,
            SurpriseCount = request.SurpriseCount && allowSurprise, Status = PhysicalInventoryRequestStatus.REQUESTED, CreatedAt = DateTime.UtcNow
        };
        db.PhysicalInventoryRequests.Add(item);
        await RaiseAlert(organizationId, session.UserId ?? Guid.Empty, InventoryAlertKind.INVENTORY_VARIANCE, InventoryAlertSeverity.MEDIUM, "Physical inventory requested", request.Reason, location.Id, null, item.Id, "PHYSICAL_INVENTORY_REQUEST", InventoryNextActionType.REQUEST_PHYSICAL_INVENTORY, cancellationToken);
        await db.SaveChangesAsync(cancellationToken);
        return new PhysicalInventoryRequestRow(item.Id, location.Id, location.LocationName, item.Reason, item.Priority, item.SurpriseCount, item.Status.ToString(), item.CreatedAt);
    }

    public async Task<InternalPurchaseRequestRow> CreatePurchaseRequestAsync(Session session, Guid organizationId, InternalPurchaseRequestInput request, CancellationToken cancellationToken)
    {
        var item = new InternalPurchaseRequest
        {
            Id = Guid.NewGuid(), OrganizationId = organizationId, InventoryLocationId = request.InventoryLocationId, MaterialId = request.MaterialId,
            Quantity = request.Quantity, Uom = request.Uom, Reason = request.Reason, Status = InternalPurchaseRequestStatus.READY_FOR_INTEGRATION,
            RequestedBy = session.UserId ?? Guid.Empty, CreatedAt = DateTime.UtcNow
        };
        db.InternalPurchaseRequests.Add(item);
        await db.SaveChangesAsync(cancellationToken);
        return new InternalPurchaseRequestRow(item.Id, item.MaterialId, item.Quantity, item.Reason, item.Status.ToString(), item.CreatedAt);
    }

    public async Task<QuickTransferPolicyRow> PolicyAsync(Guid organizationId, CancellationToken cancellationToken)
    {
        var item = await EnsurePolicy(organizationId, cancellationToken);
        return ToPolicy(item);
    }

    public async Task<QuickTransferPolicyRow> SavePolicyAsync(Guid organizationId, QuickTransferPolicyRow request, CancellationToken cancellationToken)
    {
        var item = await EnsurePolicy(organizationId, cancellationToken);
        item.Enabled = request.Enabled;
        item.AllowedSourceLocationTypes = string.Join(',', request.AllowedSourceLocationTypes);
        item.AllowedDestinationLocationTypes = string.Join(',', request.AllowedDestinationLocationTypes);
        item.MaximumQuantity = request.MaximumQuantity;
        item.MaximumValue = request.MaximumValue;
        item.SamePropertyAllowed = request.SamePropertyAllowed;
        item.CrossPropertyAllowed = request.CrossPropertyAllowed;
        item.SourceConfirmationRequired = request.SourceConfirmationRequired;
        item.DestinationConfirmationRequired = request.DestinationConfirmationRequired;
        item.ManagerNotification = request.ManagerNotification;
        item.SkipManagerApproval = request.SkipManagerApproval;
        item.UpdatedAt = DateTime.UtcNow;
        await db.SaveChangesAsync(cancellationToken);
        return ToPolicy(item);
    }

    internal async Task<QuickTransferPolicy> EnsurePolicy(Guid organizationId, CancellationToken cancellationToken)
    {
        var item = await db.QuickTransferPolicies.SingleOrDefaultAsync(row => row.OrganizationId == organizationId, cancellationToken);
        if (item is not null) return item;
        item = new QuickTransferPolicy { Id = Guid.NewGuid(), OrganizationId = organizationId, UpdatedAt = DateTime.UtcNow };
        db.QuickTransferPolicies.Add(item);
        await db.SaveChangesAsync(cancellationToken);
        return item;
    }

    internal async Task RaiseAlert(Guid organizationId, Guid userId, InventoryAlertKind kind, InventoryAlertSeverity severity, string title, string? message, Guid? locationId, Guid? materialId, Guid? referenceId, string? referenceType, InventoryNextActionType? action, CancellationToken cancellationToken)
    {
        db.InventoryAlerts.Add(new InventoryAlert
        {
            Id = Guid.NewGuid(), OrganizationId = organizationId, Kind = kind, Severity = severity, Status = InventoryAlertStatus.NEW,
            Title = title, Message = message, InventoryLocationId = locationId, MaterialId = materialId, ReferenceId = referenceId,
            ReferenceType = referenceType, RecommendedAction = action, CreatedBy = userId, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow
        });
        await Task.CompletedTask;
    }

    private static QuickTransferPolicyRow ToPolicy(QuickTransferPolicy item) => new(
        item.Enabled, Split(item.AllowedSourceLocationTypes), Split(item.AllowedDestinationLocationTypes),
        item.MaximumQuantity, item.MaximumValue, item.SamePropertyAllowed, item.CrossPropertyAllowed,
        item.SourceConfirmationRequired, item.DestinationConfirmationRequired, item.ManagerNotification, item.SkipManagerApproval);

    private static IReadOnlyList<string> Split(string value) => value.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

    private async Task RefreshStockAlerts(
        Guid organizationId, IReadOnlyList<MaterialLocation> stocked, Dictionary<(Guid MaterialId, Guid LocationId), InventoryBalance> balances, CancellationToken cancellationToken)
    {
        var open = await db.InventoryAlerts
            .Where(item => item.OrganizationId == organizationId
                && (item.Kind == InventoryAlertKind.LOW_STOCK || item.Kind == InventoryAlertKind.STOCKOUT_RISK || item.Kind == InventoryAlertKind.NEGATIVE_STOCK)
                && item.Status != InventoryAlertStatus.CLOSED && item.Status != InventoryAlertStatus.DISMISSED)
            .ToListAsync(cancellationToken);
        foreach (var row in stocked)
        {
            balances.TryGetValue((row.MaterialId, row.InventoryLocationId), out var balance);
            var available = balance?.AvailableQty ?? 0;
            var onHand = balance?.OnHandQty ?? 0;
            InventoryAlertKind kind;
            InventoryAlertSeverity severity;
            InventoryNextActionType action;
            string title;
            string message;
            if (onHand < 0)
            {
                kind = InventoryAlertKind.NEGATIVE_STOCK;
                severity = InventoryAlertSeverity.CRITICAL;
                action = InventoryNextActionType.INVESTIGATE;
                title = "Negative stock";
                message = $"On-hand is {onHand} {row.Material.BaseUom}.";
            }
            else if (available <= 0)
            {
                kind = InventoryAlertKind.STOCKOUT_RISK;
                severity = InventoryAlertSeverity.HIGH;
                action = InventoryNextActionType.REQUEST_TRANSFER;
                title = "Stockout";
                message = "Available quantity is zero at a stocking location.";
            }
            else if (row.ReorderPoint is decimal reorder && available <= reorder)
            {
                kind = InventoryAlertKind.LOW_STOCK;
                severity = InventoryAlertSeverity.MEDIUM;
                action = InventoryNextActionType.REQUEST_TRANSFER;
                title = "Low stock";
                message = $"Available quantity is {available} {row.Material.BaseUom} (reorder {reorder}).";
            }
            else continue;
            if (open.Any(item => item.MaterialId == row.MaterialId && item.InventoryLocationId == row.InventoryLocationId && item.Kind == kind))
                continue;
            db.InventoryAlerts.Add(new InventoryAlert
            {
                Id = Guid.NewGuid(), OrganizationId = organizationId, Kind = kind, Severity = severity, Status = InventoryAlertStatus.NEW,
                Title = title, Message = message, InventoryLocationId = row.InventoryLocationId, MaterialId = row.MaterialId,
                RecommendedAction = action, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow
            });
        }
        await db.SaveChangesAsync(cancellationToken);
    }
}
