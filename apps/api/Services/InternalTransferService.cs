using Microsoft.EntityFrameworkCore;
using SilaMe.Api.Auth;
using SilaMe.Api.Data;
using SilaMe.Api.DTOs;
using SilaMe.Api.Models;

namespace SilaMe.Api.Services;

public sealed class InternalTransferService(SilaMeDbContext db, InventoryWorkspaceService workspace, MaterialLocationService assignments)
{
    public async Task<IReadOnlyList<ItoListRow>> ListAsync(
        Guid organizationId, string? mode, string? status, Guid? propertyId, Guid? fromId, Guid? toId, DateTime? fromDate, DateTime? toDate, CancellationToken cancellationToken)
    {
        var rows = db.InternalTransferOrders.AsNoTracking()
            .Include(item => item.FromLocation)
            .Include(item => item.ToLocation)
            .Where(item => item.OrganizationId == organizationId);
        if (Enum.TryParse<ItoMode>(mode, true, out var parsedMode)) rows = rows.Where(item => item.Mode == parsedMode);
        if (Enum.TryParse<ItoStatus>(status, true, out var parsedStatus)) rows = rows.Where(item => item.Status == parsedStatus);
        if (fromId is Guid from) rows = rows.Where(item => item.FromInventoryLocationId == from);
        if (toId is Guid to) rows = rows.Where(item => item.ToInventoryLocationId == to);
        if (propertyId is Guid property) rows = rows.Where(item => item.FromPropertyId == property || item.ToPropertyId == property);
        if (fromDate is DateTime start) rows = rows.Where(item => item.RequestedAt >= start);
        if (toDate is DateTime end) rows = rows.Where(item => item.RequestedAt < end.AddDays(1));
        var list = await rows.OrderByDescending(item => item.RequestedAt).Take(200).ToListAsync(cancellationToken);
        var users = await db.Users.AsNoTracking().Where(item => list.Select(row => row.RequestedBy).Contains(item.Id)).ToDictionaryAsync(item => item.Id, cancellationToken);
        return list.Select(item => new ItoListRow(
            item.Id, item.ItoNumber, item.Mode.ToString(), item.FromLocation.LocationName, item.ToLocation.LocationName,
            Relationship(item.FromLocation, item.ToLocation), item.RequestedAt, item.RequiredBy, item.TotalValue, item.Currency,
            item.Status.ToString(), users.TryGetValue(item.RequestedBy, out var user) ? user.DisplayName : "")).ToList();
    }

    public async Task<ItoDetail> GetAsync(Guid organizationId, Guid id, IReadOnlySet<string> permissions, CancellationToken cancellationToken)
    {
        var item = await Load(organizationId, id, cancellationToken);
        var events = await db.InventoryWorkflowEvents.AsNoTracking()
            .Where(row => row.ReferenceId == item.Id)
            .OrderBy(row => row.CreatedAt)
            .ToListAsync(cancellationToken);
        return ToDetail(item, await SourceStock(item, cancellationToken), permissions, events);
    }

    public async Task<ItoDetail> CreateAsync(Session session, Guid organizationId, ItoCreateRequest request, IReadOnlySet<string> permissions, CancellationToken cancellationToken)
    {
        var mode = Enum.TryParse<ItoMode>(request.Mode, true, out var parsed) ? parsed : ItoMode.STANDARD;
        var from = await Location(organizationId, request.FromInventoryLocationId, cancellationToken);
        var to = await Location(organizationId, request.ToInventoryLocationId, cancellationToken);
        if (from.Id == to.Id) throw new RecipeManagementException("SOURCE_DESTINATION_SAME", "From and To locations must be different.");
        if (!from.TransferEnabled || !to.TransferEnabled)
            throw new RecipeManagementException("TRANSFER_DISABLED", "Both locations must have Transfer Enabled.");
        var policy = await workspace.EnsurePolicy(organizationId, cancellationToken);
        if (mode == ItoMode.QUICK) ValidateQuick(policy, from, to, request.Lines);
        var now = DateTime.UtcNow;
        var ito = new InternalTransferOrder
        {
            Id = Guid.NewGuid(),
            OrganizationId = organizationId,
            ItoNumber = await NextNumber(organizationId, cancellationToken),
            Mode = mode,
            FromInventoryLocationId = from.Id,
            ToInventoryLocationId = to.Id,
            FromPropertyId = from.LocationType == InventoryLocationType.PROPERTY ? from.Id : from.PropertyLocationId,
            ToPropertyId = to.LocationType == InventoryLocationType.PROPERTY ? to.Id : to.PropertyLocationId,
            Reason = string.IsNullOrWhiteSpace(request.Reason) ? (mode == ItoMode.QUICK ? "Guest Service" : null) : request.Reason,
            RequiredBy = request.RequiredBy,
            BusinessDate = now.Date,
            Status = ItoStatus.DRAFT,
            RequestedBy = Actor(session),
            RequestedAt = now,
            AlreadyCollected = request.AlreadyCollected,
            OneTimeTransfer = request.OneTimeTransfer && !request.AddToLocation,
            CreatedAt = now,
            UpdatedAt = now,
        };
        foreach (var line in request.Lines ?? [])
        {
            if (line.Quantity <= 0) throw new RecipeManagementException("INVALID_QUANTITY", "Quantity must be greater than zero.");
            var material = await db.Materials.SingleOrDefaultAsync(item => item.Id == line.MaterialId && item.OrganizationId == organizationId, cancellationToken)
                ?? throw new RecipeManagementException("MATERIAL_NOT_FOUND", "A transfer line material was not found.", 404);
            if (material.InventoryType == InventoryItemType.SERVICE || !MaterialNormalized.CanHoldStock(material))
                throw new RecipeManagementException("SERVICE_NO_STOCK", "SERVICE and non-stock materials cannot move as physical inventory.");
            if (request.AddToLocation)
                await assignments.UpsertAsync(session, organizationId, new MaterialLocationUpsertRequest(material.Id, to.Id, "ACTIVE", "REGULAR", null, null, null, null, null, from.Id, "ITO", true), cancellationToken);
            var cost = MaterialCosting.EffectiveUnitCost(material);
            ito.Lines.Add(new InternalTransferLine
            {
                Id = Guid.NewGuid(),
                MaterialId = material.Id,
                RequestedQty = line.Quantity,
                ApprovedQty = mode == ItoMode.QUICK ? line.Quantity : 0,
                Uom = string.IsNullOrWhiteSpace(line.Uom) ? material.BaseUom : line.Uom,
                BaseQty = line.Quantity,
                BaseUom = material.BaseUom,
                UnitCost = cost,
                TransferValue = (cost ?? 0) * line.Quantity,
                Comment = line.Comment,
            });
        }
        if (ito.Lines.Count == 0) throw new RecipeManagementException("LINES_REQUIRED", "Add at least one material.");
        ito.TotalValue = ito.Lines.Sum(item => item.TransferValue);
        ito.Currency = ito.Lines.Select(item => db.Materials.Local.FirstOrDefault(row => row.Id == item.MaterialId)?.Currency).FirstOrDefault(item => !string.IsNullOrWhiteSpace(item));
        if (ito.Currency is null)
        {
            var first = await db.Materials.AsNoTracking().SingleAsync(item => item.Id == ito.Lines.First().MaterialId, cancellationToken);
            ito.Currency = first.Currency;
        }
        db.InternalTransferOrders.Add(ito);
        Event(ito, Actor(session), request.AlreadyCollected ? "RECORD_ALREADY_COLLECTED" : "CREATED", request.Reason);
        await SubmitCreated(session, ito, from, to, policy, cancellationToken);
        await db.SaveChangesAsync(cancellationToken);
        return await GetAsync(organizationId, ito.Id, permissions, cancellationToken);
    }

    public Task<ItoDetail> QuickGetOneAsync(Session session, Guid organizationId, Guid materialId, Guid fromId, Guid toId, IReadOnlySet<string> permissions, CancellationToken cancellationToken) =>
        CreateAsync(session, organizationId, new ItoCreateRequest("QUICK", fromId, toId, "Guest Service", null, false, [new ItoLineInput(materialId, 1, null, null)]), permissions, cancellationToken);

    public async Task<ItoDetail> ApproveAsync(Session session, Guid organizationId, Guid id, ItoApproveRequest request, IReadOnlySet<string> permissions, CancellationToken cancellationToken)
    {
        var ito = await Load(organizationId, id, cancellationToken);
        if (ito.Status is not ItoStatus.PENDING_APPROVAL and not ItoStatus.SUBMITTED)
            throw new RecipeManagementException("INVALID_STATUS", "This transfer is not waiting for approval.");
        var pending = ito.Approvals.FirstOrDefault(item => item.Status == ItoApprovalStatus.PENDING)
            ?? throw new RecipeManagementException("NO_PENDING_APPROVAL", "There is no pending approval.");
        await EnsureManagerGroup(session, organizationId, pending, cancellationToken);
        var qty = request.ApprovedQty ?? pending.RequestedQty ?? ito.Lines.Sum(item => item.RequestedQty);
        pending.Status = ItoApprovalStatus.APPROVED;
        pending.ApprovedQty = qty;
        pending.Comment = request.Comment;
        pending.ActorUserId = Actor(session);
        pending.ActedAt = DateTime.UtcNow;
        ApplyLineQty(ito, request.LineId, qty, (line, value) => line.ApprovedQty = value);
        Event(ito, Actor(session), "APPROVED", request.Comment);
        if (ito.Approvals.All(item => item.Status == ItoApprovalStatus.APPROVED))
        {
            ito.Status = ItoStatus.READY_TO_DISPATCH;
            ito.ApprovedAt = DateTime.UtcNow;
            Event(ito, Actor(session), "READY_TO_DISPATCH", null);
        }
        ito.UpdatedAt = DateTime.UtcNow;
        await db.SaveChangesAsync(cancellationToken);
        return await GetAsync(organizationId, ito.Id, permissions, cancellationToken);
    }

    public async Task<ItoDetail> RejectAsync(Session session, Guid organizationId, Guid id, string? comment, IReadOnlySet<string> permissions, CancellationToken cancellationToken)
    {
        var ito = await Load(organizationId, id, cancellationToken);
        var pending = ito.Approvals.FirstOrDefault(item => item.Status == ItoApprovalStatus.PENDING);
        if (pending is not null)
        {
            pending.Status = ItoApprovalStatus.REJECTED;
            pending.Comment = comment;
            pending.ActorUserId = Actor(session);
            pending.ActedAt = DateTime.UtcNow;
        }
        ito.Status = ItoStatus.REJECTED;
        ito.UpdatedAt = DateTime.UtcNow;
        Event(ito, Actor(session), "REJECTED", comment);
        await db.SaveChangesAsync(cancellationToken);
        return await GetAsync(organizationId, ito.Id, permissions, cancellationToken);
    }

    public async Task<ItoDetail> DispatchAsync(Session session, Guid organizationId, Guid id, ItoQtyRequest request, IReadOnlySet<string> permissions, CancellationToken cancellationToken)
    {
        var ito = await Load(organizationId, id, cancellationToken);
        if (ito.Status is not ItoStatus.READY_TO_DISPATCH and not ItoStatus.APPROVED)
            throw new RecipeManagementException("INVALID_STATUS", "This transfer is not ready to dispatch.");
        if (!ito.FromLocation.TransferEnabled) throw new RecipeManagementException("TRANSFER_DISABLED", "Source location cannot transfer.");
        foreach (var line in ito.Lines)
        {
            var approved = line.ApprovedQty > 0 ? line.ApprovedQty : line.RequestedQty;
            var dispatchQty = LineQty(ito, line, request.LineId, request.Quantity, approved);
            if (dispatchQty is null) continue;
            if (dispatchQty <= 0) throw new RecipeManagementException("INVALID_QUANTITY", "Dispatch quantity must be greater than zero.");
            await Move(session, ito, line, ito.FromInventoryLocationId, dispatchQty.Value, ito.Mode == ItoMode.QUICK ? InventoryTxnType.QUICK_TRANSFER_OUT : InventoryTxnType.TRANSFER_OUT, InventoryDirection.OUT, true, request.ConfirmAlreadyCollected, cancellationToken);
            line.DispatchedQty = dispatchQty.Value;
            line.TransferValue = (line.UnitCost ?? 0) * dispatchQty.Value;
        }
        if (ito.Lines.All(item => item.DispatchedQty <= 0))
            throw new RecipeManagementException("INVALID_QUANTITY", "Dispatch quantity must be greater than zero.");
        ito.Status = ItoStatus.IN_TRANSIT;
        ito.DispatchedBy = Actor(session);
        ito.DispatchedAt = DateTime.UtcNow;
        ito.UpdatedAt = DateTime.UtcNow;
        ito.TotalValue = ito.Lines.Sum(item => item.TransferValue);
        Event(ito, Actor(session), "DISPATCHED", request.Comment);
        await db.SaveChangesAsync(cancellationToken);
        return await GetAsync(organizationId, ito.Id, permissions, cancellationToken);
    }

    public async Task<ItoDetail> ReceiveAsync(Session session, Guid organizationId, Guid id, ItoQtyRequest request, IReadOnlySet<string> permissions, CancellationToken cancellationToken)
    {
        var ito = await Load(organizationId, id, cancellationToken);
        if (ito.Status is not ItoStatus.IN_TRANSIT and not ItoStatus.DISPATCHED)
            throw new RecipeManagementException("INVALID_STATUS", "This transfer is not in transit.");
        var discrepancy = false;
        foreach (var line in ito.Lines)
        {
            var received = LineQty(ito, line, request.LineId, request.Quantity, line.DispatchedQty);
            if (received is null) continue;
            if (received < 0) throw new RecipeManagementException("INVALID_QUANTITY", "Received quantity cannot be negative.");
            await Move(session, ito, line, ito.ToInventoryLocationId, received.Value, ito.Mode == ItoMode.QUICK ? InventoryTxnType.QUICK_TRANSFER_IN : InventoryTxnType.TRANSFER_IN, InventoryDirection.IN, false, false, cancellationToken);
            await AdjustInTransit(ito.ToInventoryLocationId, line.MaterialId, -line.DispatchedQty, cancellationToken);
            line.ReceivedQty = received.Value;
            if (received != line.DispatchedQty) discrepancy = true;
        }
        ito.ReceivedBy = Actor(session);
        ito.ReceivedAt = DateTime.UtcNow;
        ito.UpdatedAt = DateTime.UtcNow;
        if (discrepancy)
        {
            ito.Status = ItoStatus.DISCREPANCY;
            Event(ito, Actor(session), "DISCREPANCY", request.Comment);
            var variance = ito.Lines.Sum(item => item.DispatchedQty - item.ReceivedQty);
            await workspace.RaiseAlert(organizationId, Actor(session), InventoryAlertKind.TRANSFER_DISCREPANCY, InventoryAlertSeverity.HIGH,
                $"Transfer discrepancy {ito.ItoNumber}", $"Dispatched and received quantities differ by {variance}.", ito.ToInventoryLocationId, ito.Lines.First().MaterialId, ito.Id, "ITO", InventoryNextActionType.REVIEW_TRANSFER, cancellationToken);
        }
        else
        {
            ito.Status = ItoStatus.COMPLETED;
            ito.CompletedAt = DateTime.UtcNow;
            Event(ito, Actor(session), "RECEIVED", request.Comment);
        }
        await db.SaveChangesAsync(cancellationToken);
        return await GetAsync(organizationId, ito.Id, permissions, cancellationToken);
    }

    public async Task<ItoDetail> ReportDiscrepancyAsync(Session session, Guid organizationId, Guid id, string? comment, IReadOnlySet<string> permissions, CancellationToken cancellationToken)
    {
        var ito = await Load(organizationId, id, cancellationToken);
        ito.Status = ItoStatus.DISCREPANCY;
        ito.UpdatedAt = DateTime.UtcNow;
        Event(ito, Actor(session), "DISCREPANCY_REPORTED", comment);
        await workspace.RaiseAlert(organizationId, Actor(session), InventoryAlertKind.TRANSFER_DISCREPANCY, InventoryAlertSeverity.HIGH,
            $"Transfer discrepancy {ito.ItoNumber}", comment, ito.ToInventoryLocationId, ito.Lines.FirstOrDefault()?.MaterialId, ito.Id, "ITO", InventoryNextActionType.REVIEW_TRANSFER, cancellationToken);
        await db.SaveChangesAsync(cancellationToken);
        return await GetAsync(organizationId, ito.Id, permissions, cancellationToken);
    }

    private async Task SubmitCreated(Session session, InternalTransferOrder ito, InventoryLocation from, InventoryLocation to, QuickTransferPolicy policy, CancellationToken cancellationToken)
    {
        ito.Status = ItoStatus.SUBMITTED;
        Event(ito, Actor(session), "SUBMITTED", null);
        if (ito.Mode == ItoMode.QUICK && policy.SkipManagerApproval)
        {
            foreach (var line in ito.Lines) line.ApprovedQty = line.RequestedQty;
            ito.Status = ItoStatus.READY_TO_DISPATCH;
            ito.ApprovedAt = DateTime.UtcNow;
            Event(ito, Actor(session), "QUICK_READY", "Manager approval skipped by Quick Transfer policy.");
            return;
        }
        ito.Status = ItoStatus.PENDING_APPROVAL;
        var requested = ito.Lines.Sum(item => item.RequestedQty);
        var available = await Available(from.Id, ito.Lines.First().MaterialId, cancellationToken);
        ito.Approvals.Add(Approval(ito.Id, ItoApprovalSide.SOURCE, from.ManagerGroup, requested, available));
        if (ito.Mode == ItoMode.STANDARD || policy.DestinationConfirmationRequired)
            ito.Approvals.Add(Approval(ito.Id, ItoApprovalSide.DESTINATION, to.ManagerGroup, requested, null));
        Event(ito, Actor(session), "PENDING_APPROVAL", null);
        await Task.CompletedTask;
    }

    public async Task<ItoDetail> DisputeAlreadyCollectedAsync(Session session, Guid organizationId, Guid id, string? comment, IReadOnlySet<string> permissions, CancellationToken cancellationToken)
    {
        var ito = await Load(organizationId, id, cancellationToken);
        if (!ito.AlreadyCollected)
            throw new RecipeManagementException("NOT_ALREADY_COLLECTED", "This transfer is not marked already collected.");
        ito.Status = ItoStatus.DISCREPANCY;
        ito.UpdatedAt = DateTime.UtcNow;
        Event(ito, Actor(session), "DISPUTE_ALREADY_COLLECTED", comment);
        await workspace.RaiseAlert(organizationId, Actor(session), InventoryAlertKind.TRANSFER_DISCREPANCY, InventoryAlertSeverity.HIGH,
            $"Already-collected dispute {ito.ItoNumber}", comment, ito.FromInventoryLocationId, ito.Lines.FirstOrDefault()?.MaterialId, ito.Id, "ITO", InventoryNextActionType.REVIEW_TRANSFER, cancellationToken);
        await db.SaveChangesAsync(cancellationToken);
        return await GetAsync(organizationId, ito.Id, permissions, cancellationToken);
    }

    private static InternalTransferApproval Approval(Guid itoId, ItoApprovalSide side, string? group, decimal requested, decimal? available) => new()
    {
        Id = Guid.NewGuid(), ItoId = itoId, Side = side, ManagerGroup = group, Status = ItoApprovalStatus.PENDING,
        RequestedQty = requested, AvailableQty = available, StockAfter = available is null ? null : available - requested
    };

    private void ValidateQuick(QuickTransferPolicy policy, InventoryLocation from, InventoryLocation to, IReadOnlyList<ItoLineInput> lines)
    {
        if (!policy.Enabled) throw new RecipeManagementException("QUICK_TRANSFER_DISABLED", "Quick Transfer is not enabled.");
        if (!policy.AllowedSourceLocationTypes.Contains(from.LocationType.ToString(), StringComparison.OrdinalIgnoreCase))
            throw new RecipeManagementException("QUICK_SOURCE_TYPE", "Source location type is not allowed for Quick Transfer.");
        if (!policy.AllowedDestinationLocationTypes.Contains(to.LocationType.ToString(), StringComparison.OrdinalIgnoreCase))
            throw new RecipeManagementException("QUICK_DESTINATION_TYPE", "Destination location type is not allowed for Quick Transfer.");
        var fromProperty = from.LocationType == InventoryLocationType.PROPERTY ? from.Id : from.PropertyLocationId;
        var toProperty = to.LocationType == InventoryLocationType.PROPERTY ? to.Id : to.PropertyLocationId;
        if (fromProperty == toProperty && !policy.SamePropertyAllowed)
            throw new RecipeManagementException("QUICK_SAME_PROPERTY", "Same-property Quick Transfer is not allowed.");
        if (fromProperty != toProperty && !policy.CrossPropertyAllowed)
            throw new RecipeManagementException("QUICK_CROSS_PROPERTY", "Cross-property Quick Transfer is not allowed.");
        var qty = lines.Sum(item => item.Quantity);
        if (policy.MaximumQuantity is decimal maxQty && qty > maxQty)
            throw new RecipeManagementException("QUICK_MAX_QTY", "Quantity exceeds the Quick Transfer maximum.");
    }

    private async Task Move(Session session, InternalTransferOrder ito, InternalTransferLine line, Guid locationId, decimal qty, InventoryTxnType type, InventoryDirection direction, bool decreaseSource, bool confirmAlreadyCollected, CancellationToken cancellationToken)
    {
        var material = await db.Materials.SingleAsync(item => item.Id == line.MaterialId, cancellationToken);
        if (material.InventoryType == InventoryItemType.SERVICE || !MaterialNormalized.CanHoldStock(material))
            throw new RecipeManagementException("SERVICE_NO_STOCK", "SERVICE materials must never create physical inventory.");
        var balance = await db.InventoryBalances.SingleOrDefaultAsync(item => item.MaterialId == line.MaterialId && item.InventoryLocationId == locationId && item.BatchId == null, cancellationToken);
        if (balance is null)
        {
            balance = new InventoryBalance
            {
                Id = Guid.NewGuid(), OrganizationId = ito.OrganizationId, MaterialId = line.MaterialId, InventoryLocationId = locationId,
                OnHandQty = 0, ReservedQty = 0, AvailableQty = 0, InTransitQty = 0, BaseUom = material.BaseUom, Currency = material.Currency, UpdatedAt = DateTime.UtcNow
            };
            db.InventoryBalances.Add(balance);
        }
        if (direction == InventoryDirection.OUT)
        {
            if (balance.AvailableQty < qty)
            {
                if (!ito.AlreadyCollected)
                    throw new RecipeManagementException("INSUFFICIENT_STOCK", $"Available quantity at source is {balance.AvailableQty} {balance.BaseUom}.");
                if (!confirmAlreadyCollected)
                    throw new RecipeManagementException("ALREADY_COLLECTED_CONFIRM_REQUIRED", "Source stock is insufficient. Confirm already-collected handover or dispute.");
                await workspace.RaiseAlert(ito.OrganizationId, Actor(session), InventoryAlertKind.NEGATIVE_STOCK, InventoryAlertSeverity.CRITICAL,
                    "Negative stock after already-collected handover", $"Dispatch of {qty} exceeds available {balance.AvailableQty} {balance.BaseUom}.", locationId, line.MaterialId, ito.Id, "ITO", InventoryNextActionType.INVESTIGATE, cancellationToken);
            }
            balance.OnHandQty -= qty;
            if (decreaseSource)
            {
                var dest = await EnsureBalance(ito.OrganizationId, line.MaterialId, ito.ToInventoryLocationId, material, cancellationToken);
                dest.InTransitQty += qty;
                dest.UpdatedAt = DateTime.UtcNow;
            }
        }
        else
        {
            balance.OnHandQty += qty;
        }
        balance.AvailableQty = balance.OnHandQty - balance.ReservedQty;
        balance.InventoryValue = (line.UnitCost ?? 0) * balance.OnHandQty;
        balance.LastMovementAt = DateTime.UtcNow;
        balance.UpdatedAt = DateTime.UtcNow;
        db.InventoryStockTransactions.Add(new InventoryStockTransaction
        {
            Id = Guid.NewGuid(), OrganizationId = ito.OrganizationId, TransactionId = $"IT{DateTime.UtcNow:yyyyMMddHHmmss}{Guid.NewGuid().ToString("N")[..6].ToUpperInvariant()}",
            TransactionType = type, MaterialId = line.MaterialId, InventoryLocationId = locationId, Quantity = qty, Uom = line.Uom,
            BaseQuantity = qty, BaseUom = line.BaseUom, Direction = direction, UnitCost = line.UnitCost, TransactionValue = (line.UnitCost ?? 0) * qty,
            Currency = ito.Currency, ReferenceType = "ITO", ReferenceId = ito.Id, BusinessDate = ito.BusinessDate, PostingDate = DateTime.UtcNow,
            Source = ito.Mode == ItoMode.QUICK ? "QUICK_ITO" : "STANDARD_ITO", CreatedBy = Actor(session), CreatedAt = DateTime.UtcNow
        });
    }

    private async Task<InventoryBalance> EnsureBalance(Guid organizationId, Guid materialId, Guid locationId, Material material, CancellationToken cancellationToken)
    {
        var balance = await db.InventoryBalances.SingleOrDefaultAsync(item => item.MaterialId == materialId && item.InventoryLocationId == locationId && item.BatchId == null, cancellationToken);
        if (balance is not null) return balance;
        balance = new InventoryBalance
        {
            Id = Guid.NewGuid(), OrganizationId = organizationId, MaterialId = materialId, InventoryLocationId = locationId,
            OnHandQty = 0, ReservedQty = 0, AvailableQty = 0, InTransitQty = 0, BaseUom = material.BaseUom, Currency = material.Currency, UpdatedAt = DateTime.UtcNow
        };
        db.InventoryBalances.Add(balance);
        return balance;
    }

    private async Task AdjustInTransit(Guid locationId, Guid materialId, decimal delta, CancellationToken cancellationToken)
    {
        var balance = await db.InventoryBalances.SingleOrDefaultAsync(item => item.MaterialId == materialId && item.InventoryLocationId == locationId && item.BatchId == null, cancellationToken);
        if (balance is null) return;
        balance.InTransitQty = Math.Max(0, balance.InTransitQty + delta);
        balance.UpdatedAt = DateTime.UtcNow;
    }

    private async Task<decimal> Available(Guid locationId, Guid materialId, CancellationToken cancellationToken)
    {
        var balance = await db.InventoryBalances.AsNoTracking().SingleOrDefaultAsync(item => item.MaterialId == materialId && item.InventoryLocationId == locationId && item.BatchId == null, cancellationToken);
        return balance?.AvailableQty ?? 0;
    }

    private async Task<Dictionary<Guid, InventoryBalance>> SourceStock(InternalTransferOrder ito, CancellationToken cancellationToken)
    {
        var ids = ito.Lines.Select(item => item.MaterialId).ToList();
        var rows = await db.InventoryBalances.AsNoTracking()
            .Where(item => item.InventoryLocationId == ito.FromInventoryLocationId && ids.Contains(item.MaterialId))
            .ToListAsync(cancellationToken);
        return rows.GroupBy(item => item.MaterialId)
            .ToDictionary(group => group.Key, group => group.OrderBy(item => item.BatchId == null ? 0 : 1).First());
    }

    private async Task<InternalTransferOrder> Load(Guid organizationId, Guid id, CancellationToken cancellationToken) =>
        await db.InternalTransferOrders
            .Include(item => item.FromLocation)
            .Include(item => item.ToLocation)
            .Include(item => item.Lines).ThenInclude(item => item.Material)
            .Include(item => item.Approvals)
            .SingleOrDefaultAsync(item => item.Id == id && item.OrganizationId == organizationId, cancellationToken)
        ?? throw new RecipeManagementException("ITO_NOT_FOUND", "The internal transfer was not found.", 404);

    private async Task<InventoryLocation> Location(Guid organizationId, Guid id, CancellationToken cancellationToken) =>
        await db.InventoryLocations.SingleOrDefaultAsync(item => item.Id == id && item.OrganizationId == organizationId, cancellationToken)
        ?? throw new RecipeManagementException("LOCATION_NOT_FOUND", "The inventory location was not found.", 404);

    private async Task<string> NextNumber(Guid organizationId, CancellationToken cancellationToken)
    {
        var prefix = $"ITO-{DateTime.UtcNow:yyyyMMdd}-";
        var last = await db.InternalTransferOrders.AsNoTracking()
            .Where(item => item.OrganizationId == organizationId && item.ItoNumber.StartsWith(prefix))
            .OrderByDescending(item => item.ItoNumber)
            .Select(item => item.ItoNumber)
            .FirstOrDefaultAsync(cancellationToken);
        var seq = 1;
        if (last is not null && int.TryParse(last[prefix.Length..], out var parsed)) seq = parsed + 1;
        return $"{prefix}{seq:0000}";
    }

    private void Event(InternalTransferOrder ito, Guid userId, string action, string? comment) =>
        db.InventoryWorkflowEvents.Add(new InventoryWorkflowEvent
        {
            Id = Guid.NewGuid(), OrganizationId = ito.OrganizationId, ReferenceType = "ITO", ReferenceId = ito.Id,
            Action = action, Comment = comment, ActorUserId = userId, CreatedAt = DateTime.UtcNow
        });

    private static Guid Actor(Session session) => session.UserId ?? Guid.Empty;

    private static string Relationship(InventoryLocation from, InventoryLocation to) => $"{from.LocationType} → {to.LocationType}";

    private static ItoDetail ToDetail(InternalTransferOrder item, IReadOnlyDictionary<Guid, InventoryBalance> stock, IReadOnlySet<string> permissions, IReadOnlyList<InventoryWorkflowEvent> events)
    {
        var actions = new List<string>();
        if (item.Status is ItoStatus.PENDING_APPROVAL or ItoStatus.SUBMITTED && Has(permissions, "APPROVE_STOCK_TRANSFER"))
        {
            actions.Add("APPROVE");
            actions.Add("REJECT");
        }
        if (item.Status is ItoStatus.READY_TO_DISPATCH or ItoStatus.APPROVED && Has(permissions, "DISPATCH_INTERNAL_TRANSFER", "DISPATCH_STOCK_TRANSFER"))
        {
            actions.Add("DISPATCH");
            actions.Add("CONFIRM_HANDOVER");
            if (item.AlreadyCollected)
            {
                actions.Add("CONFIRM_ALREADY_COLLECTED");
                actions.Add("DISPUTE_ALREADY_COLLECTED");
            }
        }
        if (item.Status is ItoStatus.IN_TRANSIT or ItoStatus.DISPATCHED && Has(permissions, "RECEIVE_INTERNAL_TRANSFER", "RECEIVE_STOCK_TRANSFER"))
        {
            actions.Add("RECEIVE");
            actions.Add("REPORT_DISCREPANCY");
        }
        var lines = item.Lines.Select(line =>
        {
            stock.TryGetValue(line.MaterialId, out var balance);
            var available = balance?.AvailableQty;
            var after = available is null ? null : available - (line.ApprovedQty > 0 ? line.ApprovedQty : line.RequestedQty);
            return new ItoLineRow(line.Id, line.MaterialId, line.Material.MaterialCode, line.Material.Description, line.RequestedQty, line.ApprovedQty,
                line.DispatchedQty, line.ReceivedQty, line.DispatchedQty - line.ReceivedQty, line.Uom, line.UnitCost, line.TransferValue, available, after);
        }).ToList();
        return new ItoDetail(
            item.Id, item.ItoNumber, item.Status.ToString(), item.Mode.ToString(), Relationship(item.FromLocation, item.ToLocation), item.AlreadyCollected,
            item.FromInventoryLocationId, item.FromLocation.LocationName, item.FromLocation.LocationType.ToString(), item.FromLocation.PropertyLocationId?.ToString(),
            item.ToInventoryLocationId, item.ToLocation.LocationName, item.ToLocation.LocationType.ToString(), item.ToLocation.PropertyLocationId?.ToString(),
            item.Reason, item.RequiredBy, item.BusinessDate, item.TotalValue, item.Currency, lines,
            item.Approvals.Select(row => new ItoApprovalRow(row.Id, row.Side.ToString(), row.Status.ToString(), row.ManagerGroup, row.AvailableQty, row.RequestedQty, row.ApprovedQty, row.StockAfter, row.Comment, row.ActedAt)).ToList(),
            events.Select(row => new ItoEventRow(row.Id, row.Action, row.Comment, row.CreatedAt)).ToList(),
            actions);
    }

    private static bool Has(IReadOnlySet<string> permissions, params string[] keys) => keys.Any(permissions.Contains);

    private async Task EnsureManagerGroup(Session session, Guid organizationId, InternalTransferApproval pending, CancellationToken cancellationToken)
    {
        if (session.IsSupportSession || string.IsNullOrWhiteSpace(pending.ManagerGroup)) return;
        var hasRole = await db.UserRoleAssignments.AsNoTracking()
            .AnyAsync(item => item.UserId == session.UserId && item.OrganizationId == organizationId && item.Status == StatusKind.ACTIVE && item.Role.Key == pending.ManagerGroup, cancellationToken);
        if (!hasRole)
            throw new RecipeManagementException("MANAGER_GROUP_REQUIRED", $"Approval requires the {pending.ManagerGroup} role.", 403);
    }

    private static void ApplyLineQty(InternalTransferOrder ito, Guid? lineId, decimal qty, Action<InternalTransferLine, decimal> apply)
    {
        foreach (var line in ito.Lines)
        {
            var value = LineQty(ito, line, lineId, qty, line.RequestedQty);
            if (value is not null) apply(line, value.Value);
        }
    }

    private static decimal? LineQty(InternalTransferOrder ito, InternalTransferLine line, Guid? lineId, decimal requested, decimal fallback)
    {
        if (lineId is Guid id) return line.Id == id ? (requested > 0 ? requested : fallback) : null;
        if (ito.Lines.Count == 1 && requested > 0) return requested;
        return fallback;
    }
}
