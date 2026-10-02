using System.Globalization;
using Microsoft.EntityFrameworkCore;
using SilaMe.Api.Auth;
using SilaMe.Api.Data;
using SilaMe.Api.DTOs;
using SilaMe.Api.Models;

namespace SilaMe.Api.Services;

public sealed class MaterialMasterService(
    SilaMeDbContext db,
    MaterialApprovalService approvals,
    IntegrationRouteResolver routes,
    S4ProductMaterialAdapter sap)
{
    public async Task<IReadOnlyList<MaterialMasterRow>> ListAsync(
        Guid organizationId, string? query, string? status, string? priceStatus, string? category,
        string? materialGroup, string? materialType, string? supplier, CancellationToken cancellationToken) =>
        (await SearchAsync(organizationId, query, status, materialGroup, materialType, category, priceStatus, null,
            string.IsNullOrWhiteSpace(query) && string.IsNullOrWhiteSpace(category) && string.IsNullOrWhiteSpace(materialGroup)
            && string.IsNullOrWhiteSpace(materialType) && string.IsNullOrWhiteSpace(supplier) ? 100 : 40,
            cancellationToken, supplier)).Items.ToList();

    public async Task<PageResult<MaterialMasterRow>> SearchAsync(
        Guid organizationId, string? query, string? status, string? materialGroup, string? materialType, string? category,
        string? priceStatus, string? cursor, int pageSize, CancellationToken cancellationToken, string? supplier = null)
    {
        var size = Math.Clamp(pageSize <= 0 ? 50 : pageSize, 1, 100);
        var rows = db.Materials.AsNoTracking().Where(item => item.OrganizationId == organizationId);
        if (Enum.TryParse<StatusKind>(status, true, out var parsed))
            rows = rows.Where(item => item.Status == parsed);
        if (!string.IsNullOrWhiteSpace(materialGroup))
            rows = rows.Where(item => item.MaterialGroup != null && item.MaterialGroup.ToUpper().Contains(materialGroup.Trim().ToUpper()));
        if (!string.IsNullOrWhiteSpace(materialType))
            rows = rows.Where(item => item.MaterialType != null && item.MaterialType.ToUpper().Contains(materialType.Trim().ToUpper()));
        if (!string.IsNullOrWhiteSpace(category))
            rows = rows.Where(item => item.Category != null && item.Category.ToUpper().Contains(category.Trim().ToUpper()));
        if (!string.IsNullOrWhiteSpace(supplier))
        {
            var supplierKey = supplier.Trim().ToUpperInvariant();
            if (Guid.TryParse(supplier.Trim(), out var supplierId))
                rows = rows.Where(item => db.SupplierMaterials.Any(link => link.MaterialId == item.Id && link.SupplierId == supplierId));
            else
            {
                var bySupplier = db.SupplierMaterials.Where(link =>
                    link.Supplier.OrganizationId == organizationId &&
                    (link.Supplier.SupplierCode.ToUpper().Contains(supplierKey)
                        || link.Supplier.Name.ToUpper().Contains(supplierKey)
                        || link.Supplier.NormalizedName.Contains(supplierKey)));
                rows = rows.Where(item => bySupplier.Any(link => link.MaterialId == item.Id));
            }
        }
        var pendingIds = db.MaterialChangeRequests.Where(change =>
            change.OrganizationId == organizationId && change.MaterialId != null && change.Status == MaterialGovernanceStatus.PENDING_APPROVAL)
            .Select(change => change.MaterialId!.Value);
        var filter = (priceStatus ?? "").Trim().ToUpperInvariant().Replace(' ', '_');
        if (filter is "MISSING" or "PRICE_MISSING")
            rows = rows.Where(item => !pendingIds.Contains(item.Id) && (
                (item.GovernanceStatus != MaterialGovernanceStatus.ACTIVE && item.GovernanceStatus != MaterialGovernanceStatus.APPROVED)
                || (item.UnitCost == null && item.StandardPrice == null && item.MovingAveragePrice == null)));
        else if (filter is "MISSING_PRICE")
            rows = rows.Where(item =>
                (item.GovernanceStatus != MaterialGovernanceStatus.ACTIVE && item.GovernanceStatus != MaterialGovernanceStatus.APPROVED)
                || (item.UnitCost == null && item.StandardPrice == null && item.MovingAveragePrice == null));
        else if (filter is "AVAILABLE" or "HAS_PRICE")
            rows = rows.Where(item =>
                (item.GovernanceStatus == MaterialGovernanceStatus.ACTIVE || item.GovernanceStatus == MaterialGovernanceStatus.APPROVED)
                && (item.UnitCost != null || item.StandardPrice != null || item.MovingAveragePrice != null));
        else if (filter is "APPROVED" or "PRICE_APPROVED")
            rows = rows.Where(item => !pendingIds.Contains(item.Id)
                && (item.GovernanceStatus == MaterialGovernanceStatus.ACTIVE || item.GovernanceStatus == MaterialGovernanceStatus.APPROVED)
                && (item.UnitCost != null || item.StandardPrice != null || item.MovingAveragePrice != null));
        else if (filter is "PENDING" or "PENDING_APPROVAL" or "PRICE_PENDING_APPROVAL")
            rows = rows.Where(item => pendingIds.Contains(item.Id));
        else if (filter is "REJECTED" or "PRICE_REJECTED")
            rows = rows.Where(item => item.GovernanceStatus == MaterialGovernanceStatus.REJECTED);
        else if (filter is "INVALID" or "PRICE_INVALID" or "EXPIRED" or "PRICE_EXPIRED")
            rows = rows.Where(item =>
                item.GovernanceStatus != MaterialGovernanceStatus.ACTIVE
                && item.GovernanceStatus != MaterialGovernanceStatus.APPROVED
                && item.GovernanceStatus != MaterialGovernanceStatus.REJECTED
                && (item.UnitCost != null || item.StandardPrice != null || item.MovingAveragePrice != null));
        if (!string.IsNullOrWhiteSpace(query))
        {
            var term = query.Trim().ToUpperInvariant();
            var supplierHits = db.SupplierMaterials.Where(link =>
                link.Supplier.OrganizationId == organizationId &&
                (link.Supplier.SupplierCode.ToUpper().Contains(term)
                    || link.Supplier.Name.ToUpper().Contains(term)
                    || link.Supplier.NormalizedName.Contains(term)
                    || (link.SupplierMaterialCode != null && link.SupplierMaterialCode.ToUpper().Contains(term))))
                .Select(link => link.MaterialId);
            rows = rows.Where(item =>
                item.MaterialCode.ToUpper().Contains(term) ||
                item.Description.ToUpper().Contains(term) ||
                item.NormalizedDescription.Contains(term) ||
                (item.Category != null && item.Category.ToUpper().Contains(term)) ||
                (item.MaterialGroup != null && item.MaterialGroup.ToUpper().Contains(term)) ||
                (item.MaterialType != null && item.MaterialType.ToUpper().Contains(term)) ||
                (item.CompanyCode != null && item.CompanyCode.ToUpper().Contains(term)) ||
                (item.ValuationArea != null && item.ValuationArea.ToUpper().Contains(term)) ||
                supplierHits.Contains(item.Id));
        }
        if (!string.IsNullOrWhiteSpace(cursor))
            rows = rows.Where(item => item.MaterialCode.CompareTo(cursor) > 0);
        var termKey = query?.Trim().ToUpperInvariant() ?? string.Empty;
        var materials = await rows
            .OrderBy(item => termKey != "" && item.MaterialCode.ToUpper() == termKey ? 0 : 1)
            .ThenBy(item => item.MaterialCode)
            .Take(size + 1)
            .ToListAsync(cancellationToken);
        var page = materials.Take(size).ToList();
        var next = materials.Count > size ? page[^1].MaterialCode : null;
        var valuations = await db.MaterialValuations.AsNoTracking()
            .Where(item => page.Select(row => row.Id).Contains(item.MaterialId))
            .ToListAsync(cancellationToken);
        var conversions = await db.MaterialUomConversions.AsNoTracking()
            .Where(item => page.Select(row => row.Id).Contains(item.MaterialId) && item.IsActive)
            .ToListAsync(cancellationToken);
        var pending = await db.MaterialChangeRequests.AsNoTracking()
            .Where(item => item.Status == MaterialGovernanceStatus.PENDING_APPROVAL && page.Select(row => row.Id).Contains(item.MaterialId!.Value))
            .ToListAsync(cancellationToken);
        var suppliers = await SupplierNamesAsync(page.Select(row => row.Id).ToList(), cancellationToken);
        return new PageResult<MaterialMasterRow>(
            page.Select(item => ToRow(item, valuations.Where(row => row.MaterialId == item.Id).ToList(), conversions.Where(row => row.MaterialId == item.Id).ToList(),
                pending.FirstOrDefault(row => row.MaterialId == item.Id), suppliers.GetValueOrDefault(item.Id))).ToList(),
            next, size, page.Count);
    }

    public async Task<MaterialMasterRow> GetAsync(Guid organizationId, Guid id, CancellationToken cancellationToken)
    {
        var material = await db.Materials.AsNoTracking().SingleOrDefaultAsync(item => item.Id == id && item.OrganizationId == organizationId, cancellationToken)
            ?? throw new RecipeManagementException("MATERIAL_NOT_FOUND", "The material was not found.", 404);
        var valuations = await db.MaterialValuations.AsNoTracking().Where(item => item.MaterialId == material.Id).ToListAsync(cancellationToken);
        var conversions = await db.MaterialUomConversions.AsNoTracking().Where(item => item.MaterialId == material.Id && item.IsActive).ToListAsync(cancellationToken);
        var pending = await db.MaterialChangeRequests.AsNoTracking().FirstOrDefaultAsync(item => item.MaterialId == material.Id && item.Status == MaterialGovernanceStatus.PENDING_APPROVAL, cancellationToken);
        var suppliers = await SupplierNamesAsync([material.Id], cancellationToken);
        return ToRow(material, valuations, conversions, pending, suppliers.GetValueOrDefault(material.Id));
    }

    public async Task<MaterialSearchFacets> FacetsAsync(Guid organizationId, CancellationToken cancellationToken)
    {
        var categories = await db.Materials.AsNoTracking()
            .Where(item => item.OrganizationId == organizationId && item.Category != null && item.Category != "")
            .Select(item => item.Category!)
            .Distinct().OrderBy(item => item).Take(200).ToListAsync(cancellationToken);
        var groups = await db.Materials.AsNoTracking()
            .Where(item => item.OrganizationId == organizationId && item.MaterialGroup != null && item.MaterialGroup != "")
            .Select(item => item.MaterialGroup!)
            .Distinct().OrderBy(item => item).Take(200).ToListAsync(cancellationToken);
        var types = await db.Materials.AsNoTracking()
            .Where(item => item.OrganizationId == organizationId && item.MaterialType != null && item.MaterialType != "")
            .Select(item => item.MaterialType!)
            .Distinct().OrderBy(item => item).Take(200).ToListAsync(cancellationToken);
        var suppliers = await db.SupplierMaterials.AsNoTracking()
            .Where(link => link.Supplier.OrganizationId == organizationId)
            .Select(link => new { link.SupplierId, link.Supplier.SupplierCode, link.Supplier.Name })
            .Distinct()
            .OrderBy(item => item.Name)
            .Take(200)
            .ToListAsync(cancellationToken);
        return new MaterialSearchFacets(categories, groups, types,
            suppliers.Select(item => new MaterialSupplierFacet(item.SupplierId, item.SupplierCode, item.Name)).ToList());
    }

    public async Task<MaterialMasterRow> CreateAsync(Session session, Guid organizationId, MaterialUpsertRequest request, CancellationToken cancellationToken)
    {
        var record = FromRequest(request, MaterialAcquisitionSource.MANUAL);
        Validate(record);
        if (await db.Materials.AnyAsync(item => item.OrganizationId == organizationId && item.MaterialCode == record.MaterialCode, cancellationToken))
            throw new RecipeManagementException("MATERIAL_ALREADY_EXISTS", "A material with this ID already exists.", 409);
        var now = DateTime.UtcNow;
        var material = new Material
        {
            Id = Guid.NewGuid(), OrganizationId = organizationId, MaterialCode = record.MaterialCode,
            Description = record.Name, NormalizedDescription = RecipeCosting.NormalizeKey(record.Name),
            BaseUom = record.BaseUom, AcquisitionSource = MaterialAcquisitionSource.MANUAL, SourceSystem = "MANUAL",
            GovernanceStatus = MaterialGovernanceStatus.DRAFT, Status = StatusKind.INACTIVE, CreatedAt = now, UpdatedAt = now,
        };
        MaterialNormalized.Apply(material, record, operational: false);
        material.GovernanceStatus = MaterialGovernanceStatus.DRAFT;
        material.Status = StatusKind.INACTIVE;
        db.Materials.Add(material);
        await db.SaveChangesAsync(cancellationToken);
        await PersistConversionAsync(db, material, record, "MANUAL", cancellationToken);
        await approvals.SubmitAsync(organizationId, material, record, RecipeApprovalEvent.CREATE_MATERIAL, MaterialAcquisitionSource.MANUAL, session.CustomerUserId(), cancellationToken);
        return await GetAsync(organizationId, material.Id, cancellationToken);
    }

    public async Task<MaterialMasterRow> ChangeAsync(Session session, Guid organizationId, Guid id, MaterialUpsertRequest request, CancellationToken cancellationToken)
    {
        var material = await db.Materials.SingleOrDefaultAsync(item => item.Id == id && item.OrganizationId == organizationId, cancellationToken)
            ?? throw new RecipeManagementException("MATERIAL_NOT_FOUND", "The material was not found.", 404);
        var record = FromRequest(request, material.AcquisitionSource == MaterialAcquisitionSource.ERP ? MaterialAcquisitionSource.MANUAL : material.AcquisitionSource);
        Validate(record);
        if (material.GovernanceStatus is MaterialGovernanceStatus.ACTIVE or MaterialGovernanceStatus.APPROVED)
        {
            if (material.PendingChangeRequestId is not null)
                throw new RecipeManagementException("MATERIAL_CHANGE_PENDING", "A change is already awaiting approval for this material.");
            await PersistConversionAsync(db, material, record, record.Source.ToString(), cancellationToken);
            await approvals.SubmitAsync(organizationId, material, record, RecipeApprovalEvent.CHANGE_MATERIAL, record.Source, session.CustomerUserId(), cancellationToken);
            return await GetAsync(organizationId, material.Id, cancellationToken);
        }
        MaterialNormalized.Apply(material, record, operational: false);
        material.GovernanceStatus = MaterialGovernanceStatus.DRAFT;
        material.Status = StatusKind.INACTIVE;
        await PersistConversionAsync(db, material, record, record.Source.ToString(), cancellationToken);
        await approvals.SubmitAsync(organizationId, material, record, RecipeApprovalEvent.CREATE_MATERIAL, record.Source, session.CustomerUserId(), cancellationToken);
        return await GetAsync(organizationId, material.Id, cancellationToken);
    }

    public async Task SubmitAsync(Session session, Guid organizationId, Guid id, CancellationToken cancellationToken)
    {
        var material = await db.Materials.SingleOrDefaultAsync(item => item.Id == id && item.OrganizationId == organizationId, cancellationToken)
            ?? throw new RecipeManagementException("MATERIAL_NOT_FOUND", "The material was not found.", 404);
        if (material.PendingChangeRequestId is not null)
            throw new RecipeManagementException("MATERIAL_CHANGE_PENDING", "This material is already in approval.");
        var valuations = await db.MaterialValuations.Where(item => item.MaterialId == material.Id).ToListAsync(cancellationToken);
        var record = MaterialNormalized.FromEntity(material, valuations);
        var eventKind = material.GovernanceStatus is MaterialGovernanceStatus.ACTIVE or MaterialGovernanceStatus.APPROVED
            ? RecipeApprovalEvent.CHANGE_MATERIAL
            : RecipeApprovalEvent.CREATE_MATERIAL;
        await approvals.SubmitAsync(organizationId, material, record, eventKind, material.AcquisitionSource, session.CustomerUserId(), cancellationToken);
    }

    public async Task DeactivateOrDeleteAsync(Guid organizationId, Guid id, bool hardDelete, CancellationToken cancellationToken)
    {
        var material = await db.Materials.SingleOrDefaultAsync(item => item.Id == id && item.OrganizationId == organizationId, cancellationToken)
            ?? throw new RecipeManagementException("MATERIAL_NOT_FOUND", "The material was not found.", 404);
        var referenced = await db.RecipeIngredients.AnyAsync(item => item.MaterialId == material.Id, cancellationToken)
            || await db.RecipeConsumptionLines.AnyAsync(item => item.MaterialId == material.Id, cancellationToken);
        if (referenced || !hardDelete || material.GovernanceStatus is MaterialGovernanceStatus.ACTIVE or MaterialGovernanceStatus.APPROVED)
        {
            material.Status = StatusKind.INACTIVE;
            material.GovernanceStatus = MaterialGovernanceStatus.INACTIVE;
            material.UpdatedAt = DateTime.UtcNow;
            await db.SaveChangesAsync(cancellationToken);
            return;
        }
        db.Materials.Remove(material);
        await db.SaveChangesAsync(cancellationToken);
    }

    public async Task<MaterialImportPreviewResponse> PreviewAsync(Guid organizationId, Stream file, string fileName, CancellationToken cancellationToken)
    {
        IReadOnlyList<ExcelOpenXml.Sheet> sheets;
        try { sheets = ExcelOpenXml.ReadSheets(file); }
        catch (InvalidDataException) { throw new RecipeManagementException("IMPORT_FILE_INVALID", "Upload a valid .xlsx Material Master file."); }
        var materialSheet = ExcelOpenXml.FindSheet(sheets, "Materials", "Data") ?? sheets[0];
        var parsed = ExcelOpenXml.ToDictionaries(materialSheet.Rows).Select(MaterialNormalized.FromExcel).ToList();
        var merged = MaterialNormalized.MergeByProduct(parsed);
        var existing = await db.Materials.AsNoTracking().Where(item => item.OrganizationId == organizationId).ToListAsync(cancellationToken);
        var byCode = existing.ToDictionary(item => item.MaterialCode, StringComparer.OrdinalIgnoreCase);
        var preview = new List<MaterialImportRowResponse>();
        var index = 2;
        foreach (var record in merged)
        {
            var errors = new List<string>();
            if (string.IsNullOrWhiteSpace(record.MaterialCode)) errors.Add("Material ID is required.");
            if (string.IsNullOrWhiteSpace(record.Name)) errors.Add("Material name is required.");
            if (string.IsNullOrWhiteSpace(record.BaseUom)) errors.Add("Base UOM is required.");
            try { MaterialNormalized.NormalizeInventoryType(record.InventoryType); }
            catch (RecipeManagementException exception) { errors.Add(exception.Message); }
            var action = "NEW";
            if (byCode.TryGetValue(record.MaterialCode, out var current))
                action = MaterialNormalized.SameOperational(record, current, []) ? "UNCHANGED" : "CHANGED";
            preview.Add(new MaterialImportRowResponse(index++, errors.Count == 0, action, MaterialNormalized.ToExcel(record), errors));
        }
        return new MaterialImportPreviewResponse(fileName, preview.Count, preview.Count(item => item.IsValid), preview.Count(item => !item.IsValid),
            preview.Count(item => item.IsValid && item.Action == "NEW"), preview.Count(item => item.IsValid && item.Action == "CHANGED"),
            preview.Count(item => item.IsValid && item.Action == "UNCHANGED"), preview);
    }

    public async Task<MaterialImportPreviewResponse> ImportAsync(Session session, Guid organizationId, Stream file, string fileName, CancellationToken cancellationToken)
    {
        if (file.CanSeek) file.Position = 0;
        IReadOnlyList<ExcelOpenXml.Sheet> sheets;
        try { sheets = ExcelOpenXml.ReadSheets(file); }
        catch (InvalidDataException) { throw new RecipeManagementException("IMPORT_FILE_INVALID", "Upload a valid .xlsx Material Master file."); }
        if (file.CanSeek) file.Position = 0;
        var preview = await PreviewAsync(organizationId, file, fileName, cancellationToken);
        var records = preview.Rows.Where(item => item.IsValid)
            .Select(item => MaterialNormalized.FromExcel(item.Values) with { Source = MaterialAcquisitionSource.EXCEL, SourceSystem = "EXCEL" })
            .ToList();
        foreach (var record in MaterialNormalized.MergeByProduct(records))
        {
            try { await ApplyIncomingAsync(session, organizationId, record, cancellationToken); }
            catch (Exception) { /* keep remaining Excel rows importing */ }
        }
        await ImportValuationsAsync(session, organizationId, sheets, cancellationToken);
        await ImportConversionsAsync(organizationId, sheets, cancellationToken);
        return preview;
    }

    public async Task<byte[]> ExportAsync(Guid organizationId, CancellationToken cancellationToken)
    {
        var rows = await ListAsync(organizationId, null, null, null, null, null, null, null, cancellationToken);
        return ExcelOpenXml.WriteSheets(
        [
            new ExcelOpenXml.SheetWrite("Materials", MaterialNormalized.MaterialsSheetColumns, rows.Select(item => new Dictionary<string, string?>
            {
                ["MaterialID"] = item.MaterialCode,
                ["Description"] = item.Description,
                ["MaterialType"] = item.MaterialType,
                ["MaterialGroup"] = item.MaterialGroup,
                ["Family"] = item.Category,
                ["Category"] = item.Category,
                ["BaseUOM"] = item.BaseUom,
                ["ConvFactor"] = item.ConvFactor?.ToString(CultureInfo.InvariantCulture),
                ["ConvUnit"] = item.ConvUnit,
                ["ConvValue"] = item.ConvValue?.ToString(CultureInfo.InvariantCulture),
                ["Active"] = item.ActiveStatus == "ACTIVE" ? "Y" : "N",
            }).ToList()),
            new ExcelOpenXml.SheetWrite("MaterialValuation", MaterialNormalized.ValuationSheetColumns, rows.SelectMany(item =>
                (item.Valuations.Count == 0
                    ? [new MaterialValuationRecord(item.CompanyCode, item.ValuationArea ?? "DEFAULT", item.ValuationClass, item.PriceControl, item.StandardPrice, item.MovingAveragePrice, item.Currency)]
                    : item.Valuations).Select(valuation => new Dictionary<string, string?>
                {
                    ["MaterialID"] = item.MaterialCode,
                    ["ValuationArea"] = valuation.ValuationArea,
                    ["Plant"] = valuation.ValuationArea,
                    ["PriceControl"] = valuation.PriceControl ?? item.PriceControl,
                    ["StandardPrice"] = valuation.StandardPrice?.ToString(CultureInfo.InvariantCulture),
                    ["MovingAveragePrice"] = valuation.MovingAveragePrice?.ToString(CultureInfo.InvariantCulture),
                    ["EffectivePrice"] = item.UnitCost?.ToString(CultureInfo.InvariantCulture),
                    ["PriceUOM"] = item.BaseUom,
                    ["Currency"] = valuation.Currency ?? item.Currency,
                })).ToList()),
            new ExcelOpenXml.SheetWrite("UOMConversions", MaterialNormalized.ConversionSheetColumns, rows.SelectMany(item => item.Conversions.Select(row => new Dictionary<string, string?>
            {
                ["MaterialID"] = item.MaterialCode,
                ["FromUOM"] = row.FromUom,
                ["ToUOM"] = row.ToUom,
                ["Numerator"] = row.Numerator.ToString(CultureInfo.InvariantCulture),
                ["Denominator"] = row.Denominator.ToString(CultureInfo.InvariantCulture),
                ["ConvFactor"] = row.Denominator.ToString(CultureInfo.InvariantCulture),
                ["ConvUnit"] = row.ToUom,
                ["ConvValue"] = row.Numerator.ToString(CultureInfo.InvariantCulture),
                ["PackSize"] = row.PackSize?.ToString(CultureInfo.InvariantCulture),
                ["PackUOM"] = row.PackUom,
                ["Active"] = row.IsActive ? "Y" : "N",
            })).ToList()),
        ]);
    }

    public byte[] Template() => ExcelOpenXml.WriteSheets(
    [
        new ExcelOpenXml.SheetWrite("Materials", MaterialNormalized.MaterialsSheetColumns, []),
        new ExcelOpenXml.SheetWrite("MaterialValuation", MaterialNormalized.ValuationSheetColumns, []),
        new ExcelOpenXml.SheetWrite("UOMConversions", MaterialNormalized.ConversionSheetColumns, []),
    ]);

    public async Task<MaterialErpRouteResponse> ResolveErpAsync(Guid organizationId, string companyCode, CancellationToken cancellationToken)
    {
        var resolved = await routes.ResolveAsync(organizationId, IntegrationProcessType.GET_MATERIAL, companyCode, cancellationToken);
        var sync = await db.MaterialErpSyncStates.AsNoTracking()
            .SingleOrDefaultAsync(item => item.OrganizationId == organizationId && item.CompanyCode == companyCode.Trim().ToUpperInvariant(), cancellationToken);
        return new MaterialErpRouteResponse(companyCode.Trim().ToUpperInvariant(), resolved.SystemKind.ToString(), resolved.ConfigurationName,
            resolved.Configuration.Id, resolved.RouteId, sync?.LastSyncAt, sync?.LastStatus);
    }

    public async Task<MaterialErpPullResult> PullFromErpAsync(Session session, Guid organizationId, string companyCode, CancellationToken cancellationToken)
    {
        var code = companyCode.Trim().ToUpperInvariant();
        if (string.IsNullOrWhiteSpace(code))
            throw new RecipeManagementException("COMPANY_CODE_REQUIRED", "Select a Company Code to resolve GET_MATERIAL routing.");
        var resolved = await routes.ResolveAsync(organizationId, IntegrationProcessType.GET_MATERIAL, code, cancellationToken);
        IReadOnlyList<MaterialNormalizedRecord> records;
        try
        {
            records = await sap.PullAsync(resolved.Configuration, code, cancellationToken);
        }
        catch (IntegrationException exception)
        {
            await SaveSyncAsync(organizationId, code, resolved, 0, 0, 0, 0, 1, exception.Message, "FAILED", cancellationToken);
            throw new RecipeManagementException(exception.Code, exception.Message, exception.Status);
        }

        var failures = new List<string>();
        var created = 0; var changed = 0; var unchanged = 0;
        foreach (var record in records)
        {
            try
            {
                var outcome = await ApplyIncomingAsync(session, organizationId, record with { CompanyCode = record.CompanyCode ?? code, Source = MaterialAcquisitionSource.ERP, SourceSystem = "SAP_S4HANA" }, cancellationToken);
                if (outcome == "NEW") created++;
                else if (outcome == "CHANGED") changed++;
                else unchanged++;
            }
            catch (Exception exception)
            {
                failures.Add($"{record.MaterialCode}: {exception.Message}");
            }
        }
        var now = DateTime.UtcNow;
        await SaveSyncAsync(organizationId, code, resolved, records.Count, created, changed, unchanged, failures.Count, failures.Count == 0 ? null : string.Join("; ", failures.Take(8)), failures.Count == 0 ? "SUCCEEDED" : "PARTIAL", cancellationToken);
        return new MaterialErpPullResult(code, resolved.SystemKind.ToString(), resolved.ConfigurationName, records.Count, created, changed, unchanged, failures.Count, failures, now);
    }

    private async Task ImportValuationsAsync(Session session, Guid organizationId, IReadOnlyList<ExcelOpenXml.Sheet> sheets, CancellationToken cancellationToken)
    {
        var sheet = ExcelOpenXml.FindSheet(sheets, "MaterialValuation", "Valuations");
        if (sheet is null) return;
        foreach (var values in ExcelOpenXml.ToDictionaries(sheet.Rows))
        {
            var code = RecipeExcel.Get(values, "MaterialID", "Material ID");
            if (string.IsNullOrWhiteSpace(code)) continue;
            var material = await db.Materials.SingleOrDefaultAsync(item => item.OrganizationId == organizationId && item.MaterialCode == code, cancellationToken);
            if (material is null) continue;
            var record = MaterialNormalized.FromExcel(values) with
            {
                MaterialCode = material.MaterialCode,
                Name = material.Description,
                Description = material.Description,
                BaseUom = material.BaseUom,
                Source = MaterialAcquisitionSource.EXCEL,
                SourceSystem = "EXCEL",
            };
            await ApplyIncomingAsync(session, organizationId, record, cancellationToken);
        }
    }

    private async Task ImportConversionsAsync(Guid organizationId, IReadOnlyList<ExcelOpenXml.Sheet> sheets, CancellationToken cancellationToken)
    {
        var sheet = ExcelOpenXml.FindSheet(sheets, "UOMConversions", "UOM Conversion", "Conversions");
        if (sheet is null) return;
        var now = DateTime.UtcNow;
        foreach (var values in ExcelOpenXml.ToDictionaries(sheet.Rows))
        {
            var code = RecipeExcel.Get(values, "MaterialID", "Material ID");
            if (string.IsNullOrWhiteSpace(code)) continue;
            var material = await db.Materials.SingleOrDefaultAsync(item => item.OrganizationId == organizationId && item.MaterialCode == code, cancellationToken);
            if (material is null) continue;
            var from = RecipeUom.Normalize(RecipeExcel.Get(values, "FromUOM", "From UOM") ?? material.BaseUom);
            var to = RecipeUom.Normalize(RecipeExcel.Get(values, "ToUOM", "To UOM", "ConvUnit") ?? "");
            var numerator = RecipeExcel.Decimal(values, "ConvValue", "Numerator") ?? 1;
            var denominator = RecipeExcel.Decimal(values, "ConvFactor", "Denominator") ?? 1;
            if (string.IsNullOrWhiteSpace(from) || string.IsNullOrWhiteSpace(to) || numerator <= 0 || denominator <= 0) continue;
            var row = await db.MaterialUomConversions.FirstOrDefaultAsync(item => item.MaterialId == material.Id && item.FromUom == from && item.ToUom == to, cancellationToken);
            if (row is null)
            {
                row = new MaterialUomConversion { Id = Guid.NewGuid(), MaterialId = material.Id, FromUom = from, ToUom = to, CreatedAt = now };
                db.MaterialUomConversions.Add(row);
            }
            row.Numerator = numerator;
            row.Denominator = denominator;
            row.PackSize = RecipeExcel.Decimal(values, "PackSize", "Pack Size");
            row.PackUom = string.IsNullOrWhiteSpace(RecipeExcel.Get(values, "PackUOM", "Pack UOM")) ? null : RecipeUom.Normalize(RecipeExcel.Get(values, "PackUOM", "Pack UOM"));
            row.Source = "EXCEL";
            var active = RecipeExcel.Get(values, "Active", "Status");
            row.IsActive = !string.Equals(active, "N", StringComparison.OrdinalIgnoreCase) && !string.Equals(active, "INACTIVE", StringComparison.OrdinalIgnoreCase);
            row.UpdatedAt = now;
            if (string.Equals(from, RecipeUom.Normalize(material.BaseUom), StringComparison.OrdinalIgnoreCase))
            {
                material.ConvFactor = denominator;
                material.ConvUnit = to;
                material.ConvValue = numerator;
                if (string.IsNullOrWhiteSpace(material.AlternateUom)) material.AlternateUom = to;
            }
        }
        await db.SaveChangesAsync(cancellationToken);
    }

    private async Task<string> ApplyIncomingAsync(Session session, Guid organizationId, MaterialNormalizedRecord record, CancellationToken cancellationToken)
    {
        Validate(record);
        var now = DateTime.UtcNow;
        var existing = await db.Materials.SingleOrDefaultAsync(item => item.OrganizationId == organizationId && item.MaterialCode == record.MaterialCode, cancellationToken);
        if (existing is null)
        {
            var material = new Material
            {
                Id = Guid.NewGuid(), OrganizationId = organizationId, MaterialCode = record.MaterialCode,
                Description = record.Name, NormalizedDescription = RecipeCosting.NormalizeKey(record.Name),
                BaseUom = string.IsNullOrWhiteSpace(record.BaseUom) ? "EA" : record.BaseUom,
                AcquisitionSource = record.Source, SourceSystem = record.SourceSystem ?? record.Source.ToString(),
                GovernanceStatus = MaterialGovernanceStatus.DRAFT, Status = StatusKind.INACTIVE, CreatedAt = now, UpdatedAt = now,
                ImportedAt = now, LastSynchronizedAt = record.Source == MaterialAcquisitionSource.ERP ? now : null,
            };
            MaterialNormalized.Apply(material, record, operational: false);
            material.GovernanceStatus = MaterialGovernanceStatus.DRAFT;
            material.Status = StatusKind.INACTIVE;
            db.Materials.Add(material);
            await db.SaveChangesAsync(cancellationToken);
            await MaterialApprovalService.ReplaceValuationsAsync(db, material.Id, record, cancellationToken);
            await PersistConversionAsync(db, material, record, record.Source.ToString(), cancellationToken);
            await approvals.SubmitAsync(organizationId, material, record, RecipeApprovalEvent.CREATE_MATERIAL, record.Source, session.CustomerUserId(), cancellationToken);
            return "NEW";
        }

        existing.LastSynchronizedAt = record.Source == MaterialAcquisitionSource.ERP ? now : existing.LastSynchronizedAt;
        var valuations = await db.MaterialValuations.Where(item => item.MaterialId == existing.Id).ToListAsync(cancellationToken);
        if (MaterialNormalized.SameOperational(record, existing, valuations))
        {
            await PersistConversionAsync(db, existing, record, record.Source.ToString(), cancellationToken);
            return "UNCHANGED";
        }
        if (existing.GovernanceStatus is MaterialGovernanceStatus.ACTIVE or MaterialGovernanceStatus.APPROVED)
        {
            await PersistConversionAsync(db, existing, record, record.Source.ToString(), cancellationToken);
            if (existing.PendingChangeRequestId is not null) return "UNCHANGED";
            await approvals.SubmitAsync(organizationId, existing, record, RecipeApprovalEvent.CHANGE_MATERIAL, record.Source, session.CustomerUserId(), cancellationToken);
            return "CHANGED";
        }
        MaterialNormalized.Apply(existing, record, operational: false);
        existing.GovernanceStatus = MaterialGovernanceStatus.DRAFT;
        existing.Status = StatusKind.INACTIVE;
        await MaterialApprovalService.ReplaceValuationsAsync(db, existing.Id, record, cancellationToken);
        await PersistConversionAsync(db, existing, record, record.Source.ToString(), cancellationToken);
        if (existing.PendingChangeRequestId is null)
            await approvals.SubmitAsync(organizationId, existing, record, RecipeApprovalEvent.CREATE_MATERIAL, record.Source, session.CustomerUserId(), cancellationToken);
        return "CHANGED";
    }

    private async Task SaveSyncAsync(Guid organizationId, string companyCode, ResolvedIntegrationRoute resolved, int read, int created, int changed, int unchanged, int failed, string? error, string status, CancellationToken cancellationToken)
    {
        var row = await db.MaterialErpSyncStates.SingleOrDefaultAsync(item => item.OrganizationId == organizationId && item.CompanyCode == companyCode, cancellationToken);
        if (row is null)
        {
            row = new MaterialErpSyncState { Id = Guid.NewGuid(), OrganizationId = organizationId, CompanyCode = companyCode };
            db.MaterialErpSyncStates.Add(row);
        }
        row.IntegrationRouteId = resolved.RouteId;
        row.IntegrationConfigurationId = resolved.Configuration.Id;
        row.LastReadCount = read;
        row.LastNewCount = created;
        row.LastChangedCount = changed;
        row.LastUnchangedCount = unchanged;
        row.LastFailedCount = failed;
        row.LastError = error;
        row.LastStatus = status;
        row.LastSyncAt = DateTime.UtcNow;
        await db.SaveChangesAsync(cancellationToken);
    }

    private static MaterialNormalizedRecord FromRequest(MaterialUpsertRequest request, MaterialAcquisitionSource source) => new(
        (request.MaterialCode ?? string.Empty).Trim(), (request.Name ?? string.Empty).Trim(), request.Description, request.MaterialType, request.MaterialGroup,
        request.Category, (request.BaseUom ?? string.Empty).Trim(), request.AlternateUom, request.CompanyCode, request.ValuationArea,
        request.ValuationClass, request.PriceControl, request.StandardPrice, request.MovingAveragePrice, request.Currency,
        request.UnitCost, source, source.ToString(), null, request.Active ?? true,
        string.IsNullOrWhiteSpace(request.ValuationArea) ? [] : [new MaterialValuationRecord(request.CompanyCode, request.ValuationArea, request.ValuationClass, request.PriceControl, request.StandardPrice, request.MovingAveragePrice, request.Currency)],
        request.ConvFactor, request.ConvUnit, request.ConvValue,
        request.InventoryItem ?? false, MaterialNormalized.NormalizeInventoryType(request.InventoryType),
        request.BatchManaged ?? false, request.ExpiryManaged ?? false, request.ShelfLifeDays, request.SerialManaged ?? false);

    private static void Validate(MaterialNormalizedRecord record)
    {
        if (string.IsNullOrWhiteSpace(record.MaterialCode) || string.IsNullOrWhiteSpace(record.Name) || string.IsNullOrWhiteSpace(record.BaseUom))
            throw new RecipeManagementException("MATERIAL_REQUIRED_FIELDS", "Material ID, name, and base UOM are required.");
        MaterialNormalized.NormalizeInventoryType(record.InventoryType);
    }

    private static async Task PersistConversionAsync(SilaMeDbContext db, Material material, MaterialNormalizedRecord record, string? source, CancellationToken cancellationToken)
    {
        MaterialNormalized.ApplyConversion(material, record);
        await SyncAlternateConversionAsync(db, material, source, cancellationToken);
        await db.SaveChangesAsync(cancellationToken);
    }

    public static async Task SyncAlternateConversionAsync(SilaMeDbContext db, Material material, string? source, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(material.ConvUnit) || material.ConvValue is not > 0) return;
        var from = RecipeUom.Normalize(material.BaseUom);
        var to = RecipeUom.Normalize(material.ConvUnit);
        var factor = material.ConvFactor is > 0 ? material.ConvFactor.Value : 1m;
        var value = material.ConvValue.Value;
        if (string.IsNullOrWhiteSpace(from) || string.IsNullOrWhiteSpace(to) || from == to) return;
        var now = DateTime.UtcNow;
        var row = await db.MaterialUomConversions.FirstOrDefaultAsync(item => item.MaterialId == material.Id && item.FromUom == from && item.ToUom == to, cancellationToken);
        if (row is null)
        {
            row = new MaterialUomConversion { Id = Guid.NewGuid(), MaterialId = material.Id, FromUom = from, ToUom = to, CreatedAt = now };
            db.MaterialUomConversions.Add(row);
        }
        row.Numerator = value;
        row.Denominator = factor;
        row.PackSize = value;
        row.PackUom = to;
        row.Source = source ?? "MATERIAL";
        row.IsActive = true;
        row.UpdatedAt = now;
    }

    public async Task<MaterialMasterRow> ProposePriceAsync(Session session, Guid organizationId, Guid id, MaterialPriceUpdateRequest request, CancellationToken cancellationToken)
    {
        var material = await db.Materials.SingleOrDefaultAsync(item => item.Id == id && item.OrganizationId == organizationId, cancellationToken)
            ?? throw new RecipeManagementException("MATERIAL_NOT_FOUND", "The material was not found.", 404);
        if (material.PendingChangeRequestId is not null)
            throw new RecipeManagementException("MATERIAL_CHANGE_PENDING", "A change is already awaiting approval for this material.");
        if (request.UnitPrice < 0)
            throw new RecipeManagementException("MATERIAL_PRICE_INVALID", "Enter a unit price. Do not use a blank value as zero.");
        if (string.IsNullOrWhiteSpace(request.Comment))
            throw new RecipeManagementException("MATERIAL_PRICE_REASON_REQUIRED", "A reason is required to submit a Material unit price change.");
        var valuations = await db.MaterialValuations.Where(item => item.MaterialId == material.Id).ToListAsync(cancellationToken);
        var current = MaterialNormalized.FromEntity(material, valuations);
        var valuationArea = FirstNonEmpty(request.ValuationArea, material.ValuationArea) ?? "DEFAULT";
        var currency = FirstNonEmpty(request.Currency, material.Currency)?.ToUpperInvariant();
        var control = FirstNonEmpty(request.PriceControl, material.PriceControl) ?? "V";
        var priceUom = FirstNonEmpty(request.PriceUom, material.BaseUom) ?? material.BaseUom;
        var plant = FirstNonEmpty(request.Plant);
        var effectiveFrom = request.EffectiveFrom ?? DateTime.UtcNow;
        var proposedValuations = (current.Valuations ?? []).ToList();
        var existingValuation = proposedValuations.FindIndex(item => string.Equals(item.ValuationArea, valuationArea, StringComparison.OrdinalIgnoreCase));
        var valuation = new MaterialValuationRecord(request.CompanyCode ?? material.CompanyCode, valuationArea, material.ValuationClass, control,
            control is "S" or "STANDARD" ? request.UnitPrice : current.StandardPrice,
            control is "S" or "STANDARD" ? current.MovingAveragePrice : request.UnitPrice, currency,
            plant, priceUom, effectiveFrom, null);
        if (existingValuation >= 0) proposedValuations[existingValuation] = valuation;
        else proposedValuations.Add(valuation);
        var proposed = current with
        {
            UnitCost = request.UnitPrice,
            StandardPrice = control is "S" or "STANDARD" ? request.UnitPrice : current.StandardPrice,
            MovingAveragePrice = control is "S" or "STANDARD" ? current.MovingAveragePrice : request.UnitPrice,
            Currency = currency,
            PriceControl = control,
            CompanyCode = request.CompanyCode ?? current.CompanyCode,
            ValuationArea = valuationArea,
            Source = MaterialAcquisitionSource.MANUAL,
            SourceSystem = "RECIPE",
            Valuations = proposedValuations,
        };
        var eventKind = material.GovernanceStatus is MaterialGovernanceStatus.ACTIVE or MaterialGovernanceStatus.APPROVED
            ? RecipeApprovalEvent.CHANGE_MATERIAL
            : RecipeApprovalEvent.CREATE_MATERIAL;
        var currentApproved = MaterialCosting.EffectiveUnitCost(material);
        await approvals.SubmitAsync(organizationId, material, proposed, eventKind, MaterialAcquisitionSource.MANUAL, session.CustomerUserId(), cancellationToken);
        var change = await db.MaterialChangeRequests.SingleAsync(item => item.Id == material.PendingChangeRequestId, cancellationToken);
        change.Reason = request.Comment.Trim();
        change.CurrentUnitPrice = currentApproved;
        change.ProposedUnitPrice = request.UnitPrice;
        change.PriceUom = priceUom;
        change.Currency = currency;
        change.EffectiveFrom = effectiveFrom;
        await db.SaveChangesAsync(cancellationToken);
        return await GetAsync(organizationId, material.Id, cancellationToken);
    }

    private static string? FirstNonEmpty(params string?[] values) => values.FirstOrDefault(item => !string.IsNullOrWhiteSpace(item))?.Trim();

    private static MaterialMasterRow ToRow(Material item, IReadOnlyList<MaterialValuation> valuations, IReadOnlyList<MaterialUomConversion> conversions, MaterialChangeRequest? pending, IReadOnlyList<string>? suppliers = null)
    {
        var current = MaterialNormalized.CurrentValuations(valuations);
        var primary = current.FirstOrDefault(row => string.Equals(row.ValuationArea, item.ValuationArea, StringComparison.OrdinalIgnoreCase))
            ?? current.FirstOrDefault();
        var status = MaterialCosting.PriceStatus(item, pending, primary?.EffectiveFrom, primary?.EffectiveTo);
        var approvedPrice = MaterialCosting.EffectiveUnitCost(item);
        var approved = approvedPrice is not null;
        var alt = RecipeUom.Alternate(item, conversions);
        return new(
            item.Id, item.MaterialCode, item.Description, item.Description, item.MaterialGroup, item.MaterialType, item.Category,
            item.BaseUom, item.AlternateUom, item.CompanyCode, item.ValuationArea, item.ValuationClass, item.PriceControl,
            item.StandardPrice, item.MovingAveragePrice, approvedPrice, item.Currency,
            item.AcquisitionSource.ToString(), item.GovernanceStatus.ToString(), item.Status.ToString(), item.SourceSystem,
            item.UpdatedAt, item.LastSynchronizedAt, item.PendingChangeRequestId,
            current.Select(MaterialNormalized.ToValuationRecord).ToList(),
            approved, status, RecipeUom.PackSummary(item.BaseUom, conversions),
            conversions.Select(row => new MaterialUomConversionRow(row.Id, row.MaterialId, item.MaterialCode, row.FromUom, row.ToUom, row.Numerator, row.Denominator, row.PackSize, row.PackUom, row.Source, row.IsActive)).ToList(),
            pending?.ProposedUnitPrice ?? MaterialCosting.ProposedUnitPrice(pending), MaterialCosting.PriceStatusLabel(status),
            primary?.PriceUom ?? item.BaseUom, primary?.EffectiveFrom, primary?.Plant, pending is { Status: MaterialGovernanceStatus.PENDING_APPROVAL },
            approvedPrice, suppliers is { Count: > 0 } ? string.Join(" · ", suppliers) : null,
            alt?.Factor, alt?.Unit, alt?.Value, RecipeUom.Formula(item.BaseUom, alt?.Factor, alt?.Unit, alt?.Value),
            item.InventoryItem, item.InventoryType.ToString(), item.BatchManaged, item.ExpiryManaged, item.ShelfLifeDays, item.SerialManaged);
    }

    private async Task<Dictionary<Guid, List<string>>> SupplierNamesAsync(IReadOnlyList<Guid> materialIds, CancellationToken cancellationToken)
    {
        if (materialIds.Count == 0) return [];
        var links = await db.SupplierMaterials.AsNoTracking()
            .Where(link => materialIds.Contains(link.MaterialId))
            .Select(link => new { link.MaterialId, link.Supplier.SupplierCode, link.Supplier.Name })
            .ToListAsync(cancellationToken);
        return links
            .GroupBy(link => link.MaterialId)
            .ToDictionary(group => group.Key, group => group
                .Select(link => string.IsNullOrWhiteSpace(link.Name) ? link.SupplierCode : $"{link.SupplierCode} {link.Name}".Trim())
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList());
    }
}
