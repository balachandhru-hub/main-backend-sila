using System.Globalization;
using Microsoft.EntityFrameworkCore;
using SilaMe.Api.Auth;
using SilaMe.Api.Data;
using SilaMe.Api.DTOs;
using SilaMe.Api.Models;

namespace SilaMe.Api.Services;

public sealed class InventoryLocationService(SilaMeDbContext db)
{
    internal static readonly string[] TemplateColumns =
    [
        "LocationCode", "LocationName", "LocationType", "ParentLocationCode", "PropertyCode", "Description",
        "InventoryEnabled", "SalesEnabled", "ConsumptionEnabled", "TransferEnabled",
        "CompanyCode", "GeneralLedgerNumber", "CostCenter", "ProfitCenter", "Currency", "ManagerGroup", "Active"
    ];

    public byte[] Template() => ExcelOpenXml.WriteSheets(
    [
        new ExcelOpenXml.SheetWrite("INSTRUCTIONS", ["Field", "Required", "Notes"],
        [
            Field("LocationCode", "Yes", "Unique per tenant. Upload is idempotent on this code."),
            Field("LocationName", "Yes", "Display name."),
            Field("LocationType", "Yes", "PROPERTY, VENUE, STORE, or OUTLET."),
            Field("ParentLocationCode", "No", "Blank for PROPERTY. VENUE parent is a PROPERTY. STORE/OUTLET parent is a PROPERTY or VENUE."),
            Field("PropertyCode", "No", "Unused for PROPERTY. For children, leave blank when ParentLocationCode is set."),
            Field("Description", "No", ""),
            Field("InventoryEnabled", "Yes", "TRUE/FALSE. Location may hold stock only when TRUE."),
            Field("SalesEnabled", "Yes", "TRUE/FALSE."),
            Field("ConsumptionEnabled", "Yes", "TRUE/FALSE."),
            Field("TransferEnabled", "Yes", "TRUE/FALSE. Required for ITO."),
            Field("CompanyCode", "Yes for PROPERTY", "Parent of a Property. Required when LocationType is PROPERTY. Inherited from the Property for Store/Outlet/Venue when blank."),
            Field("GeneralLedgerNumber", "No", "GL account number for this location."),
            Field("CostCenter", "No", "Financial dimension for later ERP posting."),
            Field("ProfitCenter", "No", "Financial dimension for later ERP posting."),
            Field("Currency", "No", ""),
            Field("ManagerGroup", "No", "Existing role key used for ITO approvals, for example INVENTORY_MANAGER."),
            Field("Active", "Yes", "TRUE/FALSE."),
        ]),
        new ExcelOpenXml.SheetWrite("Data", TemplateColumns, []),
    ]);

    private static Dictionary<string, string?> Field(string name, string required, string notes) =>
        new() { ["Field"] = name, ["Required"] = required, ["Notes"] = notes };

    public async Task<byte[]> ExportAsync(Guid organizationId, CancellationToken cancellationToken)
    {
        var rows = await ListAsync(organizationId, null, null, null, null, cancellationToken);
        var data = rows.Select(item => new Dictionary<string, string?>
        {
            ["LocationCode"] = item.LocationCode,
            ["LocationName"] = item.LocationName,
            ["LocationType"] = item.LocationType,
            ["ParentLocationCode"] = rows.FirstOrDefault(row => row.Id == item.ParentLocationId)?.LocationCode,
            ["PropertyCode"] = item.PropertyCode,
            ["Description"] = item.Description,
            ["InventoryEnabled"] = item.InventoryEnabled ? "TRUE" : "FALSE",
            ["SalesEnabled"] = item.SalesEnabled ? "TRUE" : "FALSE",
            ["ConsumptionEnabled"] = item.ConsumptionEnabled ? "TRUE" : "FALSE",
            ["TransferEnabled"] = item.TransferEnabled ? "TRUE" : "FALSE",
            ["CompanyCode"] = item.CompanyCode,
            ["GeneralLedgerNumber"] = item.GeneralLedgerNumber,
            ["CostCenter"] = item.CostCenter,
            ["ProfitCenter"] = item.ProfitCenter,
            ["Currency"] = item.Currency,
            ["ManagerGroup"] = item.ManagerGroup,
            ["Active"] = item.Status == "ACTIVE" ? "TRUE" : "FALSE",
        }).ToList();
        return ExcelOpenXml.Write(TemplateColumns, data);
    }

    public async Task<IReadOnlyList<InventoryLocationRow>> ListAsync(
        Guid organizationId, string? query, string? locationType, Guid? propertyLocationId, bool? active, CancellationToken cancellationToken)
    {
        var rows = db.InventoryLocations.AsNoTracking().Where(item => item.OrganizationId == organizationId);
        if (!string.IsNullOrWhiteSpace(query))
        {
            var term = query.Trim().ToUpperInvariant();
            rows = rows.Where(item => item.LocationCode.ToUpper().Contains(term) || item.LocationName.ToUpper().Contains(term));
        }
        if (Enum.TryParse<InventoryLocationType>(locationType, true, out var type))
            rows = rows.Where(item => item.LocationType == type);
        if (propertyLocationId is Guid propertyId)
            rows = rows.Where(item => item.Id == propertyId || item.PropertyLocationId == propertyId);
        if (active == true) rows = rows.Where(item => item.Status == StatusKind.ACTIVE);
        if (active == false) rows = rows.Where(item => item.Status != StatusKind.ACTIVE);
        var list = await rows.OrderBy(item => item.LocationType).ThenBy(item => item.LocationCode).ToListAsync(cancellationToken);
        var all = await db.InventoryLocations.AsNoTracking().Where(item => item.OrganizationId == organizationId).ToListAsync(cancellationToken);
        var names = all.ToDictionary(item => item.Id);
        return list.Select(item => ToRow(item, names)).ToList();
    }

    public async Task<InventoryLocationOptions> OptionsAsync(Guid organizationId, CancellationToken cancellationToken)
    {
        var locations = await ListAsync(organizationId, null, null, null, true, cancellationToken);
        var properties = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        foreach (var item in locations.Where(row => row.LocationType == "PROPERTY"))
            properties[item.LocationCode] = item.LocationName;
        var companies = await db.CompanyCodes.AsNoTracking().Where(row => row.OrganizationId == organizationId).OrderBy(row => row.CompanyCode)
            .Select(row => new InventoryCodeName(row.CompanyCode, row.CompanyName)).ToListAsync(cancellationToken);
        var roles = await db.Roles.AsNoTracking().Where(row => row.Status == StatusKind.ACTIVE).OrderBy(row => row.Key)
            .Select(row => new InventoryCodeName(row.Key, row.Name)).ToListAsync(cancellationToken);
        return new InventoryLocationOptions(
            [new("PROPERTY", "Property"), new("VENUE", "Venue"), new("STORE", "Store"), new("OUTLET", "Outlet")],
            locations,
            properties.Select(item => new InventoryCodeName(item.Key, item.Value)).OrderBy(item => item.Code).ToList(),
            companies, roles);
    }

    public async Task<InventoryLocationRow> GetAsync(Guid organizationId, Guid id, CancellationToken cancellationToken)
    {
        var item = await db.InventoryLocations.AsNoTracking().SingleOrDefaultAsync(row => row.Id == id && row.OrganizationId == organizationId, cancellationToken)
            ?? throw new RecipeManagementException("LOCATION_NOT_FOUND", "The inventory location was not found.", 404);
        var names = await db.InventoryLocations.AsNoTracking().Where(row => row.OrganizationId == organizationId).ToDictionaryAsync(row => row.Id, cancellationToken);
        return ToRow(item, names);
    }

    public async Task<InventoryLocationRow> UpsertAsync(Session session, Guid organizationId, InventoryLocationUpsertRequest request, CancellationToken cancellationToken)
    {
        var now = DateTime.UtcNow;
        var code = Require(request.LocationCode, "LocationCode");
        if (!Enum.TryParse<InventoryLocationType>(request.LocationType, true, out var type))
            throw new RecipeManagementException("INVALID_LOCATION_TYPE", "LocationType must be PROPERTY, VENUE, STORE, or OUTLET.");
        var existing = await db.InventoryLocations.SingleOrDefaultAsync(item => item.OrganizationId == organizationId && item.LocationCode == code, cancellationToken);
        var companyCode = await ResolveCompanyCode(organizationId, type, request, cancellationToken);
        request = request with { CompanyCode = companyCode, ParentLocationCode = type == InventoryLocationType.PROPERTY ? null : request.ParentLocationCode, PropertyCode = type == InventoryLocationType.PROPERTY ? null : request.PropertyCode };
        var parent = type == InventoryLocationType.PROPERTY ? null : await ResolveOperationalParent(organizationId, type, FirstNonEmpty(request.ParentLocationCode, request.PropertyCode), cancellationToken);
        if (parent is not null && parent.Id == existing?.Id)
            throw new RecipeManagementException("INVALID_PARENT", "A location cannot be its own parent.");
        if (parent is not null && existing is not null && await WouldCycle(existing.Id, parent.Id, cancellationToken))
            throw new RecipeManagementException("CIRCULAR_HIERARCHY", "The parent would create a circular hierarchy.");
        if (existing is null)
        {
            existing = new InventoryLocation
            {
                Id = Guid.NewGuid(),
                OrganizationId = organizationId,
                LocationCode = code,
                LocationName = Require(request.LocationName, "LocationName"),
                LocationType = type,
                CreatedAt = now,
                CreatedBy = session.UserId,
            };
            db.InventoryLocations.Add(existing);
        }
        Apply(existing, request, type, parent, session.UserId, now);
        await MapReferences(existing, request, cancellationToken);
        await db.SaveChangesAsync(cancellationToken);
        if (existing.LocationType == InventoryLocationType.PROPERTY && existing.PropertyLocationId != existing.Id)
        {
            existing.PropertyLocationId = existing.Id;
            await db.SaveChangesAsync(cancellationToken);
        }
        await SyncSharedLocationAsync(existing, cancellationToken);
        return await GetAsync(organizationId, existing.Id, cancellationToken);
    }

    public async Task ActivateAsync(Session session, Guid organizationId, Guid id, bool active, CancellationToken cancellationToken)
    {
        var item = await db.InventoryLocations.SingleOrDefaultAsync(row => row.Id == id && row.OrganizationId == organizationId, cancellationToken)
            ?? throw new RecipeManagementException("LOCATION_NOT_FOUND", "The inventory location was not found.", 404);
        item.Status = active ? StatusKind.ACTIVE : StatusKind.INACTIVE;
        item.UpdatedAt = DateTime.UtcNow;
        item.UpdatedBy = session.UserId;
        await db.SaveChangesAsync(cancellationToken);
        await SyncSharedLocationAsync(item, cancellationToken);
    }

    public async Task<InventoryImportSummary> ImportAsync(Session session, Guid organizationId, Stream file, string fileName, bool commit, CancellationToken cancellationToken)
    {
        IReadOnlyList<ExcelOpenXml.Sheet> sheets;
        try { sheets = ExcelOpenXml.ReadSheets(file); }
        catch { throw new RecipeManagementException("IMPORT_FILE_INVALID", "The file is not a valid Excel workbook."); }
        var sheet = ExcelOpenXml.FindSheet(sheets, "Data", "Locations") ?? sheets[0];
        var rows = ExcelOpenXml.ToDictionaries(sheet.Rows);
        var existing = await db.InventoryLocations.Where(item => item.OrganizationId == organizationId).ToListAsync(cancellationToken);
        var byCode = existing.ToDictionary(item => item.LocationCode, StringComparer.OrdinalIgnoreCase);
        var companies = await db.CompanyCodes.AsNoTracking().Where(item => item.OrganizationId == organizationId).ToListAsync(cancellationToken);
        var errors = new List<InventoryImportInvalid>();
        var parsed = new List<(int Row, InventoryLocationUpsertRequest Request)>();
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        for (var index = 0; index < rows.Count; index++)
        {
            var rowNumber = index + 2;
            var row = rows[index];
            try
            {
                var request = ParseRow(row);
                if (!seen.Add(request.LocationCode))
                {
                    errors.Add(new InventoryImportInvalid(rowNumber, "DUPLICATE_LOCATION_CODE", $"LocationCode {request.LocationCode} is duplicated in the file."));
                    continue;
                }
                parsed.Add((rowNumber, request));
            }
            catch (RecipeManagementException exception)
            {
                errors.Add(new InventoryImportInvalid(rowNumber, exception.Code, exception.Message));
            }
        }

        var fileCodes = parsed.ToDictionary(item => item.Request.LocationCode, StringComparer.OrdinalIgnoreCase);
        foreach (var (rowNumber, request) in parsed.ToList())
        {
            if (!Enum.TryParse<InventoryLocationType>(request.LocationType, true, out var parsedType))
                continue;
            if (parsedType == InventoryLocationType.PROPERTY)
            {
                var company = FirstNonEmpty(request.CompanyCode, request.ParentLocationCode);
                if (string.IsNullOrWhiteSpace(company) || companies.All(item => !string.Equals(item.CompanyCode, company, StringComparison.OrdinalIgnoreCase)))
                {
                    errors.Add(new InventoryImportInvalid(rowNumber, "COMPANY_CODE_REQUIRED", "Property requires an existing Company Code as parent."));
                    parsed.RemoveAll(item => item.Row == rowNumber);
                }
                continue;
            }
            var parentCode = FirstNonEmpty(request.ParentLocationCode, request.PropertyCode);
            if (string.IsNullOrWhiteSpace(parentCode)
                || (!fileCodes.ContainsKey(parentCode) && !byCode.ContainsKey(parentCode)))
            {
                errors.Add(new InventoryImportInvalid(rowNumber, "INVALID_PARENT", ParentRule(parsedType)));
                parsed.RemoveAll(item => item.Row == rowNumber);
            }
        }

        var order = Topological(parsed.Select(item => item.Request).ToList(), errors, byCode.Keys);
        var valid = parsed.Where(item => order.Contains(item.Request.LocationCode, StringComparer.OrdinalIgnoreCase)).ToList();
        var created = 0;
        var changed = 0;
        var unchanged = 0;
        var now = DateTime.UtcNow;
        foreach (var code in order)
        {
            var request = valid.First(item => string.Equals(item.Request.LocationCode, code, StringComparison.OrdinalIgnoreCase)).Request;
            Enum.TryParse<InventoryLocationType>(request.LocationType, true, out var type);
            byCode.TryGetValue(code, out var current);
            Guid? parentId = null;
            Guid? propertyId = null;
            if (type == InventoryLocationType.PROPERTY)
            {
                propertyId = current?.Id;
            }
            else
            {
                var parentCode = FirstNonEmpty(request.ParentLocationCode, request.PropertyCode);
                if (!string.IsNullOrWhiteSpace(parentCode) && byCode.TryGetValue(parentCode, out var parent) && AllowedParent(type, parent.LocationType))
                {
                    parentId = parent.Id;
                    propertyId = parent.LocationType == InventoryLocationType.PROPERTY ? parent.Id : parent.PropertyLocationId;
                    if (string.IsNullOrWhiteSpace(request.CompanyCode))
                        request = request with { CompanyCode = parent.CompanyCode };
                }
                else
                {
                    errors.Add(new InventoryImportInvalid(0, "INVALID_PARENT", ParentRule(type)));
                    continue;
                }
            }
            var fingerprint = Fingerprint(request, parentId, propertyId);
            var existingFingerprint = current is null ? null : Fingerprint(current);
            if (current is null)
            {
                created++;
                if (!commit) continue;
                current = new InventoryLocation
                {
                    Id = Guid.NewGuid(),
                    OrganizationId = organizationId,
                    LocationCode = request.LocationCode,
                    LocationName = request.LocationName,
                    LocationType = type,
                    CreatedAt = now,
                    CreatedBy = session.UserId,
                };
                db.InventoryLocations.Add(current);
                byCode[current.LocationCode] = current;
                if (type == InventoryLocationType.PROPERTY) propertyId = current.Id;
            }
            else if (fingerprint == existingFingerprint)
            {
                unchanged++;
                continue;
            }
            else
            {
                changed++;
                if (!commit) continue;
            }
            if (!commit) continue;
            current.LocationName = request.LocationName;
            current.LocationType = type;
            current.ParentLocationId = parentId;
            current.PropertyLocationId = type == InventoryLocationType.PROPERTY ? current.Id : propertyId;
            current.Description = request.Description;
            current.InventoryEnabled = request.InventoryEnabled;
            current.SalesEnabled = request.SalesEnabled;
            current.ConsumptionEnabled = request.ConsumptionEnabled;
            current.TransferEnabled = request.TransferEnabled;
            current.CompanyCode = request.CompanyCode;
            current.GeneralLedgerNumber = request.GeneralLedgerNumber;
            current.CostCenter = request.CostCenter;
            current.ProfitCenter = request.ProfitCenter;
            current.Currency = request.Currency;
            current.ManagerGroup = request.ManagerGroup;
            current.CompanyCodeMasterId = companies.FirstOrDefault(item => string.Equals(item.CompanyCode, request.CompanyCode, StringComparison.OrdinalIgnoreCase))?.Id;
            current.PropertyMasterId = null;
            current.Status = request.Active ? StatusKind.ACTIVE : StatusKind.INACTIVE;
            current.UpdatedAt = now;
            current.UpdatedBy = session.UserId;
        }
        if (commit)
        {
            await db.SaveChangesAsync(cancellationToken);
            foreach (var item in byCode.Values)
                await SyncSharedLocationAsync(item, cancellationToken);
        }
        return new InventoryImportSummary(fileName, rows.Count, created, changed, unchanged, errors.Count, errors);
    }

    private static InventoryLocationUpsertRequest ParseRow(Dictionary<string, string?> row)
    {
        var type = Require(Value(row, "LocationType"), "LocationType");
        if (!Enum.TryParse<InventoryLocationType>(type, true, out var parsedType))
            throw new RecipeManagementException("INVALID_LOCATION_TYPE", "LocationType must be PROPERTY, VENUE, STORE, or OUTLET.");
        var companyCode = Value(row, "CompanyCode");
        var parentCode = Value(row, "ParentLocationCode");
        var propertyCode = Value(row, "PropertyCode");
        if (parsedType == InventoryLocationType.PROPERTY)
        {
            companyCode = FirstNonEmpty(companyCode, parentCode);
            parentCode = null;
            propertyCode = null;
        }
        else
        {
            parentCode = FirstNonEmpty(parentCode, propertyCode);
            propertyCode = parentCode;
        }
        return new InventoryLocationUpsertRequest(
            Require(Value(row, "LocationCode"), "LocationCode"),
            Require(Value(row, "LocationName"), "LocationName"),
            parsedType.ToString(),
            parentCode,
            propertyCode,
            Value(row, "Description"),
            Bool(row, "InventoryEnabled"),
            Bool(row, "SalesEnabled"),
            Bool(row, "ConsumptionEnabled"),
            Bool(row, "TransferEnabled"),
            companyCode,
            Value(row, "GeneralLedgerNumber") ?? Value(row, "GLNumber") ?? Value(row, "GlAccount"),
            Value(row, "CostCenter"),
            Value(row, "ProfitCenter"),
            Value(row, "Currency"),
            Value(row, "ManagerGroup"),
            Bool(row, "Active"));
    }

    private static List<string> Topological(List<InventoryLocationUpsertRequest> rows, List<InventoryImportInvalid> errors, IEnumerable<string> existingCodes)
    {
        var known = new HashSet<string>(existingCodes, StringComparer.OrdinalIgnoreCase);
        var remaining = rows.ToList();
        var order = new List<string>();
        while (remaining.Count > 0)
        {
            var ready = remaining.Where(item => string.IsNullOrWhiteSpace(item.ParentLocationCode) || known.Contains(item.ParentLocationCode) || string.Equals(item.ParentLocationCode, item.LocationCode, StringComparison.OrdinalIgnoreCase) is false && order.Contains(item.ParentLocationCode, StringComparer.OrdinalIgnoreCase)).ToList();
            ready = remaining.Where(item => string.IsNullOrWhiteSpace(item.ParentLocationCode) || known.Contains(item.ParentLocationCode!)).ToList();
            if (ready.Count == 0)
            {
                foreach (var stuck in remaining)
                    errors.Add(new InventoryImportInvalid(0, "CIRCULAR_HIERARCHY", $"Location {stuck.LocationCode} has a circular or unresolved parent."));
                break;
            }
            foreach (var item in ready)
            {
                order.Add(item.LocationCode);
                known.Add(item.LocationCode);
                remaining.Remove(item);
            }
        }
        return order;
    }

    private async Task<string?> ResolveCompanyCode(Guid organizationId, InventoryLocationType type, InventoryLocationUpsertRequest request, CancellationToken cancellationToken)
    {
        var code = type == InventoryLocationType.PROPERTY
            ? FirstNonEmpty(request.CompanyCode, request.ParentLocationCode)
            : request.CompanyCode?.Trim();
        if (type == InventoryLocationType.PROPERTY && string.IsNullOrWhiteSpace(code))
            throw new RecipeManagementException("COMPANY_CODE_REQUIRED", "Property requires a Company Code as parent.");
        if (string.IsNullOrWhiteSpace(code)) return null;
        var company = await db.CompanyCodes.AsNoTracking().FirstOrDefaultAsync(row =>
            row.OrganizationId == organizationId && !row.IsDeleted && row.CompanyCode == code, cancellationToken)
            ?? throw new RecipeManagementException("COMPANY_CODE_NOT_FOUND", $"Company Code {code} was not found.");
        return company.CompanyCode;
    }

    private async Task<InventoryLocation> ResolveOperationalParent(Guid organizationId, InventoryLocationType type, string? code, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(code))
            throw new RecipeManagementException("PARENT_REQUIRED", ParentRule(type));
        var parent = await db.InventoryLocations.SingleOrDefaultAsync(item => item.OrganizationId == organizationId && item.LocationCode == code.Trim(), cancellationToken)
            ?? throw new RecipeManagementException("INVALID_PARENT", $"Parent location {code} was not found.");
        if (!AllowedParent(type, parent.LocationType))
            throw new RecipeManagementException("INVALID_PARENT", ParentRule(type));
        return parent;
    }

    private static bool AllowedParent(InventoryLocationType child, InventoryLocationType parent) => child switch
    {
        InventoryLocationType.VENUE => parent == InventoryLocationType.PROPERTY,
        InventoryLocationType.STORE or InventoryLocationType.OUTLET => parent is InventoryLocationType.PROPERTY or InventoryLocationType.VENUE,
        _ => false,
    };

    private static string ParentRule(InventoryLocationType type) => type == InventoryLocationType.VENUE
        ? "Venue requires a Property as parent."
        : "Store and Outlet require a Property or Venue as parent.";

    private async Task MapReferences(InventoryLocation item, InventoryLocationUpsertRequest request, CancellationToken cancellationToken)
    {
        item.CompanyCodeMasterId = string.IsNullOrWhiteSpace(request.CompanyCode) ? null :
            (await db.CompanyCodes.AsNoTracking().FirstOrDefaultAsync(row => row.OrganizationId == item.OrganizationId && row.CompanyCode == request.CompanyCode.Trim(), cancellationToken))?.Id;
        item.PropertyMasterId = null;
    }

    private async Task<bool> WouldCycle(Guid locationId, Guid proposedParentId, CancellationToken cancellationToken)
    {
        var current = proposedParentId;
        var seen = new HashSet<Guid> { locationId };
        while (true)
        {
            if (!seen.Add(current)) return true;
            var parent = await db.InventoryLocations.AsNoTracking().SingleOrDefaultAsync(item => item.Id == current, cancellationToken);
            if (parent?.ParentLocationId is not Guid next) return false;
            current = next;
        }
    }

    private static void Apply(InventoryLocation item, InventoryLocationUpsertRequest request, InventoryLocationType type, InventoryLocation? parent, Guid? userId, DateTime now)
    {
        item.LocationName = Require(request.LocationName, "LocationName");
        item.LocationType = type;
        item.ParentLocationId = type == InventoryLocationType.PROPERTY ? null : parent?.Id;
        item.PropertyLocationId = type == InventoryLocationType.PROPERTY
            ? item.Id
            : parent?.LocationType == InventoryLocationType.PROPERTY ? parent.Id : parent?.PropertyLocationId;
        item.Description = request.Description;
        item.InventoryEnabled = request.InventoryEnabled;
        item.SalesEnabled = request.SalesEnabled;
        item.ConsumptionEnabled = request.ConsumptionEnabled;
        item.TransferEnabled = request.TransferEnabled;
        item.CompanyCode = FirstNonEmpty(request.CompanyCode, parent?.CompanyCode);
        item.GeneralLedgerNumber = request.GeneralLedgerNumber;
        item.CostCenter = request.CostCenter;
        item.ProfitCenter = request.ProfitCenter;
        item.Currency = request.Currency;
        item.ManagerGroup = request.ManagerGroup;
        item.Status = request.Active ? StatusKind.ACTIVE : StatusKind.INACTIVE;
        item.UpdatedAt = now;
        item.UpdatedBy = userId;
    }

    internal static InventoryLocationRow ToRow(InventoryLocation item, IReadOnlyDictionary<Guid, InventoryLocation> names)
    {
        names.TryGetValue(item.ParentLocationId ?? Guid.Empty, out var parent);
        var property = item.PropertyLocationId is Guid propertyId && names.TryGetValue(propertyId, out var found) ? found : (item.LocationType == InventoryLocationType.PROPERTY ? item : null);
        return new InventoryLocationRow(
            item.Id, item.LocationCode, item.LocationName, item.LocationType.ToString(), item.ParentLocationId, parent?.LocationName, parent?.LocationCode,
            item.PropertyLocationId, property?.LocationCode, property?.LocationName, item.Description,
            item.InventoryEnabled, item.SalesEnabled, item.ConsumptionEnabled, item.TransferEnabled,
            item.CompanyCode, item.GeneralLedgerNumber, item.CostCenter, item.ProfitCenter,
            item.Currency, item.ManagerGroup, item.Status.ToString());
    }

    private static string Fingerprint(InventoryLocationUpsertRequest request, Guid? parentId, Guid? propertyId) =>
        string.Join('|', request.LocationName, request.LocationType, parentId, propertyId, request.Description,
            request.InventoryEnabled, request.SalesEnabled, request.ConsumptionEnabled, request.TransferEnabled,
            request.CompanyCode, request.GeneralLedgerNumber, request.CostCenter, request.ProfitCenter,
            request.Currency, request.ManagerGroup, request.Active);

    private static string Fingerprint(InventoryLocation item) =>
        string.Join('|', item.LocationName, item.LocationType, item.ParentLocationId, item.PropertyLocationId, item.Description,
            item.InventoryEnabled, item.SalesEnabled, item.ConsumptionEnabled, item.TransferEnabled,
            item.CompanyCode, item.GeneralLedgerNumber, item.CostCenter, item.ProfitCenter,
            item.Currency, item.ManagerGroup, item.Status == StatusKind.ACTIVE);

    private async Task SyncSharedLocationAsync(InventoryLocation item, CancellationToken cancellationToken)
    {
        if (!Enum.TryParse<RecipeLocationKind>(item.LocationType.ToString(), true, out var kind)) return;
        var row = await db.RecipeLocations.SingleOrDefaultAsync(location =>
            location.OrganizationId == item.OrganizationId && location.Kind == kind && location.Code == item.LocationCode, cancellationToken);
        if (row is null)
        {
            row = new RecipeLocation
            {
                Id = Guid.NewGuid(),
                OrganizationId = item.OrganizationId,
                Kind = kind,
                Code = item.LocationCode,
                Name = item.LocationName,
                CreatedAt = DateTime.UtcNow,
            };
            db.RecipeLocations.Add(row);
        }
        row.Name = item.LocationName;
        row.Description = item.Description;
        row.Status = item.Status;
        row.UpdatedAt = DateTime.UtcNow;
        await db.SaveChangesAsync(cancellationToken);
    }

    private static string? FirstNonEmpty(params string?[] values) =>
        values.FirstOrDefault(value => !string.IsNullOrWhiteSpace(value))?.Trim();

    private static string Require(string? value, string field) =>
        string.IsNullOrWhiteSpace(value) ? throw new RecipeManagementException("REQUIRED_FIELD", $"{field} is required.") : value.Trim();

    private static string? Value(Dictionary<string, string?> row, string key) =>
        row.TryGetValue(key, out var value) ? value?.Trim() : null;

    private static bool Bool(Dictionary<string, string?> row, string key)
    {
        var value = Value(row, key) ?? throw new RecipeManagementException("REQUIRED_FIELD", $"{key} is required.");
        return value.ToUpperInvariant() switch
        {
            "TRUE" or "YES" or "Y" or "1" => true,
            "FALSE" or "NO" or "N" or "0" => false,
            _ => throw new RecipeManagementException("INVALID_BOOLEAN", $"{key} must be TRUE or FALSE.")
        };
    }
}
