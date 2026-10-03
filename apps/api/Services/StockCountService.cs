using Microsoft.EntityFrameworkCore;
using SilaMe.Api.Auth;
using SilaMe.Api.Data;
using SilaMe.Api.Models;

namespace SilaMe.Api.Services;

public sealed record StockCountAccess(Session Session, bool CanCreate, bool CanEnter, bool CanReview, bool CanPost)
{
    public Guid Actor => Session.UserId ?? Guid.Empty;
    public bool CanSeeBook => CanReview || CanPost;
}

public sealed class StockCountService(SilaMeDbContext db, IntegrationService integrations)
{
    public async Task<StockCountDetail> CreateAsync(StockCountAccess access, Guid organizationId, StockCountCreateRequest request, CancellationToken cancellationToken)
    {
        if (!access.CanCreate) throw new RecipeManagementException("FORBIDDEN", "You cannot create a stock count.", 403);
        var location = await db.InventoryLocations.Include(item => item.PropertyLocation)
            .SingleOrDefaultAsync(item => item.Id == request.InventoryLocationId && item.OrganizationId == organizationId, cancellationToken)
            ?? throw new RecipeManagementException("LOCATION_NOT_FOUND", "The inventory location was not found.", 404);
        if (!Enum.TryParse<StockCountType>(request.CountType, true, out var countType))
            throw new RecipeManagementException("COUNT_TYPE", "Count type must be MONTHLY, PERIODIC, SURPRISE, or ADHOC.");
        if (countType == StockCountType.SURPRISE && !access.CanReview)
            throw new RecipeManagementException("SURPRISE_COUNT_FORBIDDEN", "Surprise counts require inventory-count approval permission.", 403);
        var propertyId = location.LocationType == InventoryLocationType.PROPERTY ? location.Id : location.PropertyLocationId;
        var assignments = await db.MaterialLocations.Include(item => item.Material)
            .Where(item => item.OrganizationId == organizationId && item.InventoryLocationId == location.Id && item.Active && item.StockingStatus == MaterialStockingStatus.ACTIVE)
            .ToListAsync(cancellationToken);
        var stocked = assignments.Where(item => item.Material.Status == StatusKind.ACTIVE && MaterialNormalized.CanHoldStock(item.Material)).ToList();
        if (stocked.Count == 0)
            throw new RecipeManagementException("NO_COUNT_LINES", "This location has no active inventory-managed materials.");
        var materialIds = stocked.Select(item => item.MaterialId).ToList();
        var balances = await db.InventoryBalances.AsNoTracking()
            .Where(item => item.InventoryLocationId == location.Id && materialIds.Contains(item.MaterialId) && item.BatchId == null)
            .ToDictionaryAsync(item => item.MaterialId, cancellationToken);
        var now = DateTime.UtcNow;
        var businessDate = DateTime.SpecifyKind(request.BusinessDate.Date, DateTimeKind.Utc);
        var session = new StockCountSession
        {
            Id = Guid.NewGuid(), OrganizationId = organizationId, CountNumber = await NextNumber(organizationId, "SC", businessDate, cancellationToken),
            CountType = countType, PropertyId = propertyId, InventoryLocationId = location.Id, BusinessDate = businessDate,
            BlindCount = request.BlindCount, Status = StockCountStatus.PLANNED, CreatedBy = access.Actor, CreatedAt = now, Notes = Trim(request.Notes, 2000),
        };
        foreach (var assignment in stocked.OrderBy(item => item.Material.MaterialCode))
        {
            balances.TryGetValue(assignment.MaterialId, out var balance);
            var cost = assignment.Material.UnitCost ?? assignment.Material.MovingAveragePrice ?? assignment.Material.StandardPrice;
            session.Lines.Add(new StockCountLine
            {
                Id = Guid.NewGuid(), MaterialId = assignment.MaterialId, InventoryLocationId = location.Id,
                SystemQty = StockCountMath.RoundQty(balance?.OnHandQty ?? 0), SystemUom = assignment.Material.BaseUom,
                BaseUom = assignment.Material.BaseUom, UnitCost = cost, Currency = balance?.Currency ?? assignment.Material.Currency ?? location.Currency,
                Status = StockCountLineStatus.NOT_COUNTED,
            });
        }
        db.StockCountSessions.Add(session);
        Audit(organizationId, "STOCK_COUNT", session.Id, "CREATED", access.Actor, $"{session.CountNumber} {location.LocationName}");
        await db.SaveChangesAsync(cancellationToken);
        return await GetAsync(access, organizationId, session.Id, null, null, cancellationToken);
    }

    public async Task<IReadOnlyList<StockCountListRow>> ListAsync(Guid organizationId, string? status, string? countType, Guid? propertyId, Guid? locationId, DateTime? from, DateTime? to, CancellationToken cancellationToken)
    {
        var query = db.StockCountSessions.AsNoTracking().Include(item => item.InventoryLocation).Include(item => item.PropertyLocation)
            .Where(item => item.OrganizationId == organizationId);
        if (!string.IsNullOrWhiteSpace(status) && Enum.TryParse<StockCountStatus>(status, true, out var parsedStatus)) query = query.Where(item => item.Status == parsedStatus);
        if (!string.IsNullOrWhiteSpace(countType) && Enum.TryParse<StockCountType>(countType, true, out var parsedType)) query = query.Where(item => item.CountType == parsedType);
        if (propertyId is { } property) query = query.Where(item => item.PropertyId == property);
        if (locationId is { } location) query = query.Where(item => item.InventoryLocationId == location);
        if (from is { } start) query = query.Where(item => item.BusinessDate >= start.Date);
        if (to is { } end) query = query.Where(item => item.BusinessDate < end.Date.AddDays(1));
        var sessions = await query.OrderByDescending(item => item.BusinessDate).ThenByDescending(item => item.CreatedAt).Take(200).ToListAsync(cancellationToken);
        var ids = sessions.Select(item => item.Id).ToList();
        var lines = await db.StockCountLines.AsNoTracking().Where(item => ids.Contains(item.StockCountSessionId))
            .Select(item => new { item.StockCountSessionId, item.Status, item.VarianceValue, item.Currency }).ToListAsync(cancellationToken);
        var names = await Names(sessions.Select(item => item.CreatedBy), cancellationToken);
        return sessions.Select(item =>
        {
            var rows = lines.Where(line => line.StockCountSessionId == item.Id).ToList();
            var shortageRows = rows.Where(line => line.VarianceValue < 0).ToList();
            var shortageCurrencies = shortageRows.Select(line => line.Currency).Where(value => !string.IsNullOrWhiteSpace(value)).Distinct().ToList();
            decimal? shortage = shortageCurrencies.Count == 1 ? shortageRows.Sum(line => -(line.VarianceValue ?? 0)) : null;
            return new StockCountListRow(item.Id, item.CountNumber, item.CountType.ToString(), item.PropertyLocation?.LocationName, item.InventoryLocation.LocationName,
                item.BusinessDate, rows.Count, rows.Count(line => line.Status != StockCountLineStatus.NOT_COUNTED && line.Status != StockCountLineStatus.RECOUNT_REQUIRED),
                rows.Count(line => line.Status == StockCountLineStatus.MATCHED), shortageRows.Count, rows.Count(line => line.VarianceValue > 0),
                shortage, shortageCurrencies.Count == 1 ? shortageCurrencies[0] : null, item.Status.ToString(), names.GetValueOrDefault(item.CreatedBy));
        }).ToList();
    }

    public Task<StockCountDetail> GetAsync(StockCountAccess access, Guid organizationId, Guid id, CancellationToken cancellationToken) =>
        GetAsync(access, organizationId, id, null, null, cancellationToken);

    public async Task<StockCountDetail> GetAsync(StockCountAccess access, Guid organizationId, Guid id, string? lineQuery, int? take, CancellationToken cancellationToken)
    {
        var session = await Load(organizationId, id, cancellationToken);
        var detail = await Map(session, access.CanSeeBook || !session.BlindCount, cancellationToken);
        if (lineQuery is null && take is null) return detail;
        IEnumerable<StockCountLineRow> lines = detail.Lines;
        if (!string.IsNullOrWhiteSpace(lineQuery))
        {
            var text = lineQuery.Trim();
            lines = lines.Where(item => item.MaterialCode.Contains(text, StringComparison.OrdinalIgnoreCase) || item.Description.Contains(text, StringComparison.OrdinalIgnoreCase));
        }
        return detail with { Lines = lines.Take(Math.Clamp(take ?? 40, 0, 80)).ToList() };
    }

    public async Task<StockCountDetail> CancelAsync(StockCountAccess access, Guid organizationId, Guid id, CancellationToken cancellationToken)
    {
        if (!access.CanCreate && !access.CanReview) throw new RecipeManagementException("FORBIDDEN", "You cannot cancel this stock count.", 403);
        var session = await Load(organizationId, id, cancellationToken);
        if (session.Lines.Any(item => item.Status == StockCountLineStatus.POSTED || item.SapStatus is StockSapStatus.POSTED or StockSapStatus.POSTING or StockSapStatus.POSTING_UNKNOWN))
            throw new RecipeManagementException("COUNT_LOCKED", "A posted or uncertain SAP adjustment cannot be cancelled.");
        session.Status = StockCountStatus.CANCELLED;
        Audit(organizationId, "STOCK_COUNT", session.Id, "CANCELLED", access.Actor, null);
        await db.SaveChangesAsync(cancellationToken);
        return await Map(session, true, cancellationToken);
    }

    public async Task<StockCountDetail> SubmitAsync(StockCountAccess access, Guid organizationId, Guid id, CancellationToken cancellationToken)
    {
        if (!access.CanEnter && !access.CanReview) throw new RecipeManagementException("FORBIDDEN", "You cannot submit this stock count.", 403);
        var session = await Load(organizationId, id, cancellationToken);
        var remaining = session.Lines.Count(item => item.Status is StockCountLineStatus.NOT_COUNTED or StockCountLineStatus.RECOUNT_REQUIRED);
        if (remaining > 0) throw new RecipeManagementException("COUNT_INCOMPLETE", $"{remaining} materials are still uncounted.");
        session.SubmittedBy = access.Actor;
        session.SubmittedAt = DateTime.UtcNow;
        Rollup(session);
        if (session.Status == StockCountStatus.IN_PROGRESS) session.Status = StockCountStatus.SUBMITTED;
        Audit(organizationId, "STOCK_COUNT", session.Id, "SUBMITTED", access.Actor, null);
        await db.SaveChangesAsync(cancellationToken);
        return await Map(session, access.CanSeeBook || !session.BlindCount, cancellationToken);
    }

    public async Task<IReadOnlyList<StockMaterialHit>> SearchAsync(Guid organizationId, Guid? locationId, Guid? sessionId, string? query, CancellationToken cancellationToken)
    {
        var text = query?.Trim() ?? "";
        if (text.Length < 1) return [];
        var upper = text.ToUpperInvariant();
        var barcodeHits = await db.MaterialBarcodes.AsNoTracking().Where(item => item.OrganizationId == organizationId && item.Active && item.Barcode == text)
            .Select(item => item.MaterialId).ToListAsync(cancellationToken);
        var materials = db.Materials.AsNoTracking().Where(item => item.OrganizationId == organizationId && item.Status == StatusKind.ACTIVE && item.InventoryItem && item.InventoryType == InventoryItemType.STOCK);
        Dictionary<Guid, Guid>? onCount = null;
        if (sessionId is { } sid)
        {
            onCount = await db.StockCountLines.AsNoTracking().Where(item => item.StockCountSessionId == sid && item.Session.OrganizationId == organizationId)
                .ToDictionaryAsync(item => item.MaterialId, item => item.Id, cancellationToken);
            var ids = onCount.Keys.ToList();
            materials = materials.Where(item => ids.Contains(item.Id));
        }
        else if (locationId is { } location)
        {
            var assigned = db.MaterialLocations.Where(item => item.OrganizationId == organizationId && item.InventoryLocationId == location && item.Active).Select(item => item.MaterialId);
            materials = materials.Where(item => assigned.Contains(item.Id));
        }
        var rows = await materials.Where(item => barcodeHits.Contains(item.Id) || item.MaterialCode.ToUpper() == upper || item.MaterialCode.ToUpper().Contains(upper) || item.Description.ToUpper().Contains(upper) || (item.MaterialGroup != null && item.MaterialGroup.ToUpper().Contains(upper)))
            .OrderBy(item => item.MaterialCode == upper ? 0 : barcodeHits.Contains(item.Id) ? 1 : 2).ThenBy(item => item.MaterialCode)
            .Take(20).ToListAsync(cancellationToken);
        return rows.Select(item => new StockMaterialHit(item.Id, item.MaterialCode, item.Description, item.MaterialGroup, item.BaseUom, item.ConvUnit, item.ConvValue, onCount != null && onCount.TryGetValue(item.Id, out var lineId) ? lineId : null)).ToList();
    }

    public async Task<StockIdentifyResult> IdentifyBarcodeAsync(StockCountAccess access, Guid organizationId, Guid sessionId, string barcode, CancellationToken cancellationToken)
    {
        var session = await Load(organizationId, sessionId, cancellationToken);
        var code = barcode.Trim();
        var mapped = await db.MaterialBarcodes.AsNoTracking().Include(item => item.Material)
            .Where(item => item.OrganizationId == organizationId && item.Active && item.Barcode == code).ToListAsync(cancellationToken);
        Audit(organizationId, "STOCK_COUNT", session.Id, "BARCODE_SCAN", access.Actor, code);
        await db.SaveChangesAsync(cancellationToken);
        if (mapped.Count == 0) return new StockIdentifyResult("BARCODE_NOT_MAPPED", "BARCODE NOT MAPPED", []);
        var hits = mapped.Where(item => session.Lines.Any(line => line.MaterialId == item.MaterialId))
            .Select(item => new StockMaterialHit(item.MaterialId, item.Material.MaterialCode, item.Material.Description, item.Material.MaterialGroup, item.Material.BaseUom, item.PackUom ?? item.Material.ConvUnit, item.PackQuantity ?? item.Material.ConvValue, session.Lines.First(line => line.MaterialId == item.MaterialId).Id)).ToList();
        if (hits.Count == 0) return new StockIdentifyResult("NOT_ON_COUNT", "This barcode is mapped, but the material is not on this count.", []);
        return new StockIdentifyResult("IDENTIFIED", null, hits);
    }

    public async Task<StockIdentifyResult> IdentifyPhotoAsync(StockCountAccess access, Guid organizationId, Guid sessionId, CancellationToken cancellationToken)
    {
        var session = await Load(organizationId, sessionId, cancellationToken);
        Audit(organizationId, "STOCK_COUNT", session.Id, "PHOTO_IDENTIFICATION", access.Actor, "No vision provider configured. The user must confirm the material.");
        await db.SaveChangesAsync(cancellationToken);
        // ponytail: no vision model in this repo. Candidates stay empty so a photo can never select a material or move stock.
        return new StockIdentifyResult("UNCONFIRMED", "Photo recorded. Confirm the material by search. Vision matching is not configured.", []);
    }

    public async Task<StockCountLineRow> SaveCountAsync(StockCountAccess access, Guid organizationId, Guid sessionId, Guid lineId, StockCountEntry request, CancellationToken cancellationToken)
    {
        if (!access.CanEnter && !access.CanReview) throw new RecipeManagementException("FORBIDDEN", "You cannot enter a physical count.", 403);
        var session = await Load(organizationId, sessionId, cancellationToken);
        if (session.Status is StockCountStatus.CANCELLED or StockCountStatus.COMPLETED)
            throw new RecipeManagementException("COUNT_CLOSED", "This count is closed.");
        var line = session.Lines.SingleOrDefault(item => item.Id == lineId) ?? throw new RecipeManagementException("LINE_NOT_FOUND", "The count line was not found.", 404);
        if (line.Status == StockCountLineStatus.POSTED || line.SapStatus is StockSapStatus.POSTED or StockSapStatus.POSTING or StockSapStatus.POSTING_UNKNOWN)
            throw new RecipeManagementException("LINE_LOCKED", "This line already has an SAP adjustment.");
        var material = line.Material;
        var conversions = await db.MaterialUomConversions.Where(item => item.MaterialId == material.Id).ToListAsync(cancellationToken);
        var fullUom = string.IsNullOrWhiteSpace(request.FullUom) ? material.BaseUom : request.FullUom.Trim();
        decimal physical = 0;
        if (request.FullQty is { } full)
            physical += StockCountMath.ConvertToBase(full, fullUom, material.BaseUom, conversions, material);
        if (request.OpenQty is > 0)
        {
            if (string.IsNullOrWhiteSpace(request.OpenUom)) throw new RecipeManagementException("OPEN_UOM_REQUIRED", "Open quantity needs a unit of measure.");
            physical += StockCountMath.ConvertToBase(request.OpenQty.Value, request.OpenUom, material.BaseUom, conversions, material);
        }
        physical = StockCountMath.RoundQty(physical);
        if (!Enum.TryParse<StockCountMethod>(request.Method, true, out var method)) method = StockCountMethod.MANUAL;
        var now = DateTime.UtcNow;
        var sequence = line.Captures.Count + 1;
        db.StockCountCaptures.Add(new StockCountCapture
        {
            Id = Guid.NewGuid(), StockCountLineId = line.Id, Sequence = sequence, FullQty = request.FullQty, FullUom = fullUom,
            OpenQty = request.OpenQty, OpenUom = request.OpenUom, ConvertedQty = physical, BaseUom = material.BaseUom,
            Method = method, CountedBy = access.Actor, CountedAt = now,
        });
        var variance = StockCountMath.Variance(physical, line.SystemQty);
        var classification = StockCountMath.Classify(variance);
        line.FullQty = request.FullQty;
        line.OpenQty = request.OpenQty;
        line.OpenUom = request.OpenUom;
        line.PhysicalQty = physical;
        line.PhysicalUom = material.BaseUom;
        line.ConvertedPhysicalQty = physical;
        line.BaseUom = material.BaseUom;
        line.VarianceQty = variance;
        line.VariancePercent = StockCountMath.Percent(variance, line.SystemQty);
        line.VarianceValue = line.UnitCost is null ? null : StockCountMath.RoundQty(variance * line.UnitCost.Value);
        line.CountMethod = method;
        line.CountedBy = access.Actor;
        line.CountedAt = now;
        line.Status = classification;
        if (session.StartedAt is null) { session.StartedAt = now; session.StartedBy = access.Actor; session.Status = StockCountStatus.IN_PROGRESS; }
        Audit(organizationId, "STOCK_COUNT_LINE", line.Id, sequence == 1 ? "PHYSICAL_COUNT" : "RECOUNT", access.Actor, $"{physical} {material.BaseUom} variance {variance}");
        if (classification == StockCountLineStatus.MATCHED)
        {
            line.SapStatus = StockSapStatus.NONE;
            if (line.Enquiry is not null && line.Enquiry.Status is not ShortageEnquiryStatus.CLOSED)
            {
                line.Enquiry.Status = ShortageEnquiryStatus.CLOSED;
                line.Enquiry.ClosedAt = now;
            }
            Audit(organizationId, "STOCK_COUNT_LINE", line.Id, "MATCHED", access.Actor, "Verified. No inventory transaction and no SAP posting.");
        }
        else if (classification == StockCountLineStatus.SHORTAGE)
        {
            await EnsureEnquiry(access, organizationId, session, line, cancellationToken);
            line.Status = StockCountLineStatus.ENQUIRY_REQUIRED;
        }
        else
        {
            line.Status = StockCountLineStatus.SURPLUS;
            line.SapStatus = StockSapStatus.NONE;
        }
        Rollup(session);
        await db.SaveChangesAsync(cancellationToken);
        var names = await Names([access.Actor], cancellationToken);
        return MapLine(line, access.CanSeeBook || !session.BlindCount, names);
    }

    public async Task<MaterialBarcodeRow> UpsertBarcodeAsync(StockCountAccess access, Guid organizationId, MaterialBarcodeRequest request, CancellationToken cancellationToken)
    {
        if (!access.CanCreate && !access.CanReview) throw new RecipeManagementException("FORBIDDEN", "You cannot map a barcode.", 403);
        var material = await db.Materials.SingleOrDefaultAsync(item => item.Id == request.MaterialId && item.OrganizationId == organizationId, cancellationToken)
            ?? throw new RecipeManagementException("MATERIAL_NOT_FOUND", "The material was not found.", 404);
        var code = request.Barcode.Trim();
        if (code.Length == 0) throw new RecipeManagementException("BARCODE_REQUIRED", "Barcode is required.");
        var existing = await db.MaterialBarcodes.SingleOrDefaultAsync(item => item.OrganizationId == organizationId && item.Barcode == code, cancellationToken);
        if (existing is not null && existing.MaterialId != material.Id)
            throw new RecipeManagementException("BARCODE_IN_USE", "This barcode is already mapped to another material.");
        existing ??= new MaterialBarcode { Id = Guid.NewGuid(), OrganizationId = organizationId, MaterialId = material.Id, Barcode = code, CreatedAt = DateTime.UtcNow };
        if (db.Entry(existing).State == EntityState.Detached) db.MaterialBarcodes.Add(existing);
        existing.MaterialId = material.Id;
        existing.BarcodeType = string.IsNullOrWhiteSpace(request.BarcodeType) ? "CODE128" : request.BarcodeType.Trim();
        existing.PackUom = request.PackUom;
        existing.PackQuantity = request.PackQuantity;
        existing.Active = request.Active;
        await db.SaveChangesAsync(cancellationToken);
        return new MaterialBarcodeRow(existing.Id, existing.MaterialId, material.MaterialCode, existing.Barcode, existing.BarcodeType, existing.PackUom, existing.PackQuantity, existing.Active);
    }

    public async Task<IReadOnlyList<StockTaskRow>> TasksAsync(StockCountAccess access, Guid organizationId, CancellationToken cancellationToken)
    {
        var roles = await db.UserRoleAssignments.AsNoTracking().Where(item => item.UserId == access.Actor && item.OrganizationId == organizationId && item.Status == StatusKind.ACTIVE)
            .Select(item => item.Role.Key).ToListAsync(cancellationToken);
        var locations = await db.UserInventoryLocations.AsNoTracking().Where(item => item.UserId == access.Actor && item.OrganizationId == organizationId)
            .Select(item => item.InventoryLocationId).ToListAsync(cancellationToken);
        var sessions = await db.StockCountSessions.AsNoTracking().Include(item => item.InventoryLocation).Include(item => item.Lines)
            .Where(item => item.OrganizationId == organizationId && item.Status != StockCountStatus.CANCELLED && item.Status != StockCountStatus.COMPLETED
                && (locations.Contains(item.InventoryLocationId) || item.CreatedBy == access.Actor))
            .OrderBy(item => item.BusinessDate).Take(50).ToListAsync(cancellationToken);
        var enquiries = await db.StockShortageEnquiries.AsNoTracking().Include(item => item.Line).ThenInclude(item => item.Material).Include(item => item.Line).ThenInclude(item => item.Session).ThenInclude(item => item.InventoryLocation)
            .Where(item => item.OrganizationId == organizationId && item.Status != ShortageEnquiryStatus.CLOSED && item.Status != ShortageEnquiryStatus.ACCEPTED && item.Status != ShortageEnquiryStatus.REJECTED
                && (item.AssignedManagerUserId == access.Actor || (item.AssignedManagerGroup != null && roles.Contains(item.AssignedManagerGroup)) || access.CanReview))
            .OrderByDescending(item => item.CreatedAt).Take(100).ToListAsync(cancellationToken);
        var tasks = new List<StockTaskRow>();
        foreach (var session in sessions)
        {
            var remaining = session.Lines.Count(item => item.Status is StockCountLineStatus.NOT_COUNTED or StockCountLineStatus.RECOUNT_REQUIRED);
            if (remaining > 0) tasks.Add(new StockTaskRow("STOCK_COUNT", session.Id, null, session.CountNumber, session.InventoryLocation.LocationName, $"{remaining} remaining", session.Status.ToString()));
            var recounts = session.Lines.Count(item => item.Status == StockCountLineStatus.RECOUNT_REQUIRED);
            if (recounts > 0) tasks.Add(new StockTaskRow("RECOUNT_REQUIRED", session.Id, null, session.CountNumber, session.InventoryLocation.LocationName, $"{recounts} recounts", "RECOUNT_REQUIRED"));
        }
        foreach (var enquiry in enquiries)
        {
            var kind = enquiry.Status == ShortageEnquiryStatus.MORE_INFORMATION_REQUIRED ? "INFORMATION_REQUESTED" : "SHORTAGE_ENQUIRY";
            tasks.Add(new StockTaskRow(kind, enquiry.StockCountSessionId, enquiry.Id, enquiry.EnquiryNumber, enquiry.Line.Session.InventoryLocation.LocationName, enquiry.Line.Material.Description, enquiry.Status.ToString()));
        }
        return tasks;
    }

    public async Task<StockEnquiryDetail> EnquiryAsync(StockCountAccess access, Guid organizationId, Guid id, CancellationToken cancellationToken)
    {
        var enquiry = await LoadEnquiry(organizationId, id, cancellationToken);
        return await MapEnquiry(enquiry, true, cancellationToken);
    }

    public async Task<StockEnquiryDetail> RespondAsync(StockCountAccess access, Guid organizationId, Guid id, StockJustificationRequest request, CancellationToken cancellationToken)
    {
        var enquiry = await LoadEnquiry(organizationId, id, cancellationToken);
        var roles = await db.UserRoleAssignments.AsNoTracking().Where(item => item.UserId == access.Actor && item.OrganizationId == organizationId && item.Status == StatusKind.ACTIVE).Select(item => item.Role.Key).ToListAsync(cancellationToken);
        var allowed = access.CanReview || enquiry.AssignedManagerUserId == access.Actor || (enquiry.AssignedManagerGroup != null && roles.Contains(enquiry.AssignedManagerGroup));
        if (!allowed) throw new RecipeManagementException("FORBIDDEN", "This enquiry is not assigned to you.", 403);
        if (string.IsNullOrWhiteSpace(request.Comments)) throw new RecipeManagementException("COMMENTS_REQUIRED", "Comments are required.");
        if (!Enum.TryParse<JustificationCategory>(request.Category, true, out var category))
            throw new RecipeManagementException("CATEGORY_REQUIRED", "Choose a justification category.");
        var now = DateTime.UtcNow;
        db.StockShortageMessages.Add(new StockShortageMessage
        {
            Id = Guid.NewGuid(), EnquiryId = enquiry.Id, Kind = "JUSTIFICATION", Category = category,
            Comments = request.Comments.Trim(), AttachmentName = Trim(request.AttachmentName, 260), ActorUserId = access.Actor, CreatedAt = now,
        });
        enquiry.Category = category;
        enquiry.Status = ShortageEnquiryStatus.RESPONDED;
        enquiry.RespondedAt = now;
        enquiry.Line.Status = StockCountLineStatus.UNDER_REVIEW;
        Rollup(enquiry.Line.Session);
        Audit(organizationId, "ENQUIRY", enquiry.Id, "MANAGER_RESPONSE", access.Actor, StockCountMath.Label(category));
        await db.SaveChangesAsync(cancellationToken);
        return await MapEnquiry(enquiry, true, cancellationToken);
    }

    public async Task<StockEnquiryDetail> ReviewAsync(StockCountAccess access, Guid organizationId, Guid lineId, StockReviewRequest request, CancellationToken cancellationToken)
    {
        if (!access.CanReview) throw new RecipeManagementException("FORBIDDEN", "You cannot review this count.", 403);
        var line = await db.StockCountLines.Include(item => item.Material).Include(item => item.Session).ThenInclude(item => item.InventoryLocation).ThenInclude(item => item.PropertyLocation)
            .Include(item => item.Enquiry).ThenInclude(item => item!.Messages)
            .SingleOrDefaultAsync(item => item.Id == lineId && item.Session.OrganizationId == organizationId, cancellationToken)
            ?? throw new RecipeManagementException("LINE_NOT_FOUND", "The count line was not found.", 404);
        var action = request.Action.Trim().ToUpperInvariant();
        var now = DateTime.UtcNow;
        var comment = string.IsNullOrWhiteSpace(request.Comment) ? action : request.Comment.Trim();
        if (line.Enquiry is not null)
        {
            db.StockShortageMessages.Add(new StockShortageMessage
            {
                Id = Guid.NewGuid(), EnquiryId = line.Enquiry.Id, Kind = "REVIEW", Comments = comment, ActorUserId = access.Actor, CreatedAt = now,
            });
        }
        line.Session.ReviewedBy = access.Actor;
        line.Session.ReviewedAt = now;
        switch (action)
        {
            case "ACCEPT":
                if (line.VarianceQty is null or 0) throw new RecipeManagementException("NO_VARIANCE", "A matched line is not adjusted.");
                if (line.Status == StockCountLineStatus.SHORTAGE || line.Status == StockCountLineStatus.ENQUIRY_REQUIRED)
                    throw new RecipeManagementException("JUSTIFICATION_REQUIRED", "A shortage needs a manager justification before adjustment.");
                line.Status = StockCountLineStatus.APPROVED;
                line.SapStatus = StockSapStatus.ADJUSTMENT_PENDING;
                if (line.Enquiry is not null) { line.Enquiry.Status = ShortageEnquiryStatus.ACCEPTED; line.Enquiry.ReviewedAt = now; }
                Audit(organizationId, "STOCK_COUNT_LINE", line.Id, "APPROVED", access.Actor, comment);
                if (access.CanPost) await PostSapAsync(access, organizationId, line, cancellationToken);
                break;
            case "MORE_INFORMATION":
                if (line.Enquiry is null) throw new RecipeManagementException("ENQUIRY_REQUIRED", "There is no enquiry to send back.");
                line.Enquiry.Status = ShortageEnquiryStatus.MORE_INFORMATION_REQUIRED;
                line.Status = StockCountLineStatus.ENQUIRY_REQUIRED;
                Audit(organizationId, "ENQUIRY", line.Enquiry.Id, "MORE_INFORMATION", access.Actor, comment);
                break;
            case "REJECT":
                line.Status = StockCountLineStatus.REJECTED;
                if (line.Enquiry is not null) { line.Enquiry.Status = ShortageEnquiryStatus.REJECTED; line.Enquiry.ReviewedAt = now; line.Enquiry.ClosedAt = now; }
                Audit(organizationId, "STOCK_COUNT_LINE", line.Id, "REJECTED", access.Actor, comment);
                break;
            case "RECOUNT":
                line.Status = StockCountLineStatus.RECOUNT_REQUIRED;
                if (line.Enquiry is not null) line.Enquiry.Status = ShortageEnquiryStatus.AWAITING_RESPONSE;
                Audit(organizationId, "STOCK_COUNT_LINE", line.Id, "RECOUNT_REQUIRED", access.Actor, comment);
                break;
            case "REQUEST_EXPLANATION":
                if (line.Enquiry is null) await EnsureEnquiry(access, organizationId, line.Session, line, cancellationToken);
                line.Status = StockCountLineStatus.ENQUIRY_REQUIRED;
                Audit(organizationId, "STOCK_COUNT_LINE", line.Id, "EXPLANATION_REQUESTED", access.Actor, comment);
                break;
            default:
                throw new RecipeManagementException("REVIEW_ACTION", "Action must be ACCEPT, MORE_INFORMATION, REJECT, RECOUNT, or REQUEST_EXPLANATION.");
        }
        Rollup(line.Session);
        await db.SaveChangesAsync(cancellationToken);
        return line.Enquiry is null
            ? new StockEnquiryDetail(Guid.Empty, "", line.Id, line.Status.ToString(), false, null, null, null, null, null, null, null, null, null, null, null, line.SapStatus.ToString(), line.SapMaterialDocument, [])
            : await MapEnquiry(line.Enquiry, true, cancellationToken);
    }

    public async Task<StockCountLineRow> ReprocessAsync(StockCountAccess access, Guid organizationId, Guid lineId, CancellationToken cancellationToken)
    {
        if (!access.CanPost) throw new RecipeManagementException("FORBIDDEN", "You cannot post an inventory adjustment.", 403);
        var line = await db.StockCountLines.Include(item => item.Material).Include(item => item.Session).ThenInclude(item => item.InventoryLocation).ThenInclude(item => item.PropertyLocation)
            .Include(item => item.Enquiry)
            .SingleOrDefaultAsync(item => item.Id == lineId && item.Session.OrganizationId == organizationId, cancellationToken)
            ?? throw new RecipeManagementException("LINE_NOT_FOUND", "The count line was not found.", 404);
        if (line.SapStatus == StockSapStatus.POSTING_UNKNOWN)
            throw new RecipeManagementException("POSTING_UNKNOWN", "SAP confirmation is unknown. This adjustment is not retried automatically.");
        if (line.SapStatus is not StockSapStatus.FAILED and not StockSapStatus.ADJUSTMENT_PENDING)
            throw new RecipeManagementException("NOT_REPROCESSABLE", "Only a failed or approved adjustment can be posted.");
        await PostSapAsync(access, organizationId, line, cancellationToken);
        Rollup(line.Session);
        await db.SaveChangesAsync(cancellationToken);
        return MapLine(line, true, await Names([line.CountedBy ?? Guid.Empty], cancellationToken));
    }

    public async Task<StockShortageReport> ShortagesAsync(Guid organizationId, StockReportFilter filter, CancellationToken cancellationToken)
    {
        var rows = await ReportLines(organizationId, filter, cancellationToken);
        return Summarize(rows, filter);
    }

    public async Task<IReadOnlyList<StockLedgerRow>> TransactionsAsync(Guid organizationId, CancellationToken cancellationToken)
    {
        var rows = await db.InventoryStockTransactions.AsNoTracking()
            .Where(item => item.OrganizationId == organizationId)
            .OrderByDescending(item => item.CreatedAt).Take(200).ToListAsync(cancellationToken);
        var materials = await db.Materials.AsNoTracking().Where(item => rows.Select(row => row.MaterialId).Contains(item.Id)).ToDictionaryAsync(item => item.Id, item => item.MaterialCode + " " + item.Description, cancellationToken);
        var locations = await db.InventoryLocations.AsNoTracking().Where(item => rows.Select(row => row.InventoryLocationId).Contains(item.Id)).ToDictionaryAsync(item => item.Id, item => item.LocationName, cancellationToken);
        return rows.Select(item => new StockLedgerRow(item.Id, item.TransactionId, item.TransactionType.ToString(), item.Direction.ToString(), materials.GetValueOrDefault(item.MaterialId), locations.GetValueOrDefault(item.InventoryLocationId), item.BaseQuantity, item.BaseUom, item.ReferenceType, item.ReferenceId, item.BusinessDate, item.CreatedAt)).ToList();
    }

    private async Task PostSapAsync(StockCountAccess access, Guid organizationId, StockCountLine line, CancellationToken cancellationToken)
    {
        if (line.SapStatus is StockSapStatus.POSTED or StockSapStatus.POSTING or StockSapStatus.POSTING_UNKNOWN)
            throw new RecipeManagementException("DUPLICATE_ADJUSTMENT", "This line already has an SAP posting in progress or completed.");
        if (line.VarianceQty is null || line.VarianceQty == 0) throw new RecipeManagementException("NO_VARIANCE", "There is no quantity to adjust.");
        var location = line.Session.InventoryLocation;
        var resolved = await integrations.TryResolveUpdateStockAsync(organizationId, location.CompanyCode, cancellationToken);
        var (plant, storage) = RecipeSapUpdateStock.ResolvePlantStorage(null, location, location.PropertyLocation?.CostCenter, resolved?.Configuration.Plant);
        if (resolved is null || string.IsNullOrWhiteSpace(plant) || string.IsNullOrWhiteSpace(storage))
        {
            line.SapStatus = StockSapStatus.FAILED;
            line.SapError = resolved is null ? "UPDATE_STOCK API route not configured" : "Plant or storage location mapping is missing.";
            Audit(organizationId, "STOCK_COUNT_LINE", line.Id, "SAP_FAILED", access.Actor, line.SapError);
            return;
        }
        var movement = line.VarianceQty < 0 ? RecipeSapUpdateStock.NegativeMovementType : RecipeSapUpdateStock.PositiveMovementType;
        var date = DateOnly.FromDateTime(line.Session.BusinessDate);
        var quantity = Math.Abs(line.VarianceQty.Value);
        var body = RecipeSapUpdateStock.BuildRequestJson(date, [new RecipeSapStockItem(date, plant, storage, line.Material.MaterialCode, movement, quantity, line.Material.BaseUom, [line.Id])]);
        line.SapStatus = StockSapStatus.POSTING;
        line.Session.Status = StockCountStatus.POSTING;
        await db.SaveChangesAsync(cancellationToken);
        var result = await integrations.PostJsonOnceAsync(resolved.Value.Configuration, body, cancellationToken);
        if (result.Unknown)
        {
            line.SapStatus = StockSapStatus.POSTING_UNKNOWN;
            line.SapError = result.ErrorMessage ?? "SAP posting confirmation unknown";
            Audit(organizationId, "STOCK_COUNT_LINE", line.Id, "SAP_UNKNOWN", access.Actor, line.SapError);
            return;
        }
        if (!result.Success)
        {
            line.SapStatus = StockSapStatus.FAILED;
            line.SapError = result.ErrorMessage ?? "SAP posting failed";
            Audit(organizationId, "STOCK_COUNT_LINE", line.Id, "SAP_FAILED", access.Actor, line.SapError);
            return;
        }
        ApplyLocal(organizationId, line, access.Actor);
        line.SapStatus = StockSapStatus.POSTED;
        line.Status = StockCountLineStatus.POSTED;
        line.SapMaterialDocument = result.MaterialDocument;
        line.SapError = null;
        if (line.Enquiry is not null) { line.Enquiry.Status = ShortageEnquiryStatus.CLOSED; line.Enquiry.ClosedAt = DateTime.UtcNow; }
        Audit(organizationId, "STOCK_COUNT_LINE", line.Id, "SAP_POSTED", access.Actor, $"{movement} {quantity} {line.Material.BaseUom} {result.MaterialDocument}");
    }

    private void ApplyLocal(Guid organizationId, StockCountLine line, Guid actor)
    {
        if (line.InventoryTransactionId is not null) return;
        var qty = Math.Abs(line.VarianceQty ?? 0);
        var direction = line.VarianceQty < 0 ? InventoryDirection.OUT : InventoryDirection.IN;
        var balance = db.InventoryBalances.SingleOrDefault(item => item.MaterialId == line.MaterialId && item.InventoryLocationId == line.InventoryLocationId && item.BatchId == null);
        if (balance is null)
        {
            balance = new InventoryBalance
            {
                Id = Guid.NewGuid(), OrganizationId = organizationId, MaterialId = line.MaterialId, InventoryLocationId = line.InventoryLocationId,
                BaseUom = line.Material.BaseUom, Currency = line.Currency, UpdatedAt = DateTime.UtcNow,
            };
            db.InventoryBalances.Add(balance);
        }
        balance.OnHandQty += direction == InventoryDirection.IN ? qty : -qty;
        balance.AvailableQty = balance.OnHandQty - balance.ReservedQty;
        balance.InventoryValue = (line.UnitCost ?? 0) * balance.OnHandQty;
        balance.LastMovementAt = DateTime.UtcNow;
        balance.UpdatedAt = DateTime.UtcNow;
        var txn = new InventoryStockTransaction
        {
            Id = Guid.NewGuid(), OrganizationId = organizationId,
            TransactionId = $"SC{DateTime.UtcNow:yyyyMMddHHmmss}{Guid.NewGuid().ToString("N")[..6].ToUpperInvariant()}",
            TransactionType = InventoryTxnType.STOCK_COUNT_ADJUSTMENT, MaterialId = line.MaterialId, InventoryLocationId = line.InventoryLocationId,
            Quantity = qty, Uom = line.BaseUom ?? line.Material.BaseUom, BaseQuantity = qty, BaseUom = line.Material.BaseUom,
            Direction = direction, UnitCost = line.UnitCost, TransactionValue = (line.UnitCost ?? 0) * qty, Currency = line.Currency,
            ReferenceType = "STOCK_COUNT_LINE", ReferenceId = line.Id, BusinessDate = line.Session.BusinessDate, PostingDate = DateTime.UtcNow,
            Source = "STOCK_COUNT", CreatedBy = actor, CreatedAt = DateTime.UtcNow,
        };
        db.InventoryStockTransactions.Add(txn);
        line.InventoryTransactionId = txn.Id;
    }

    private async Task EnsureEnquiry(StockCountAccess access, Guid organizationId, StockCountSession session, StockCountLine line, CancellationToken cancellationToken)
    {
        var shortage = StockCountMath.ShortageQty(line.VarianceQty ?? 0);
        var (userId, group, configured) = await ResolveManager(organizationId, session.InventoryLocation, cancellationToken);
        var enquiry = line.Enquiry;
        if (enquiry is null)
        {
            enquiry = new StockShortageEnquiry
            {
                Id = Guid.NewGuid(), OrganizationId = organizationId, EnquiryNumber = await NextNumber(organizationId, "EN", DateTime.UtcNow, cancellationToken),
                StockCountSessionId = session.Id, StockCountLineId = line.Id, MaterialId = line.MaterialId, InventoryLocationId = line.InventoryLocationId,
                Uom = line.SystemUom, CreatedAt = DateTime.UtcNow,
            };
            db.StockShortageEnquiries.Add(enquiry);
            line.Enquiry = enquiry;
            Audit(organizationId, "ENQUIRY", enquiry.Id, "CREATED", access.Actor, configured ? group : "MANAGER NOT CONFIGURED");
        }
        enquiry.SystemQty = line.SystemQty;
        enquiry.PhysicalQty = line.ConvertedPhysicalQty ?? 0;
        enquiry.ShortageQty = shortage == 0 ? Math.Abs(line.VarianceQty ?? 0) : shortage;
        enquiry.UnitCost = line.UnitCost;
        enquiry.ShortageValue = line.VarianceValue is < 0 ? -line.VarianceValue : line.VarianceValue;
        enquiry.Currency = line.Currency;
        enquiry.AssignedManagerUserId = userId;
        enquiry.AssignedManagerGroup = group;
        enquiry.ManagerConfigured = configured;
        if (enquiry.Status is ShortageEnquiryStatus.CREATED or ShortageEnquiryStatus.CLOSED)
            enquiry.Status = configured ? ShortageEnquiryStatus.AWAITING_RESPONSE : ShortageEnquiryStatus.CREATED;
    }

    private async Task<(Guid? UserId, string? Group, bool Configured)> ResolveManager(Guid organizationId, InventoryLocation location, CancellationToken cancellationToken)
    {
        var group = string.IsNullOrWhiteSpace(location.ManagerGroup) ? null : location.ManagerGroup.Trim();
        if (group is null) return (null, null, false);
        var assigned = await db.UserInventoryLocations.AsNoTracking().Where(item => item.OrganizationId == organizationId && item.InventoryLocationId == location.Id).Select(item => item.UserId).ToListAsync(cancellationToken);
        var users = await db.UserRoleAssignments.AsNoTracking().Include(item => item.User)
            .Where(item => item.OrganizationId == organizationId && item.Status == StatusKind.ACTIVE && item.Role.Key == group && item.User.Status == StatusKind.ACTIVE)
            .Select(item => item.UserId).ToListAsync(cancellationToken);
        var match = users.FirstOrDefault(item => assigned.Contains(item));
        if (match != Guid.Empty) return (match, group, true);
        if (users.Count > 0) return (users[0], group, true);
        return (null, group, false);
    }

    private async Task<List<StockReportLine>> ReportLines(Guid organizationId, StockReportFilter filter, CancellationToken cancellationToken)
    {
        var query = db.StockCountLines.AsNoTracking().Include(item => item.Material).Include(item => item.Session).ThenInclude(item => item.InventoryLocation).ThenInclude(item => item.PropertyLocation)
            .Include(item => item.Session).ThenInclude(item => item.InventoryLocation).ThenInclude(item => item.ParentLocation)
            .Include(item => item.Enquiry).ThenInclude(item => item!.Messages)
            .Where(item => item.Session.OrganizationId == organizationId && item.VarianceQty < 0);
        if (filter.From is { } from) query = query.Where(item => item.Session.BusinessDate >= from.Date);
        if (filter.To is { } to) query = query.Where(item => item.Session.BusinessDate < to.Date.AddDays(1));
        if (filter.PropertyId is { } property) query = query.Where(item => item.Session.PropertyId == property);
        if (filter.LocationId is { } location) query = query.Where(item => item.InventoryLocationId == location);
        if (!string.IsNullOrWhiteSpace(filter.MaterialGroup)) query = query.Where(item => item.Material.MaterialGroup == filter.MaterialGroup);
        if (!string.IsNullOrWhiteSpace(filter.Category) && Enum.TryParse<JustificationCategory>(filter.Category, true, out var category))
            query = query.Where(item => item.Enquiry != null && item.Enquiry.Category == category);
        if (!string.IsNullOrWhiteSpace(filter.EnquiryStatus) && Enum.TryParse<ShortageEnquiryStatus>(filter.EnquiryStatus, true, out var enquiryStatus))
            query = query.Where(item => item.Enquiry != null && item.Enquiry.Status == enquiryStatus);
        if (!string.IsNullOrWhiteSpace(filter.SapStatus) && Enum.TryParse<StockSapStatus>(filter.SapStatus, true, out var sap))
            query = query.Where(item => item.SapStatus == sap);
        var lines = await query.OrderByDescending(item => item.Session.BusinessDate).Take(2000).ToListAsync(cancellationToken);
        if (filter.Manager is { } manager && manager != Guid.Empty)
            lines = lines.Where(item => item.Enquiry?.AssignedManagerUserId == manager).ToList();
        var users = await Names(lines.SelectMany(item => new[] { item.CountedBy, item.Enquiry?.AssignedManagerUserId, item.Session.ReviewedBy }).OfType<Guid>(), cancellationToken);
        return lines.Select(item =>
        {
            var latest = item.Enquiry?.Messages.Where(message => message.Kind == "JUSTIFICATION").OrderByDescending(message => message.CreatedAt).FirstOrDefault();
            var review = item.Enquiry?.Messages.Where(message => message.Kind == "REVIEW").OrderByDescending(message => message.CreatedAt).FirstOrDefault();
            var propertyName = item.Session.PropertyLocation?.LocationName ?? item.Session.InventoryLocation.PropertyLocation?.LocationName;
            return new StockReportLine(propertyName, item.Session.InventoryLocation.ParentLocation?.LocationName, item.Session.InventoryLocation.LocationName,
                item.Enquiry?.AssignedManagerGroup, users.GetValueOrDefault(item.Enquiry?.AssignedManagerUserId ?? Guid.Empty), item.Session.CountNumber, item.Session.BusinessDate,
                item.Material.MaterialCode, item.Material.Description, item.Material.MaterialGroup, item.SystemQty, item.ConvertedPhysicalQty, StockCountMath.ShortageQty(item.VarianceQty ?? 0),
                item.SystemUom, item.UnitCost, item.VarianceValue is < 0 ? -item.VarianceValue : null, item.Currency,
                item.Enquiry?.Category is { } cat ? StockCountMath.Label(cat) : "Unexplained", latest?.Comments, latest?.CreatedAt,
                users.GetValueOrDefault(item.Session.ReviewedBy ?? Guid.Empty), review?.Comments, item.Session.ReviewedAt,
                item.SapStatus.ToString(), item.SapMaterialDocument, item.Enquiry?.EnquiryNumber, item.Enquiry?.Status.ToString() ?? "NONE", item.Status.ToString(), item.Session.InventoryLocationId);
        }).ToList();
    }

    private static StockShortageReport Summarize(IReadOnlyList<StockReportLine> rows, StockReportFilter filter)
    {
        decimal? Sum(Func<StockReportLine, bool> predicate)
        {
            var matched = rows.Where(predicate).Where(item => item.ShortageValue is not null).ToList();
            var currencies = matched.Select(item => item.Currency).Distinct().ToList();
            return currencies.Count == 1 ? matched.Sum(item => item.ShortageValue ?? 0) : null;
        }
        var currency = rows.Select(item => item.Currency).Where(item => !string.IsNullOrWhiteSpace(item)).Distinct().Count() == 1 ? rows.Select(item => item.Currency).FirstOrDefault(item => !string.IsNullOrWhiteSpace(item)) : null;
        var uom = rows.Select(item => item.Uom).Distinct().Count() == 1 ? rows.Select(item => item.Uom).FirstOrDefault() : null;
        var total = Sum(_ => true);
        var awaiting = Sum(item => item.EnquiryStatus is "CREATED" or "SENT" or "AWAITING_RESPONSE" or "MORE_INFORMATION_REQUIRED");
        var justified = Sum(item => item.Category != "Unexplained");
        var unresolved = Sum(item => item.Category == "Unexplained" || item.EnquiryStatus is "REJECTED");
        var approved = Sum(item => item.LineStatus is "APPROVED" or "POSTED");
        var posted = Sum(item => item.SapStatus == "POSTED");
        var failed = Sum(item => item.SapStatus is "FAILED" or "POSTING_UNKNOWN" or "ADJUSTMENT_PENDING" or "POSTING");
        var byLocation = rows.GroupBy(item => new { item.Property, item.Location, item.LocationId }).Select(group => new StockLocationTotal(group.Key.Property, group.Key.Location, group.Key.LocationId, group.Select(item => item.Currency).Distinct().Count() == 1 ? group.Sum(item => item.ShortageValue ?? 0) : null, group.Select(item => item.Currency).FirstOrDefault())).OrderByDescending(item => item.Value).ToList();
        var reasonBase = total is > 0 ? total.Value : 0;
        var byReason = rows.GroupBy(item => item.Category).Select(group =>
        {
            var value = group.Select(item => item.Currency).Distinct().Count() <= 1 ? group.Sum(item => item.ShortageValue ?? 0) : (decimal?)null;
            return new StockReasonTotal(group.Key, group.Count(), value, reasonBase > 0 && value is not null ? StockCountMath.RoundQty(value.Value / reasonBase * 100m) : null);
        }).OrderByDescending(item => item.Value).ToList();
        return new StockShortageReport(filter.From, filter.To, rows.Select(item => item.LocationId).Distinct().Count(), rows.Count, uom is null ? null : rows.Sum(item => item.ShortageQty), uom, currency, total, awaiting, justified, unresolved, approved, posted, failed, byLocation, byReason, rows);
    }

    private async Task<StockCountSession> Load(Guid organizationId, Guid id, CancellationToken cancellationToken) =>
        await db.StockCountSessions.Include(item => item.InventoryLocation).ThenInclude(item => item.PropertyLocation).Include(item => item.PropertyLocation)
            .Include(item => item.Lines).ThenInclude(item => item.Material)
            .Include(item => item.Lines).ThenInclude(item => item.Captures)
            .Include(item => item.Lines).ThenInclude(item => item.Enquiry)
            .SingleOrDefaultAsync(item => item.Id == id && item.OrganizationId == organizationId, cancellationToken)
        ?? throw new RecipeManagementException("COUNT_NOT_FOUND", "The stock count was not found.", 404);

    private async Task<StockShortageEnquiry> LoadEnquiry(Guid organizationId, Guid id, CancellationToken cancellationToken) =>
        await db.StockShortageEnquiries.Include(item => item.Messages).Include(item => item.Line).ThenInclude(item => item.Material)
            .Include(item => item.Line).ThenInclude(item => item.Session).ThenInclude(item => item.InventoryLocation)
            .SingleOrDefaultAsync(item => item.Id == id && item.OrganizationId == organizationId, cancellationToken)
        ?? throw new RecipeManagementException("ENQUIRY_NOT_FOUND", "The enquiry was not found.", 404);

    private async Task<string> NextNumber(Guid organizationId, string prefix, DateTime date, CancellationToken cancellationToken)
    {
        var key = $"{prefix}{date:yyyyMMdd}";
        var count = prefix == "SC"
            ? await db.StockCountSessions.CountAsync(item => item.OrganizationId == organizationId && item.CountNumber.StartsWith(key), cancellationToken)
            : await db.StockShortageEnquiries.CountAsync(item => item.OrganizationId == organizationId && item.EnquiryNumber.StartsWith(key), cancellationToken);
        return $"{key}-{(count + 1):D4}";
    }

    private static void Rollup(StockCountSession session)
    {
        if (session.Status == StockCountStatus.CANCELLED) return;
        var lines = session.Lines.ToList();
        if (lines.Any(item => item.SapStatus == StockSapStatus.POSTING)) { session.Status = StockCountStatus.POSTING; return; }
        if (lines.Any(item => item.Status is StockCountLineStatus.ENQUIRY_REQUIRED)) { session.Status = StockCountStatus.ENQUIRY_PENDING; return; }
        if (lines.Any(item => item.Status is StockCountLineStatus.UNDER_REVIEW or StockCountLineStatus.SURPLUS)) { session.Status = StockCountStatus.UNDER_REVIEW; return; }
        if (lines.Any(item => item.SapStatus is StockSapStatus.ADJUSTMENT_PENDING or StockSapStatus.FAILED or StockSapStatus.POSTING_UNKNOWN)) { session.Status = StockCountStatus.ADJUSTMENT_PENDING; return; }
        if (lines.Count > 0 && lines.All(item => item.Status is StockCountLineStatus.MATCHED or StockCountLineStatus.POSTED or StockCountLineStatus.REJECTED))
        {
            session.Status = StockCountStatus.COMPLETED;
            session.CompletedAt ??= DateTime.UtcNow;
            return;
        }
        if (lines.Any(item => item.Status != StockCountLineStatus.NOT_COUNTED)) session.Status = StockCountStatus.IN_PROGRESS;
    }

    private async Task<StockCountDetail> Map(StockCountSession session, bool reveal, CancellationToken cancellationToken)
    {
        var ids = session.Lines.Select(item => item.CountedBy).Append(session.CreatedBy).OfType<Guid>();
        var names = await Names(ids, cancellationToken);
        var lines = session.Lines.OrderBy(item => item.Material.MaterialCode).Select(item => MapLine(item, reveal, names)).ToList();
        var shortageValue = reveal ? session.Lines.Where(item => item.VarianceValue < 0).Sum(item => -(item.VarianceValue ?? 0)) : (decimal?)null;
        return new StockCountDetail(session.Id, session.CountNumber, session.CountType.ToString(), session.PropertyLocation?.LocationName ?? session.InventoryLocation.PropertyLocation?.LocationName,
            session.InventoryLocationId, session.InventoryLocation.LocationName, session.BusinessDate, session.BlindCount, session.Status.ToString(), session.Notes,
            lines.Count, session.Lines.Count(item => item.Status is not StockCountLineStatus.NOT_COUNTED and not StockCountLineStatus.RECOUNT_REQUIRED),
            session.Lines.Count(item => item.Status is StockCountLineStatus.NOT_COUNTED or StockCountLineStatus.RECOUNT_REQUIRED),
            session.Lines.Count(item => item.Status == StockCountLineStatus.MATCHED), session.Lines.Count(item => item.VarianceQty < 0), session.Lines.Count(item => item.VarianceQty > 0), shortageValue,
            lines.Select(item => item.Currency).FirstOrDefault(item => !string.IsNullOrWhiteSpace(item)), names.GetValueOrDefault(session.CreatedBy), lines, StockCountMath.Categories.Select(item => new StockCategoryOption(item.Key.ToString(), item.Label)).ToList());
    }

    private static StockCountLineRow MapLine(StockCountLine line, bool reveal, IReadOnlyDictionary<Guid, string> names)
    {
        return new StockCountLineRow(line.Id, line.MaterialId, line.Material.MaterialCode, line.Material.Description, line.Material.MaterialGroup,
            reveal ? line.SystemQty : null,
            line.SystemUom, line.FullQty, line.OpenQty, line.OpenUom, line.ConvertedPhysicalQty, line.BaseUom,
            reveal ? line.VarianceQty : null, reveal ? line.VarianceValue : null, reveal ? line.UnitCost : null, line.Currency, line.CountMethod?.ToString(),
            names.GetValueOrDefault(line.CountedBy ?? Guid.Empty), line.CountedAt, line.Status.ToString(), line.Enquiry?.Id, line.Enquiry?.Status.ToString(),
            line.Enquiry is { ManagerConfigured: false } ? "MANAGER NOT CONFIGURED" : line.Enquiry?.AssignedManagerGroup,
            line.SapStatus.ToString(), line.SapMaterialDocument, line.SapError, line.Captures.OrderBy(item => item.Sequence).Select(item => new StockCaptureRow(item.Sequence, item.FullQty, item.FullUom, item.OpenQty, item.OpenUom, item.ConvertedQty, item.CountedAt)).ToList(),
            reveal || line.Status != StockCountLineStatus.NOT_COUNTED);
    }

    private async Task<StockEnquiryDetail> MapEnquiry(StockShortageEnquiry enquiry, bool reveal, CancellationToken cancellationToken)
    {
        var ids = enquiry.Messages.Select(item => item.ActorUserId).Append(enquiry.AssignedManagerUserId ?? Guid.Empty);
        var names = await Names(ids, cancellationToken);
        var line = enquiry.Line;
        return new StockEnquiryDetail(enquiry.Id, enquiry.EnquiryNumber, line.Id, enquiry.Status.ToString(), enquiry.ManagerConfigured, enquiry.AssignedManagerGroup,
            names.GetValueOrDefault(enquiry.AssignedManagerUserId ?? Guid.Empty), line.Material.MaterialCode, line.Material.Description, line.Session.InventoryLocation.LocationName,
            reveal ? line.SystemQty : null, line.ConvertedPhysicalQty, reveal ? StockCountMath.ShortageQty(line.VarianceQty ?? 0) : null, line.SystemUom,
            reveal ? enquiry.ShortageValue : null, enquiry.Currency, line.SapStatus.ToString(), line.SapMaterialDocument,
            enquiry.Messages.OrderBy(item => item.CreatedAt).Select(item => new StockMessageRow(item.Kind, item.Category?.ToString(), item.Category is { } cat ? StockCountMath.Label(cat) : null, item.Comments, item.AttachmentName, names.GetValueOrDefault(item.ActorUserId), item.CreatedAt)).ToList());
    }

    private async Task<Dictionary<Guid, string>> Names(IEnumerable<Guid> ids, CancellationToken cancellationToken)
    {
        var keys = ids.Where(item => item != Guid.Empty).Distinct().ToList();
        if (keys.Count == 0) return [];
        return await db.Users.AsNoTracking().Where(item => keys.Contains(item.Id)).ToDictionaryAsync(item => item.Id, item => item.DisplayName, cancellationToken);
    }

    private void Audit(Guid organizationId, string type, Guid id, string action, Guid actor, string? comment) =>
        db.InventoryWorkflowEvents.Add(new InventoryWorkflowEvent
        {
            Id = Guid.NewGuid(), OrganizationId = organizationId, ReferenceType = type, ReferenceId = id, Action = action, Comment = comment, ActorUserId = actor, CreatedAt = DateTime.UtcNow,
        });

    private static string? Trim(string? value, int max) => string.IsNullOrWhiteSpace(value) ? null : value.Trim()[..Math.Min(value.Trim().Length, max)];
}

public sealed record StockCountCreateRequest(string CountType, DateTime BusinessDate, Guid InventoryLocationId, bool BlindCount, string? Notes);
public sealed record StockCountEntry(string? Method, decimal? FullQty, string? FullUom, decimal? OpenQty, string? OpenUom);
public sealed record MaterialBarcodeRequest(Guid MaterialId, string Barcode, string? BarcodeType, string? PackUom, decimal? PackQuantity, bool Active = true);
public sealed record StockJustificationRequest(string Category, string Comments, string? AttachmentName);
public sealed record StockReviewRequest(string Action, string? Comment);
public sealed record StockReportFilter(DateTime? From, DateTime? To, Guid? PropertyId, Guid? LocationId, string? MaterialGroup, Guid? Manager, string? Category, string? EnquiryStatus, string? ReviewStatus, string? SapStatus);
public sealed record StockCountListRow(Guid Id, string CountNumber, string CountType, string? Property, string Location, DateTime BusinessDate, int Materials, int Counted, int Matched, int Shortage, int Surplus, decimal? ShortageValue, string? Currency, string Status, string? CreatedBy);
public sealed record StockCaptureRow(int Sequence, decimal? FullQty, string? FullUom, decimal? OpenQty, string? OpenUom, decimal ConvertedQty, DateTime CountedAt);
public sealed record StockCountLineRow(Guid Id, Guid MaterialId, string MaterialCode, string Description, string? MaterialGroup, decimal? SystemQty, string SystemUom, decimal? FullQty, decimal? OpenQty, string? OpenUom, decimal? PhysicalQty, string? BaseUom, decimal? VarianceQty, decimal? VarianceValue, decimal? UnitCost, string? Currency, string? CountMethod, string? CountedBy, DateTime? CountedAt, string Status, Guid? EnquiryId, string? EnquiryStatus, string? Manager, string SapStatus, string? SapMaterialDocument, string? SapError, IReadOnlyList<StockCaptureRow> Captures, bool BookVisible);
public sealed record StockCategoryOption(string Key, string Label);
public sealed record StockCountDetail(Guid Id, string CountNumber, string CountType, string? Property, Guid LocationId, string Location, DateTime BusinessDate, bool BlindCount, string Status, string? Notes, int Materials, int Counted, int Remaining, int Matched, int Shortage, int Surplus, decimal? ShortageValue, string? Currency, string? CreatedBy, IReadOnlyList<StockCountLineRow> Lines, IReadOnlyList<StockCategoryOption> Categories);
public sealed record StockMaterialHit(Guid Id, string MaterialCode, string Description, string? MaterialGroup, string BaseUom, string? AlternateUom, decimal? AlternateValue, Guid? LineId = null);
public sealed record StockIdentifyResult(string Code, string? Message, IReadOnlyList<StockMaterialHit> Candidates);
public sealed record MaterialBarcodeRow(Guid Id, Guid MaterialId, string MaterialCode, string Barcode, string BarcodeType, string? PackUom, decimal? PackQuantity, bool Active);
public sealed record StockTaskRow(string Kind, Guid SessionId, Guid? EnquiryId, string Number, string Location, string Title, string Status);
public sealed record StockMessageRow(string Kind, string? Category, string? CategoryLabel, string Comments, string? AttachmentName, string? Actor, DateTime CreatedAt);
public sealed record StockEnquiryDetail(Guid Id, string EnquiryNumber, Guid LineId, string Status, bool ManagerConfigured, string? ManagerGroup, string? ManagerName, string? MaterialCode, string? Description, string? Location, decimal? SystemQty, decimal? PhysicalQty, decimal? ShortageQty, string? Uom, decimal? ShortageValue, string? Currency, string? SapStatus, string? SapMaterialDocument, IReadOnlyList<StockMessageRow> Messages);
public sealed record StockLocationTotal(string? Property, string? Location, Guid LocationId, decimal? Value, string? Currency);
public sealed record StockReasonTotal(string Category, int Count, decimal? Value, decimal? Percent);
public sealed record StockReportLine(string? Property, string? Venue, string? Location, string? ManagerGroup, string? Manager, string CountNumber, DateTime CountDate, string MaterialCode, string Description, string? MaterialGroup, decimal SystemQty, decimal? PhysicalQty, decimal ShortageQty, string Uom, decimal? UnitCost, decimal? ShortageValue, string? Currency, string Category, string? Comments, DateTime? ResponseAt, string? Reviewer, string? ReviewDecision, DateTime? ReviewAt, string SapStatus, string? SapMaterialDocument, string? EnquiryNumber, string EnquiryStatus, string LineStatus, Guid LocationId);
public sealed record StockShortageReport(DateTime? From, DateTime? To, int Locations, int Materials, decimal? ShortageQuantity, string? QuantityUom, string? Currency, decimal? TotalValue, decimal? AwaitingValue, decimal? JustifiedValue, decimal? UnresolvedValue, decimal? ApprovedValue, decimal? PostedValue, decimal? FailedValue, IReadOnlyList<StockLocationTotal> ByLocation, IReadOnlyList<StockReasonTotal> ByReason, IReadOnlyList<StockReportLine> Lines);
public sealed record StockLedgerRow(Guid Id, string TransactionId, string TransactionType, string Direction, string? Material, string? Location, decimal Quantity, string Uom, string? ReferenceType, Guid? ReferenceId, DateTime BusinessDate, DateTime CreatedAt);
