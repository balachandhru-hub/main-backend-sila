using System.Globalization;
using Microsoft.EntityFrameworkCore;
using SilaMe.Api.Auth;
using SilaMe.Api.Data;
using SilaMe.Api.DTOs;
using SilaMe.Api.Models;

namespace SilaMe.Api.Services;

public sealed class RecipeCatalogService(SilaMeDbContext db, RecipeApprovalService approvals)
{
    public async Task<RecipeDashboardResponse> DashboardAsync(Guid organizationId, CancellationToken cancellationToken) =>
        new(
            await db.Recipes.CountAsync(item => item.OrganizationId == organizationId, cancellationToken),
            await db.RecipeVersions.CountAsync(item => item.OrganizationId == organizationId && item.Status == RecipeStatus.ACTIVE, cancellationToken),
            await db.RecipeApprovalActions.CountAsync(item => item.Recipe.OrganizationId == organizationId && item.Status == RecipeApprovalActionStatus.PENDING, cancellationToken),
            await db.Materials.CountAsync(item => item.OrganizationId == organizationId && item.Status == StatusKind.ACTIVE, cancellationToken),
            await db.RecipeConsumptionTransactions.CountAsync(item => item.OrganizationId == organizationId && item.Status == RecipeTransactionStatus.FAILED, cancellationToken),
            await db.RecipeConsumptionTransactions.CountAsync(item => item.OrganizationId == organizationId && item.Status == RecipeTransactionStatus.POSTED, cancellationToken));

    public async Task<IReadOnlyList<RecipeCategoryRow>> ListCategoriesAsync(Guid organizationId, string? status, CancellationToken cancellationToken)
    {
        var rows = db.RecipeCategories.AsNoTracking().Where(item => item.OrganizationId == organizationId);
        if (Enum.TryParse<StatusKind>(status, true, out var parsed))
            rows = rows.Where(item => item.Status == parsed);
        return await rows.OrderBy(item => item.Name)
            .Select(item => new RecipeCategoryRow(item.Id, item.Code, item.Name, item.Description, item.Status.ToString()))
            .ToListAsync(cancellationToken);
    }

    public async Task<RecipeCategoryRow> UpsertCategoryAsync(Guid organizationId, Guid? id, RecipeCategoryUpsertRequest request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.Code) || string.IsNullOrWhiteSpace(request.Name))
            throw new RecipeManagementException("CATEGORY_REQUIRED", "Category code and name are required.");
        var category = id is null ? null : await db.RecipeCategories.SingleOrDefaultAsync(item => item.Id == id && item.OrganizationId == organizationId, cancellationToken);
        if (id is not null && category is null) throw new RecipeManagementException("CATEGORY_NOT_FOUND", "The recipe category was not found.", 404);
        var now = DateTime.UtcNow;
        category ??= new RecipeCategory { Id = Guid.NewGuid(), OrganizationId = organizationId, Code = request.Code.Trim(), Name = request.Name.Trim(), CreatedAt = now };
        if (id is null) db.RecipeCategories.Add(category);
        category.Code = request.Code.Trim();
        category.Name = request.Name.Trim();
        category.Description = request.Description?.Trim();
        category.Status = Enum.TryParse<StatusKind>(request.Status, true, out var parsed) ? parsed : StatusKind.ACTIVE;
        category.UpdatedAt = now;
        await db.SaveChangesAsync(cancellationToken);
        return new RecipeCategoryRow(category.Id, category.Code, category.Name, category.Description, category.Status.ToString());
    }

    public async Task<IReadOnlyList<RecipeFamilyRow>> ListFamiliesAsync(Guid organizationId, string? status, CancellationToken cancellationToken)
    {
        var rows = db.RecipeFamilies.AsNoTracking().Where(item => item.OrganizationId == organizationId);
        if (Enum.TryParse<StatusKind>(status, true, out var parsed))
            rows = rows.Where(item => item.Status == parsed);
        return await rows.OrderBy(item => item.Name)
            .Select(item => new RecipeFamilyRow(item.Id, item.Code, item.Name, item.Description, item.Status.ToString()))
            .ToListAsync(cancellationToken);
    }

    public async Task<RecipeFamilyRow> UpsertFamilyAsync(Guid organizationId, Guid? id, RecipeFamilyUpsertRequest request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.Code) || string.IsNullOrWhiteSpace(request.Name))
            throw new RecipeManagementException("FAMILY_REQUIRED", "Family code and name are required.");
        var family = id is null ? null : await db.RecipeFamilies.SingleOrDefaultAsync(item => item.Id == id && item.OrganizationId == organizationId, cancellationToken);
        if (id is not null && family is null) throw new RecipeManagementException("FAMILY_NOT_FOUND", "The recipe family was not found.", 404);
        var now = DateTime.UtcNow;
        family ??= new RecipeFamily { Id = Guid.NewGuid(), OrganizationId = organizationId, Code = request.Code.Trim(), Name = request.Name.Trim(), CreatedAt = now };
        if (id is null) db.RecipeFamilies.Add(family);
        family.Code = request.Code.Trim();
        family.Name = request.Name.Trim();
        family.Description = request.Description?.Trim();
        family.Status = Enum.TryParse<StatusKind>(request.Status, true, out var parsed) ? parsed : StatusKind.ACTIVE;
        family.UpdatedAt = now;
        await db.SaveChangesAsync(cancellationToken);
        return new RecipeFamilyRow(family.Id, family.Code, family.Name, family.Description, family.Status.ToString());
    }

    public byte[] MasterTemplate() => ExcelOpenXml.Write(["Code", "Name", "Description", "Active"], []);

    public async Task<byte[]> ExportFamiliesAsync(Guid organizationId, CancellationToken cancellationToken) =>
        ExcelOpenXml.Write(["Code", "Name", "Description", "Active"],
            (await ListFamiliesAsync(organizationId, null, cancellationToken)).Select(item => new Dictionary<string, string?>
            {
                ["Code"] = item.Code, ["Name"] = item.Name, ["Description"] = item.Description, ["Active"] = item.Status == "ACTIVE" ? "Y" : "N",
            }).ToList());

    public async Task<byte[]> ExportCategoriesAsync(Guid organizationId, CancellationToken cancellationToken) =>
        ExcelOpenXml.Write(["Code", "Name", "Description", "Active"],
            (await ListCategoriesAsync(organizationId, null, cancellationToken)).Select(item => new Dictionary<string, string?>
            {
                ["Code"] = item.Code, ["Name"] = item.Name, ["Description"] = item.Description, ["Active"] = item.Status == "ACTIVE" ? "Y" : "N",
            }).ToList());

    public RecipeImportPreviewResponse PreviewMaster(Stream file, string fileName)
    {
        var sheets = ExcelOpenXml.ReadSheets(file);
        var parsed = ExcelOpenXml.ToDictionaries(sheets[0].Rows);
        var rows = parsed.Select((values, index) =>
        {
            var errors = new List<string>();
            if (string.IsNullOrWhiteSpace(RecipeExcel.Get(values, "Code"))) errors.Add("Code is required.");
            if (string.IsNullOrWhiteSpace(RecipeExcel.Get(values, "Name"))) errors.Add("Name is required.");
            return new MaterialImportRowResponse(index + 2, errors.Count == 0, errors.Count == 0 ? "OK" : "INVALID", values, errors);
        }).ToList();
        return new RecipeImportPreviewResponse(fileName, rows.Count, rows.Count(item => item.IsValid), rows.Count(item => !item.IsValid), rows.Count(item => item.IsValid), rows);
    }

    public async Task<int> ImportFamiliesAsync(Guid organizationId, Stream file, string fileName, CancellationToken cancellationToken)
    {
        var preview = PreviewMaster(file, fileName);
        var count = 0;
        foreach (var row in preview.Rows.Where(item => item.IsValid))
        {
            var code = RecipeExcel.Get(row.Values!, "Code", "Family Code")!;
            var existing = await db.RecipeFamilies.SingleOrDefaultAsync(item => item.OrganizationId == organizationId && item.Code == code, cancellationToken);
            var active = RecipeExcel.Get(row.Values!, "Active", "Status");
            await UpsertFamilyAsync(organizationId, existing?.Id, new RecipeFamilyUpsertRequest(
                code, RecipeExcel.Get(row.Values!, "Name", "Family Name")!,
                RecipeExcel.Get(row.Values!, "Description"), ActiveStatus(active)), cancellationToken);
            count++;
        }
        return count;
    }

    public async Task<int> ImportCategoriesAsync(Guid organizationId, Stream file, string fileName, CancellationToken cancellationToken)
    {
        var preview = PreviewMaster(file, fileName);
        var count = 0;
        foreach (var row in preview.Rows.Where(item => item.IsValid))
        {
            var code = RecipeExcel.Get(row.Values!, "Code", "Category Code")!;
            var existing = await db.RecipeCategories.SingleOrDefaultAsync(item => item.OrganizationId == organizationId && item.Code == code, cancellationToken);
            var active = RecipeExcel.Get(row.Values!, "Active", "Status");
            await UpsertCategoryAsync(organizationId, existing?.Id, new RecipeCategoryUpsertRequest(
                code, RecipeExcel.Get(row.Values!, "Name", "Category Name")!,
                RecipeExcel.Get(row.Values!, "Description"), ActiveStatus(active)), cancellationToken);
            count++;
        }
        return count;
    }

    private static string ActiveStatus(string? value)
    {
        var key = RecipeCosting.NormalizeKey(value);
        return key is "N" or "NO" or "INACTIVE" or "FALSE" or "0" ? "INACTIVE" : "ACTIVE";
    }

    public async Task<IReadOnlyList<RecipeLocationRow>> ListLocationsAsync(Guid organizationId, string? kind, string? status, CancellationToken cancellationToken)
    {
        await SyncFromLocationMasterAsync(organizationId, cancellationToken);
        var rows = db.RecipeLocations.AsNoTracking().Where(item => item.OrganizationId == organizationId);
        if (Enum.TryParse<RecipeLocationKind>(kind, true, out var parsedKind))
            rows = rows.Where(item => item.Kind == parsedKind);
        if (Enum.TryParse<StatusKind>(status, true, out var parsedStatus))
            rows = rows.Where(item => item.Status == parsedStatus);
        return await rows.OrderBy(item => item.Kind).ThenBy(item => item.Name)
            .Select(item => new RecipeLocationRow(item.Id, item.Kind.ToString(), item.Code, item.Name, item.Description, item.Status.ToString()))
            .ToListAsync(cancellationToken);
    }

    public async Task<RecipeLocationRow> UpsertLocationAsync(Guid organizationId, Guid? id, RecipeLocationUpsertRequest request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.Code) || string.IsNullOrWhiteSpace(request.Name))
            throw new RecipeManagementException("LOCATION_REQUIRED", "Location code and name are required.");
        if (!Enum.TryParse<RecipeLocationKind>(request.Kind, true, out var kind))
            throw new RecipeManagementException("LOCATION_KIND_REQUIRED", "Kind must be OUTLET, STORE, or VENUE.");
        var row = id is null ? null : await db.RecipeLocations.SingleOrDefaultAsync(item => item.Id == id && item.OrganizationId == organizationId, cancellationToken);
        if (id is not null && row is null) throw new RecipeManagementException("LOCATION_NOT_FOUND", "The location was not found.", 404);
        var now = DateTime.UtcNow;
        row ??= new RecipeLocation { Id = Guid.NewGuid(), OrganizationId = organizationId, Kind = kind, Code = request.Code.Trim(), Name = request.Name.Trim(), CreatedAt = now };
        if (id is null) db.RecipeLocations.Add(row);
        row.Kind = kind;
        row.Code = request.Code.Trim();
        row.Name = request.Name.Trim();
        row.Description = request.Description?.Trim();
        row.Status = Enum.TryParse<StatusKind>(request.Status, true, out var parsed) ? parsed : StatusKind.ACTIVE;
        row.UpdatedAt = now;
        await db.SaveChangesAsync(cancellationToken);
        await MirrorInventoryLocationAsync(organizationId, row, cancellationToken);
        return new RecipeLocationRow(row.Id, row.Kind.ToString(), row.Code, row.Name, row.Description, row.Status.ToString());
    }

    private async Task SyncFromLocationMasterAsync(Guid organizationId, CancellationToken cancellationToken)
    {
        var inventory = await db.InventoryLocations.Where(item => item.OrganizationId == organizationId).ToListAsync(cancellationToken);
        var existing = await db.RecipeLocations.Where(item => item.OrganizationId == organizationId).ToListAsync(cancellationToken);
        var changed = false;
        foreach (var item in inventory)
        {
            if (!Enum.TryParse<RecipeLocationKind>(item.LocationType.ToString(), true, out var kind)) continue;
            var row = existing.FirstOrDefault(location => location.Kind == kind && string.Equals(location.Code, item.LocationCode, StringComparison.OrdinalIgnoreCase));
            if (row is null)
            {
                row = new RecipeLocation
                {
                    Id = Guid.NewGuid(), OrganizationId = organizationId, Kind = kind, Code = item.LocationCode,
                    Name = item.LocationName, Description = item.Description, Status = item.Status, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow,
                };
                db.RecipeLocations.Add(row);
                existing.Add(row);
                changed = true;
                continue;
            }
            if (row.Name == item.LocationName && row.Description == item.Description && row.Status == item.Status) continue;
            row.Name = item.LocationName;
            row.Description = item.Description;
            row.Status = item.Status;
            row.UpdatedAt = DateTime.UtcNow;
            changed = true;
        }
        if (changed) await db.SaveChangesAsync(cancellationToken);
    }

    private async Task MirrorInventoryLocationAsync(Guid organizationId, RecipeLocation row, CancellationToken cancellationToken)
    {
        if (!Enum.TryParse<InventoryLocationType>(row.Kind.ToString(), true, out var type)) return;
        var item = await db.InventoryLocations.SingleOrDefaultAsync(location => location.OrganizationId == organizationId && location.LocationCode == row.Code, cancellationToken);
        if (item is null)
        {
            item = new InventoryLocation
            {
                Id = Guid.NewGuid(),
                OrganizationId = organizationId,
                LocationCode = row.Code,
                LocationName = row.Name,
                LocationType = type,
                Description = row.Description,
                InventoryEnabled = true,
                ConsumptionEnabled = true,
                TransferEnabled = true,
                Status = row.Status,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
            };
            db.InventoryLocations.Add(item);
        }
        else
        {
            item.LocationName = row.Name;
            item.LocationType = type;
            item.Description = row.Description;
            item.Status = row.Status;
            item.UpdatedAt = DateTime.UtcNow;
        }
        await db.SaveChangesAsync(cancellationToken);
    }

    public byte[] LocationTemplate() => ExcelOpenXml.Write(RecipeExcel.LocationColumns, []);

    public async Task<byte[]> ExportLocationsAsync(Guid organizationId, CancellationToken cancellationToken) =>
        ExcelOpenXml.Write(RecipeExcel.LocationColumns,
            (await ListLocationsAsync(organizationId, null, null, cancellationToken)).Select(item => new Dictionary<string, string?>
            {
                ["Kind"] = item.Kind, ["Code"] = item.Code, ["Name"] = item.Name, ["Description"] = item.Description,
                ["Active"] = item.Status == "ACTIVE" ? "Y" : "N",
            }).ToList());

    public RecipeImportPreviewResponse PreviewLocations(Stream file, string fileName)
    {
        var sheets = ExcelOpenXml.ReadSheets(file);
        var parsed = ExcelOpenXml.ToDictionaries(sheets[0].Rows);
        var rows = parsed.Select((values, index) =>
        {
            var errors = new List<string>();
            var kind = RecipeExcel.Get(values, "Kind", "Type");
            if (string.IsNullOrWhiteSpace(kind) || !Enum.TryParse<RecipeLocationKind>(kind, true, out _))
                errors.Add("Kind must be OUTLET, STORE, or VENUE.");
            if (string.IsNullOrWhiteSpace(RecipeExcel.Get(values, "Code"))) errors.Add("Code is required.");
            if (string.IsNullOrWhiteSpace(RecipeExcel.Get(values, "Name"))) errors.Add("Name is required.");
            return new MaterialImportRowResponse(index + 2, errors.Count == 0, errors.Count == 0 ? "OK" : "INVALID", values, errors);
        }).ToList();
        return new RecipeImportPreviewResponse(fileName, rows.Count, rows.Count(item => item.IsValid), rows.Count(item => !item.IsValid), rows.Count(item => item.IsValid), rows);
    }

    public async Task<int> ImportLocationsAsync(Guid organizationId, Stream file, string fileName, CancellationToken cancellationToken)
    {
        var preview = PreviewLocations(file, fileName);
        var count = 0;
        foreach (var row in preview.Rows.Where(item => item.IsValid))
        {
            var kind = RecipeExcel.Get(row.Values!, "Kind", "Type")!;
            var code = RecipeExcel.Get(row.Values!, "Code")!;
            var parsedKind = Enum.Parse<RecipeLocationKind>(kind, true);
            var existing = await db.RecipeLocations.SingleOrDefaultAsync(item => item.OrganizationId == organizationId && item.Kind == parsedKind && item.Code == code, cancellationToken);
            var active = RecipeExcel.Get(row.Values!, "Active", "Status");
            await UpsertLocationAsync(organizationId, existing?.Id, new RecipeLocationUpsertRequest(
                kind, code, RecipeExcel.Get(row.Values!, "Name")!, RecipeExcel.Get(row.Values!, "Description"), ActiveStatus(active)), cancellationToken);
            count++;
        }
        return count;
    }

    public async Task<IReadOnlyList<RecipeListRow>> RecipesAtLocationAsync(Guid organizationId, Guid locationId, CancellationToken cancellationToken)
    {
        if (!await db.RecipeLocations.AnyAsync(item => item.Id == locationId && item.OrganizationId == organizationId, cancellationToken))
            throw new RecipeManagementException("LOCATION_NOT_FOUND", "The location was not found.", 404);
        var recipeIds = await db.RecipeLocationAssignments.AsNoTracking().Where(item => item.LocationId == locationId).Select(item => item.RecipeId).ToListAsync(cancellationToken);
        var rows = await db.Recipes.AsNoTracking().Include(item => item.Category).Include(item => item.RecipeFamily)
            .Include(item => item.Versions).ThenInclude(item => item.Ingredients).ThenInclude(item => item.Material)
            .Where(item => item.OrganizationId == organizationId && recipeIds.Contains(item.Id))
            .OrderBy(item => item.RecipeCode).ToListAsync(cancellationToken);
        var materialIds = rows.SelectMany(item => item.Versions.SelectMany(version => version.Ingredients).Select(line => line.MaterialId)).Where(id => id is not null).Select(id => id!.Value).Distinct().ToList();
        var conversions = await db.MaterialUomConversions.AsNoTracking().Where(item => materialIds.Contains(item.MaterialId) && item.IsActive).ToListAsync(cancellationToken);
        var pending = await db.MaterialChangeRequests.AsNoTracking()
            .Where(item => item.Status == MaterialGovernanceStatus.PENDING_APPROVAL && item.MaterialId != null && materialIds.Contains(item.MaterialId.Value))
            .ToListAsync(cancellationToken);
        var conversionMap = conversions.GroupBy(item => item.MaterialId).ToDictionary(item => item.Key, item => (IReadOnlyList<MaterialUomConversion>)item.ToList());
        var pendingMap = pending.Where(item => item.MaterialId is not null).GroupBy(item => item.MaterialId!.Value).ToDictionary(item => item.Key, item => item.First());
        return rows.Select(item => ToListRow(item, conversionMap, pendingMap)).ToList();
    }

    public async Task AssignRecipeToLocationAsync(Guid organizationId, Guid locationId, Guid recipeId, CancellationToken cancellationToken)
    {
        if (!await db.RecipeLocations.AnyAsync(item => item.Id == locationId && item.OrganizationId == organizationId, cancellationToken))
            throw new RecipeManagementException("LOCATION_NOT_FOUND", "The location was not found.", 404);
        if (!await db.Recipes.AnyAsync(item => item.Id == recipeId && item.OrganizationId == organizationId, cancellationToken))
            throw new RecipeManagementException("RECIPE_NOT_FOUND", "The recipe was not found.", 404);
        if (await db.RecipeLocationAssignments.AnyAsync(item => item.LocationId == locationId && item.RecipeId == recipeId, cancellationToken))
            return;
        db.RecipeLocationAssignments.Add(new RecipeLocationAssignment { Id = Guid.NewGuid(), RecipeId = recipeId, LocationId = locationId, CreatedAt = DateTime.UtcNow });
        await db.SaveChangesAsync(cancellationToken);
    }

    public async Task RemoveRecipeFromLocationAsync(Guid organizationId, Guid locationId, Guid recipeId, CancellationToken cancellationToken)
    {
        var row = await db.RecipeLocationAssignments.Include(item => item.Location)
            .SingleOrDefaultAsync(item => item.LocationId == locationId && item.RecipeId == recipeId && item.Location.OrganizationId == organizationId, cancellationToken)
            ?? throw new RecipeManagementException("ASSIGNMENT_NOT_FOUND", "That recipe is not assigned to this location.", 404);
        db.RecipeLocationAssignments.Remove(row);
        await db.SaveChangesAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<RecipeListRow>> ListRecipesAsync(Guid organizationId, string? query, CancellationToken cancellationToken, bool withPosItem = false)
    {
        var rows = db.Recipes.AsNoTracking()
            .Include(item => item.Category)
            .Include(item => item.RecipeFamily)
            .Include(item => item.Versions).ThenInclude(item => item.Ingredients).ThenInclude(item => item.Material)
            .Where(item => item.OrganizationId == organizationId);
        if (withPosItem)
            rows = rows.Where(item => item.PosItem != null && item.PosItem != "");
        if (!string.IsNullOrWhiteSpace(query))
        {
            var term = query.Trim().ToUpperInvariant();
            rows = rows.Where(item => item.RecipeCode.ToUpper().Contains(term) || item.Name.ToUpper().Contains(term)
                || (item.PosCode != null && item.PosCode.ToUpper().Contains(term))
                || (item.PosItem != null && item.PosItem.ToUpper().Contains(term)));
        }
        var list = await rows.OrderBy(item => item.RecipeCode).Take(300).ToListAsync(cancellationToken);
        var materialIds = list.SelectMany(item => item.Versions.SelectMany(version => version.Ingredients).Select(line => line.MaterialId)).Where(id => id is not null).Select(id => id!.Value).Distinct().ToList();
        var conversions = await db.MaterialUomConversions.AsNoTracking().Where(item => materialIds.Contains(item.MaterialId) && item.IsActive).ToListAsync(cancellationToken);
        var pending = await db.MaterialChangeRequests.AsNoTracking()
            .Where(item => item.Status == MaterialGovernanceStatus.PENDING_APPROVAL && item.MaterialId != null && materialIds.Contains(item.MaterialId.Value))
            .ToListAsync(cancellationToken);
        var conversionMap = conversions.GroupBy(item => item.MaterialId).ToDictionary(item => item.Key, item => (IReadOnlyList<MaterialUomConversion>)item.ToList());
        var pendingMap = pending.Where(item => item.MaterialId is not null).GroupBy(item => item.MaterialId!.Value).ToDictionary(item => item.Key, item => item.First());
        return list.Select(item => ToListRow(item, conversionMap, pendingMap)).ToList();
    }

    public async Task<RecipeVersionDetail> GetVersionAsync(Guid organizationId, Guid recipeId, Guid? versionId, CancellationToken cancellationToken)
    {
        var recipe = await LoadRecipeAsync(organizationId, recipeId, cancellationToken);
        var version = versionId is null
            ? recipe.Versions.OrderByDescending(item => item.VersionNumber).FirstOrDefault()
            : recipe.Versions.SingleOrDefault(item => item.Id == versionId);
        if (version is null) throw new RecipeManagementException("RECIPE_VERSION_NOT_FOUND", "The recipe version was not found.", 404);
        if (version.Ingredients.Any(item => string.IsNullOrWhiteSpace(item.RecipeIngredientId)))
        {
            AssignIngredientIds(recipe.RecipeCode, version.Ingredients);
            await db.SaveChangesAsync(cancellationToken);
        }
        return await ToDetailAsync(recipe, version, cancellationToken);
    }

    public async Task<IReadOnlyList<RecipeVersionDetail>> ListVersionsAsync(Guid organizationId, Guid recipeId, CancellationToken cancellationToken)
    {
        var recipe = await LoadRecipeAsync(organizationId, recipeId, cancellationToken);
        var details = new List<RecipeVersionDetail>();
        foreach (var item in recipe.Versions.OrderBy(row => row.VersionNumber))
            details.Add(await ToDetailAsync(recipe, item, cancellationToken));
        return details;
    }

    public async Task<RecipeVersionDetail> CreateOrReviseAsync(Session session, Guid organizationId, Guid? recipeId, RecipeUpsertRequest request, CancellationToken cancellationToken)
    {
        var title = FirstNonEmpty(request.Title, request.Name) ?? string.Empty;
        var servingUom = FirstNonEmpty(request.ServingUom) ?? "EA";
        var servingUnit = request.ServingUnit is > 0 ? request.ServingUnit.Value : request.ServingSize is > 0 ? request.ServingSize.Value : 1m;
        var now = DateTime.UtcNow;
        var userId = session.CustomerUserId();
        var categoryId = await ResolveCategoryIdAsync(organizationId, request.CategoryId, request.Category, cancellationToken)
            ?? throw new RecipeManagementException("CATEGORY_REQUIRED", "Select a Category from Category Master.");
        var familyId = await ResolveFamilyIdAsync(organizationId, request.FamilyId, request.Family, cancellationToken)
            ?? throw new RecipeManagementException("FAMILY_REQUIRED", "Select a Family from Family Master.");
        var family = await db.RecipeFamilies.SingleAsync(item => item.Id == familyId, cancellationToken);
        Recipe recipe;
        if (recipeId is null)
        {
            var recipeCode = await NextRecipeCodeAsync(organizationId, cancellationToken);
            recipe = new Recipe
            {
                Id = Guid.NewGuid(), OrganizationId = organizationId, RecipeCode = recipeCode, Name = title,
                Status = RecipeStatus.DRAFT, CurrentVersionNumber = 1, CreatedByUserId = userId, UpdatedByUserId = userId, CreatedAt = now, UpdatedAt = now,
            };
            db.Recipes.Add(recipe);
        }
        else
        {
            recipe = await db.Recipes.Include(item => item.Versions).ThenInclude(item => item.Ingredients)
                .SingleOrDefaultAsync(item => item.Id == recipeId && item.OrganizationId == organizationId, cancellationToken)
                ?? throw new RecipeManagementException("RECIPE_NOT_FOUND", "The recipe was not found.", 404);
            var current = recipe.Versions.SingleOrDefault(item => item.Id == recipe.ActiveVersionId) ?? recipe.Versions.OrderByDescending(item => item.VersionNumber).FirstOrDefault();
            if (current is not null && current.Status is RecipeStatus.APPROVED or RecipeStatus.ACTIVE &&
                await db.RecipeConsumptionTransactions.AnyAsync(item => item.RecipeVersionId == current.Id, cancellationToken))
            {
                recipe.CurrentVersionNumber += 1;
            }
            else if (current is not null && current.Status is RecipeStatus.DRAFT or RecipeStatus.REJECTED)
            {
                db.RecipeIngredients.RemoveRange(current.Ingredients);
                db.RecipeVersions.Remove(current);
            }
            else
            {
                recipe.CurrentVersionNumber += 1;
            }
            recipe.CurrentVersionNumber = Math.Max(recipe.CurrentVersionNumber, 1);
        }

        recipe.Name = title;
        recipe.Description = request.Description?.Trim();
        recipe.CategoryId = categoryId;
        recipe.FamilyId = familyId;
        recipe.Family = family.Name;
        recipe.Cuisine = request.Cuisine?.Trim() ?? family.Name;
        recipe.RecipeType = request.RecipeType?.Trim() ?? family.Name;
        recipe.ServingSize = servingUnit;
        recipe.ServingUom = servingUom;
        recipe.ItemMode = Enum.TryParse<RecipeItemMode>(request.ItemMode, true, out var mode) ? mode : RecipeItemMode.RECIPE;
        recipe.YieldQty = servingUnit;
        recipe.YieldUom = servingUom;
        recipe.Currency = recipe.Currency ?? "AED";
        ApplyPosFields(recipe, request.PosCode, request.PosItem, request.PosItemMenuPrice, request.LastSaleDate, overwriteEmptyOnly: false);
        recipe.Status = RecipeStatus.DRAFT;
        recipe.UpdatedByUserId = userId;
        recipe.UpdatedAt = now;

        var versionName = string.IsNullOrWhiteSpace(title) ? recipe.RecipeCode : title;
        var version = new RecipeVersion
        {
            Id = Guid.NewGuid(), OrganizationId = organizationId, RecipeId = recipe.Id, VersionNumber = recipe.CurrentVersionNumber,
            Name = versionName, Description = request.Description?.Trim(), CategoryId = categoryId, Cuisine = recipe.Cuisine,
            RecipeType = recipe.RecipeType, YieldQuantity = servingUnit, YieldUom = servingUom,
            PortionSize = request.PortionSize ?? servingUnit, PortionUom = request.PortionUom?.Trim() ?? servingUom,
            PreparationMinutes = request.PreparationMinutes, CookingMinutes = request.CookingMinutes, ImageUrl = request.ImageUrl?.Trim(),
            PreparationInstructions = request.PreparationInstructions, ChefNotes = request.ChefNotes, Status = RecipeStatus.DRAFT,
            CreatedByUserId = userId, CreatedAt = now, UpdatedAt = now,
        };
        var sequence = 10;
        var lines = (request.Ingredients ?? []).OrderBy(item => item.Sequence).ToList();
        if (recipe.ItemMode == RecipeItemMode.DIRECT && lines.Count(item => item.MaterialId is not null || !string.IsNullOrWhiteSpace(item.ErpMaterialId)) > 1)
            throw new RecipeManagementException("DIRECT_ONE_MATERIAL", "DIRECT items consume one Material Master item.");
        foreach (var line in lines)
        {
            if (line.MaterialId is null && string.IsNullOrWhiteSpace(line.ErpMaterialId)) continue;
            var material = await ResolveMaterialAsync(organizationId, line.MaterialId, line.ErpMaterialId, cancellationToken)
                ?? throw new RecipeManagementException("MATERIAL_NOT_FOUND", "The material was not found in Material Master.", 404);
            var conversions = await db.MaterialUomConversions.Where(item => item.MaterialId == material.Id && item.IsActive).ToListAsync(cancellationToken);
            version.Ingredients.Add(ToIngredient(version.Id, line, material, conversions, sequence));
            sequence += 10;
        }
        AssignIngredientIds(recipe.RecipeCode, version.Ingredients);
        ApplyIngredientShares(version.Ingredients);
        db.RecipeVersions.Add(version);
        await db.SaveChangesAsync(cancellationToken);
        await ReplaceLocationAssignmentsAsync(organizationId, recipe.Id, request.LocationIds, cancellationToken);
        return await GetVersionAsync(organizationId, recipe.Id, version.Id, cancellationToken);
    }

    public async Task<RecipeVersionDetail> SubmitDraftAsync(Guid organizationId, Guid recipeId, CancellationToken cancellationToken)
    {
        var recipe = await db.Recipes.Include(item => item.Versions)
            .SingleOrDefaultAsync(item => item.Id == recipeId && item.OrganizationId == organizationId, cancellationToken)
            ?? throw new RecipeManagementException("RECIPE_NOT_FOUND", "The recipe was not found.", 404);
        var version = recipe.Versions.OrderByDescending(item => item.VersionNumber).FirstOrDefault()
            ?? throw new RecipeManagementException("RECIPE_VERSION_NOT_FOUND", "Save a draft before submitting for approval.", 400);
        if (version.Status is RecipeStatus.PENDING_APPROVAL)
            throw new RecipeManagementException("APPROVAL_ALREADY_PENDING", "This recipe is already waiting for approval.");
        if (version.Status is RecipeStatus.APPROVED or RecipeStatus.ACTIVE)
            throw new RecipeManagementException("RECIPE_ALREADY_APPROVED", "This version is already approved.");
        await EnsureReadyAsync(organizationId, version.Id, cancellationToken);
        var isChange = recipe.Versions.Any(item => item.Status is RecipeStatus.APPROVED or RecipeStatus.ACTIVE || item.ApprovedAt is not null);
        await approvals.SubmitAsync(organizationId, recipe.Id, version.Id,
            isChange ? RecipeApprovalEvent.CHANGE_RECIPE : RecipeApprovalEvent.CREATE_RECIPE, cancellationToken);
        var loaded = await LoadRecipeAsync(organizationId, recipe.Id, cancellationToken);
        return await ToDetailAsync(loaded, loaded.Versions.Single(item => item.Id == version.Id), cancellationToken);
    }

    public async Task ApplyPosItemAsync(Guid organizationId, Guid recipeId, string? posCode, string? posItem, decimal? menuPrice, DateOnly? lastSaleDate, CancellationToken cancellationToken, Session? session = null, bool requireReapproval = false)
    {
        var recipe = await db.Recipes.Include(item => item.LocationAssignments).Include(item => item.Versions).ThenInclude(item => item.Ingredients)
            .SingleOrDefaultAsync(item => item.Id == recipeId && item.OrganizationId == organizationId, cancellationToken)
            ?? throw new RecipeManagementException("RECIPE_NOT_FOUND", "The recipe was not found.", 404);
        var live = recipe.Status is RecipeStatus.ACTIVE or RecipeStatus.APPROVED;
        ApplyPosFields(recipe, posCode, posItem, menuPrice, lastSaleDate, overwriteEmptyOnly: false);
        recipe.UpdatedAt = DateTime.UtcNow;
        await db.SaveChangesAsync(cancellationToken);
        if (!requireReapproval || session is null || !live) return;
        var current = recipe.Versions.OrderByDescending(item => item.VersionNumber).FirstOrDefault();
        var ingredients = (current?.Ingredients ?? []).OrderBy(item => item.Sequence).Select(item =>
            new RecipeIngredientInput(item.MaterialId, item.ErpMaterialId, item.UnmappedIngredientName, item.MaterialDescription,
                item.Quantity, item.Uom, item.WastagePercent, item.YieldPercent, item.UnitCost, item.Sequence, item.PreparationNotes)).ToList();
        var request = new RecipeUpsertRequest(recipe.RecipeCode, recipe.Name, recipe.Name, recipe.Description, recipe.CategoryId, null, recipe.FamilyId, recipe.Family,
            recipe.ItemMode.ToString(), recipe.Cuisine, recipe.RecipeType, recipe.ServingSize, recipe.ServingSize, recipe.ServingUom,
            recipe.YieldQty, recipe.YieldUom, current?.PortionSize, current?.PortionUom, recipe.PosCode, recipe.PosItem, recipe.PosItemMenuPrice, recipe.LastSaleDate,
            current?.PreparationMinutes, current?.CookingMinutes, current?.ImageUrl, current?.PreparationInstructions, current?.ChefNotes, ingredients,
            recipe.LocationAssignments.Select(item => item.LocationId).ToList());
        var detail = await CreateOrReviseAsync(session, organizationId, recipe.Id, request, cancellationToken);
        try
        {
            await SubmitDraftAsync(organizationId, detail.RecipeId, cancellationToken);
        }
        catch (RecipeManagementException)
        {
            // POS fields are saved on a new draft; approval waits until the recipe is ready.
        }
    }

    public byte[] Template() => ExcelOpenXml.WriteSheets(
    [
        new ExcelOpenXml.SheetWrite("MenuItems", RecipeExcel.MenuItemColumns, []),
        new ExcelOpenXml.SheetWrite("RecipeIngredients", RecipeExcel.RecipeIngredientColumns, []),
    ]);

    public async Task<byte[]> ExportAsync(Guid organizationId, CancellationToken cancellationToken)
    {
        var recipes = await db.Recipes.AsNoTracking()
            .Include(item => item.Category)
            .Include(item => item.RecipeFamily)
            .Include(item => item.Versions).ThenInclude(item => item.Ingredients).ThenInclude(item => item.Material)
            .Where(item => item.OrganizationId == organizationId)
            .OrderBy(item => item.RecipeCode)
            .ToListAsync(cancellationToken);
        var headers = recipes.Select(recipe => new Dictionary<string, string?>
        {
            ["ItemCode"] = recipe.RecipeCode,
            ["Title"] = recipe.Name,
            ["Family"] = recipe.RecipeFamily?.Name ?? recipe.Family,
            ["Category"] = recipe.Category?.Name,
            ["ItemMode"] = recipe.ItemMode.ToString(),
            ["ServingQty"] = (recipe.ServingSize ?? 1).ToString(CultureInfo.InvariantCulture),
            ["ServingUOM"] = recipe.ServingUom,
            ["MenuPrice"] = recipe.PosItemMenuPrice?.ToString(CultureInfo.InvariantCulture),
            ["Currency"] = recipe.Currency ?? "AED",
        }).ToList();
        var ingredients = new List<Dictionary<string, string?>>();
        foreach (var recipe in recipes)
        {
            var version = Latest(recipe);
            var sequence = 10;
            foreach (var line in (version?.Ingredients ?? []).OrderBy(item => item.Sequence))
            {
                ingredients.Add(new Dictionary<string, string?>
                {
                    ["ItemCode"] = recipe.RecipeCode,
                    ["Sequence"] = (line.Sequence > 0 ? line.Sequence : sequence).ToString(CultureInfo.InvariantCulture),
                    ["MaterialID"] = line.ErpMaterialId ?? line.Material?.MaterialCode,
                    ["RecipeQty"] = line.Quantity.ToString(CultureInfo.InvariantCulture),
                    ["RecipeUOM"] = line.Uom,
                });
                sequence += 10;
            }
        }
        return ExcelOpenXml.WriteSheets(
        [
            new ExcelOpenXml.SheetWrite("MenuItems", RecipeExcel.MenuItemColumns, headers),
            new ExcelOpenXml.SheetWrite("RecipeIngredients", RecipeExcel.RecipeIngredientColumns, ingredients),
        ]);
    }

    public RecipeImportPreviewResponse Preview(Stream file, string fileName)
    {
        var sheets = ExcelOpenXml.ReadSheets(file);
        var menu = ExcelOpenXml.FindSheet(sheets, "MenuItems", "Data") ?? sheets[0];
        var parsed = ExcelOpenXml.ToDictionaries(menu.Rows);
        var rows = parsed.Select((values, index) => PreviewRow(index + 2, values)).ToList();
        return new RecipeImportPreviewResponse(fileName, rows.Count, rows.Count(item => item.IsValid), rows.Count(item => !item.IsValid),
            rows.Where(item => item.IsValid).Select(item => RecipeExcel.RecipeKey(item.Values!)).Distinct(StringComparer.OrdinalIgnoreCase).Count(), rows);
    }

    public async Task<IReadOnlyList<Guid>> ImportAsync(Session session, Guid organizationId, Stream file, string fileName, CancellationToken cancellationToken)
    {
        if (file.CanSeek) file.Position = 0;
        var sheets = ExcelOpenXml.ReadSheets(file);
        var menu = ExcelOpenXml.FindSheet(sheets, "MenuItems", "Data") ?? sheets[0];
        var ingredientSheet = ExcelOpenXml.FindSheet(sheets, "RecipeIngredients");
        var headers = ExcelOpenXml.ToDictionaries(menu.Rows);
        var ingredientRows = ingredientSheet is null ? headers : ExcelOpenXml.ToDictionaries(ingredientSheet.Rows);
        var submitted = new List<Guid>();
        foreach (var header in headers)
        {
            var preview = PreviewRow(2, header);
            if (!preview.IsValid) continue;
            var recipeCode = RecipeExcel.Get(header, "ItemCode", "Item Code", "RecipeID", "Recipe Code", "RecipeCode");
            var existing = string.IsNullOrWhiteSpace(recipeCode) ? null : await db.Recipes.Include(item => item.Versions).ThenInclude(item => item.Ingredients)
                .SingleOrDefaultAsync(item => item.OrganizationId == organizationId && item.RecipeCode == recipeCode, cancellationToken);
            var key = RecipeExcel.RecipeKey(header);
            var ingredients = ingredientRows
                .Where(item => string.Equals(RecipeExcel.RecipeKey(item), key, StringComparison.OrdinalIgnoreCase)
                    || string.Equals(RecipeExcel.Get(item, "ItemCode", "RecipeID"), recipeCode, StringComparison.OrdinalIgnoreCase))
                .Select(ToIngredientInput)
                .Where(item => item.MaterialId is not null || !string.IsNullOrWhiteSpace(item.ErpMaterialId))
                .ToList();
            var resolved = new List<RecipeIngredientInput>();
            var sequence = 10;
            foreach (var line in ingredients)
            {
                var material = await ResolveMaterialAsync(organizationId, line.MaterialId, line.ErpMaterialId, cancellationToken);
                if (material is null) throw new RecipeManagementException("MATERIAL_NOT_FOUND", $"MaterialID {line.ErpMaterialId} was not found in Material Master.", 404);
                resolved.Add(line with { MaterialId = material.Id, Sequence = line.Sequence > 0 ? line.Sequence : sequence, Uom = FirstNonEmpty(line.Uom, material.BaseUom) ?? "EA" });
                sequence += 10;
            }
            var detail = await CreateOrReviseAsync(session, organizationId, existing?.Id, new RecipeUpsertRequest(
                recipeCode, RecipeExcel.Get(header, "Title", "Name"), null, null,
                null, RecipeExcel.Get(header, "Category"), null, RecipeExcel.Get(header, "Family"),
                RecipeExcel.Get(header, "Item Mode", "ItemMode"), null, null,
                RecipeExcel.Decimal(header, "ServingQty", "Serving Qty", "Serving Unit", "Serving Size", "ServingSize"),
                RecipeExcel.Decimal(header, "ServingQty", "Serving Qty", "Serving Size", "ServingSize"), RecipeExcel.Get(header, "ServingUOM", "Serving UOM"),
                null, null,
                null, null,
                RecipeExcel.Get(header, "POS Code", "POSCode"), RecipeExcel.Get(header, "POS Item", "POSItem", "Title"),
                RecipeExcel.Decimal(header, "MenuPrice", "Menu Price", "POS Item Menu price", "POS Item Menu Price"),
                null,
                null, null, null, null, null, resolved), cancellationToken);
            submitted.Add(detail.RecipeId);
        }
        return submitted.Distinct().ToList();
    }

    public async Task<RecipeVersion?> FindOperationalVersionAsync(Guid recipeId, DateTime at, CancellationToken cancellationToken)
    {
        var versions = await db.RecipeVersions.AsNoTracking().Where(item => item.RecipeId == recipeId &&
            (item.Status == RecipeStatus.ACTIVE || item.Status == RecipeStatus.APPROVED)).ToListAsync(cancellationToken);
        return versions
            .Where(item => (item.EffectiveFrom is null || item.EffectiveFrom <= at) && (item.EffectiveTo is null || item.EffectiveTo >= at))
            .OrderByDescending(item => item.EffectiveFrom ?? item.ApprovedAt ?? item.CreatedAt)
            .ThenByDescending(item => item.VersionNumber)
            .FirstOrDefault();
    }

    private async Task<Recipe> LoadRecipeAsync(Guid organizationId, Guid recipeId, CancellationToken cancellationToken) =>
        await db.Recipes.Include(item => item.Category).Include(item => item.RecipeFamily)
            .Include(item => item.LocationAssignments).ThenInclude(item => item.Location)
            .Include(item => item.Versions).ThenInclude(item => item.Ingredients).ThenInclude(item => item.Material)
            .SingleOrDefaultAsync(item => item.Id == recipeId && item.OrganizationId == organizationId, cancellationToken)
            ?? throw new RecipeManagementException("RECIPE_NOT_FOUND", "The recipe was not found.", 404);

    private async Task ReplaceLocationAssignmentsAsync(Guid organizationId, Guid recipeId, IReadOnlyList<Guid>? locationIds, CancellationToken cancellationToken)
    {
        var existing = await db.RecipeLocationAssignments.Where(item => item.RecipeId == recipeId).ToListAsync(cancellationToken);
        db.RecipeLocationAssignments.RemoveRange(existing);
        foreach (var locationId in (locationIds ?? []).Distinct())
        {
            if (!await db.RecipeLocations.AnyAsync(item => item.Id == locationId && item.OrganizationId == organizationId, cancellationToken))
                throw new RecipeManagementException("LOCATION_NOT_FOUND", "An assigned outlet, store, or venue was not found.");
            db.RecipeLocationAssignments.Add(new RecipeLocationAssignment { Id = Guid.NewGuid(), RecipeId = recipeId, LocationId = locationId, CreatedAt = DateTime.UtcNow });
        }
        await db.SaveChangesAsync(cancellationToken);
    }

    private async Task<Guid?> ResolveFamilyIdAsync(Guid organizationId, Guid? familyId, string? familyName, CancellationToken cancellationToken)
    {
        if (familyId is { } id)
            return await db.RecipeFamilies.AnyAsync(item => item.Id == id && item.OrganizationId == organizationId, cancellationToken)
                ? id : throw new RecipeManagementException("FAMILY_NOT_FOUND", "The recipe family was not found.", 404);
        if (string.IsNullOrWhiteSpace(familyName)) return null;
        var match = await db.RecipeFamilies.SingleOrDefaultAsync(item => item.OrganizationId == organizationId &&
            (item.Name == familyName.Trim() || item.Code == familyName.Trim()), cancellationToken);
        return match?.Id;
    }

    private async Task<Guid?> ResolveCategoryIdAsync(Guid organizationId, Guid? categoryId, string? categoryName, CancellationToken cancellationToken)
    {
        if (categoryId is { } id)
            return await db.RecipeCategories.AnyAsync(item => item.Id == id && item.OrganizationId == organizationId, cancellationToken)
                ? id : throw new RecipeManagementException("CATEGORY_NOT_FOUND", "The recipe category was not found.", 404);
        if (string.IsNullOrWhiteSpace(categoryName)) return null;
        var match = await db.RecipeCategories.SingleOrDefaultAsync(item => item.OrganizationId == organizationId &&
            (item.Name == categoryName.Trim() || item.Code == categoryName.Trim()), cancellationToken);
        return match?.Id;
    }

    private async Task<Material?> ResolveMaterialAsync(Guid organizationId, Guid? materialId, string? erpMaterialId, CancellationToken cancellationToken)
    {
        if (materialId is { } id)
            return await db.Materials.SingleOrDefaultAsync(item => item.Id == id && item.OrganizationId == organizationId, cancellationToken);
        if (string.IsNullOrWhiteSpace(erpMaterialId)) return null;
        return await db.Materials.SingleOrDefaultAsync(item => item.OrganizationId == organizationId && item.MaterialCode == erpMaterialId.Trim(), cancellationToken);
    }

    private async Task<string> NextRecipeCodeAsync(Guid organizationId, CancellationToken cancellationToken)
    {
        var codes = await db.Recipes.AsNoTracking()
            .Where(item => item.OrganizationId == organizationId)
            .Select(item => item.RecipeCode)
            .ToListAsync(cancellationToken);
        return RecipeNumbering.RecipeId(RecipeNumbering.MaxRecipeSequence(codes) + 1);
    }

    private static void AssignIngredientIds(string recipeCode, IEnumerable<RecipeIngredient> ingredients)
    {
        var line = 1;
        foreach (var item in ingredients.OrderBy(row => row.Sequence))
            item.RecipeIngredientId = RecipeNumbering.IngredientId(recipeCode, line++);
    }

    private static void ApplyPosFields(Recipe recipe, string? posCode, string? posItem, decimal? menuPrice, DateOnly? lastSaleDate, bool overwriteEmptyOnly)
    {
        if (!string.IsNullOrWhiteSpace(posCode) && (!overwriteEmptyOnly || string.IsNullOrWhiteSpace(recipe.PosCode)))
            recipe.PosCode = posCode.Trim();
        if (!string.IsNullOrWhiteSpace(posItem) && (!overwriteEmptyOnly || string.IsNullOrWhiteSpace(recipe.PosItem)))
            recipe.PosItem = posItem.Trim();
        if (menuPrice is not null && (!overwriteEmptyOnly || recipe.PosItemMenuPrice is null))
            recipe.PosItemMenuPrice = menuPrice;
        if (lastSaleDate is not null && (recipe.LastSaleDate is null || lastSaleDate > recipe.LastSaleDate))
            recipe.LastSaleDate = lastSaleDate;
    }

    public async Task EnsureReadyAsync(Guid organizationId, Guid versionId, CancellationToken cancellationToken)
    {
        var version = await db.RecipeVersions.Include(item => item.Ingredients).ThenInclude(item => item.Material)
            .SingleOrDefaultAsync(item => item.Id == versionId && item.OrganizationId == organizationId, cancellationToken)
            ?? throw new RecipeManagementException("RECIPE_VERSION_NOT_FOUND", "The recipe version was not found.", 404);
        var readiness = await EvaluateVersionAsync(version, cancellationToken);
        if (version.Ingredients.Count == 0)
            throw new RecipeManagementException("RECIPE_NOT_READY", "Add at least one ingredient or consumption material before submitting.");
        if (!readiness.ReadyForApproval)
            throw new RecipeManagementException("RECIPE_NOT_READY",
                string.Join("; ", readiness.Issues.Select(item => item.Message)));
    }

    private async Task<RecipeReadinessInfo> EvaluateVersionAsync(RecipeVersion version, CancellationToken cancellationToken)
    {
        var materialIds = version.Ingredients.Where(item => item.MaterialId is not null).Select(item => item.MaterialId!.Value).Distinct().ToList();
        var materials = version.Ingredients.Where(item => item.Material is not null).Select(item => item.Material!).GroupBy(item => item.Id).ToDictionary(item => item.Key, item => item.First());
        var conversions = await db.MaterialUomConversions.AsNoTracking().Where(item => materialIds.Contains(item.MaterialId) && item.IsActive).ToListAsync(cancellationToken);
        var pending = await db.MaterialChangeRequests.AsNoTracking()
            .Where(item => item.Status == MaterialGovernanceStatus.PENDING_APPROVAL && item.MaterialId != null && materialIds.Contains(item.MaterialId.Value))
            .ToListAsync(cancellationToken);
        return RecipeReadinessEvaluator.Evaluate(version.Ingredients.ToList(), materials,
            conversions.GroupBy(item => item.MaterialId).ToDictionary(item => item.Key, item => (IReadOnlyList<MaterialUomConversion>)item.ToList()),
            pending.Where(item => item.MaterialId is not null).GroupBy(item => item.MaterialId!.Value).ToDictionary(item => item.Key, item => item.First()));
    }

    private static RecipeIngredient ToIngredient(Guid versionId, RecipeIngredientInput line, Material material, IReadOnlyList<MaterialUomConversion> conversions, int sequence)
    {
        var quantity = line.Quantity <= 0 ? 1 : line.Quantity;
        var uom = FirstNonEmpty(line.Uom, material.BaseUom) ?? "EA";
        var effective = RecipeUom.EffectiveConversions(material, conversions);
        var consumption = RecipeUom.Convert(quantity, uom, material.BaseUom, effective);
        var unitCost = MaterialCosting.EffectiveUnitCost(material);
        var cost = consumption is null ? null : RecipeCosting.LineCost(consumption.Value, unitCost);
        return new RecipeIngredient
        {
            Id = Guid.NewGuid(), RecipeVersionId = versionId, MaterialId = material.Id,
            UnmappedIngredientName = null,
            MaterialDescription = material.Description,
            Quantity = quantity, Uom = uom, WastagePercent = line.WastagePercent,
            YieldPercent = line.YieldPercent <= 0 ? 100 : line.YieldPercent,
            UnitCost = unitCost, ErpMaterialId = material.MaterialCode,
            MaterialGroup = material.MaterialGroup, IngredientCost = cost, Sequence = line.Sequence > 0 ? line.Sequence : sequence,
            PreparationNotes = line.PreparationNotes, ConsumptionQuantity = consumption, ConsumptionUom = material.BaseUom,
        };
    }

    private static void ApplyIngredientShares(IEnumerable<RecipeIngredient> ingredients)
    {
        var list = ingredients.ToList();
        var snapshot = list.Select(item => (item.Quantity, item.Uom, item.IngredientCost, item.Sequence)).ToList();
        foreach (var item in list)
            item.PercentageOfTotalCost = RecipeCosting.QuantityOrCostShare(item.Quantity, item.Uom, item.IngredientCost, item.Sequence, snapshot);
    }

    private static RecipeListRow ToListRow(Recipe recipe, IReadOnlyDictionary<Guid, IReadOnlyList<MaterialUomConversion>> conversions, IReadOnlyDictionary<Guid, MaterialChangeRequest> pending)
    {
        var version = Latest(recipe);
        var ingredients = version?.Ingredients.ToList() ?? [];
        var materials = ingredients.Where(item => item.Material is not null).Select(item => item.Material!).GroupBy(item => item.Id).ToDictionary(item => item.Key, item => item.First());
        var readiness = RecipeReadinessEvaluator.Evaluate(ingredients, materials, conversions, pending);
        var costing = CostingOf(recipe, version, ingredients, materials, conversions, pending, readiness);
        return new RecipeListRow(recipe.Id, recipe.RecipeCode, recipe.Name, recipe.PosCode, recipe.PosItem, recipe.ItemMode.ToString(),
            recipe.ServingSize ?? 1, recipe.ServingUom ?? "EA", recipe.ServingSize ?? 1, recipe.ServingUom ?? "EA",
            recipe.RecipeFamily?.Name ?? recipe.Family, recipe.Category?.Name, recipe.PosItemMenuPrice,
            costing.RecipeCost, costing.CostPerServing, costing.CostPercent, costing.MarginAmount, costing.MarginPercent,
            recipe.Status.ToString(), recipe.UpdatedAt, recipe.Currency ?? "AED", recipe.CurrentVersionNumber, recipe.ActiveVersionId,
            readiness.Status, readiness.IssueCount);
    }

    private async Task<RecipeVersionDetail> ToDetailAsync(Recipe recipe, RecipeVersion version, CancellationToken cancellationToken)
    {
        var ingredients = version.Ingredients.ToList();
        var materialIds = ingredients.Where(item => item.MaterialId is not null).Select(item => item.MaterialId!.Value).Distinct().ToList();
        var conversions = await db.MaterialUomConversions.AsNoTracking().Where(item => materialIds.Contains(item.MaterialId) && item.IsActive).ToListAsync(cancellationToken);
        var pending = await db.MaterialChangeRequests.AsNoTracking()
            .Where(item => item.Status == MaterialGovernanceStatus.PENDING_APPROVAL && item.MaterialId != null && materialIds.Contains(item.MaterialId.Value))
            .ToListAsync(cancellationToken);
        var conversionMap = conversions.GroupBy(item => item.MaterialId).ToDictionary(item => item.Key, item => (IReadOnlyList<MaterialUomConversion>)item.ToList());
        var pendingMap = pending.Where(item => item.MaterialId is not null).GroupBy(item => item.MaterialId!.Value).ToDictionary(item => item.Key, item => item.First());
        var materials = ingredients.Where(item => item.Material is not null).Select(item => item.Material!).GroupBy(item => item.Id).ToDictionary(item => item.Key, item => item.First());
        var readiness = RecipeReadinessEvaluator.Evaluate(ingredients, materials, conversionMap, pendingMap);
        var rows = IngredientRows(ingredients, materials, conversionMap, pendingMap, readiness.ReadyForApproval);
        var costing = CostingOf(recipe, version, ingredients, materials, conversionMap, pendingMap, readiness);
        var familyName = recipe.RecipeFamily?.Name ?? recipe.Family;
        return new RecipeVersionDetail(version.Id, recipe.Id, version.VersionNumber, recipe.RecipeCode, recipe.Name, version.Description, version.CategoryId ?? recipe.CategoryId,
            recipe.Category?.Name, recipe.FamilyId, familyName, recipe.ItemMode.ToString(), version.Cuisine, version.RecipeType,
            recipe.ServingSize ?? 1, recipe.ServingUom ?? "EA", recipe.ServingSize ?? 1, recipe.ServingUom ?? "EA",
            version.PortionSize, version.PortionUom, recipe.PosCode, recipe.PosItem, recipe.PosItemMenuPrice, recipe.Currency ?? "AED",
            version.PreparationMinutes, version.CookingMinutes, version.ImageUrl, version.PreparationInstructions, version.ChefNotes,
            version.Status.ToString(), version.EffectiveFrom, version.EffectiveTo, version.ApprovedByUserId, version.ApprovedAt,
            rows, costing, readiness, recipe.LastSaleDate,
            recipe.LocationAssignments.OrderBy(item => item.Location.Kind).ThenBy(item => item.Location.Name)
                .Select(item => new RecipeLocationAssignmentRow(item.LocationId, item.Location.Kind.ToString(), item.Location.Code, item.Location.Name)).ToList());
    }

    private static IReadOnlyList<RecipeIngredientRow> IngredientRows(
        IReadOnlyList<RecipeIngredient> ordered,
        IReadOnlyDictionary<Guid, Material> materials,
        IReadOnlyDictionary<Guid, IReadOnlyList<MaterialUomConversion>> conversions,
        IReadOnlyDictionary<Guid, MaterialChangeRequest> pending,
        bool costingComplete)
    {
        var costs = new List<decimal?>();
        var rows = new List<RecipeIngredientRow>();
        foreach (var item in ordered.OrderBy(row => row.Sequence))
        {
            materials.TryGetValue(item.MaterialId ?? Guid.Empty, out var material);
            pending.TryGetValue(material?.Id ?? Guid.Empty, out var change);
            conversions.TryGetValue(material?.Id ?? Guid.Empty, out var listed);
            var materialConversions = material is null ? [] : RecipeUom.EffectiveConversions(material, listed);
            var approvedPrice = material is null ? null : MaterialCosting.EffectiveUnitCost(material);
            var consumption = material is null ? item.ConsumptionQuantity : RecipeUom.Convert(item.Quantity, item.Uom, material.BaseUom, materialConversions);
            var conversionMissing = material is not null && consumption is null;
            var cost = conversionMissing ? null : RecipeCosting.LineCost(consumption ?? item.Quantity, approvedPrice);
            costs.Add(cost);
            var status = material is null ? "PRICE_MISSING" : MaterialCosting.PriceStatus(material, change);
            var pack = material is null ? null : RecipeUom.PackSummary(material.BaseUom, materialConversions);
            var conversionRows = material is null
                ? new List<MaterialUomConversionRow>()
                : materialConversions.Select(row =>
                    new MaterialUomConversionRow(row.Id, material.Id, material.MaterialCode, row.FromUom, row.ToUom, row.Numerator, row.Denominator, row.PackSize, row.PackUom, row.Source, row.IsActive)).ToList();
            rows.Add(new RecipeIngredientRow(item.Id, item.RecipeIngredientId, item.MaterialId, item.ErpMaterialId ?? material?.MaterialCode, material?.MaterialCode,
                material?.Description ?? item.MaterialDescription, item.UnmappedIngredientName, item.MaterialDescription, item.MaterialGroup ?? material?.MaterialGroup,
                item.Quantity, item.Uom, item.WastagePercent, item.YieldPercent, item.Quantity, approvedPrice, approvedPrice is null,
                cost, null, item.Sequence, item.PreparationNotes, material?.BaseUom, pack, consumption, material?.BaseUom, conversionMissing,
                status, MaterialCosting.PriceStatusLabel(status), MaterialCosting.ProposedUnitPrice(change),
                status is "PRICE_MISSING" or "PRICE_REJECTED" or "PRICE_INVALID" or "PRICE_EXPIRED", conversionRows));
        }
        var total = costingComplete ? rows.Sum(item => item.IngredientCost ?? 0) : 0m;
        for (var index = 0; index < rows.Count; index++)
        {
            var row = rows[index];
            var share = costingComplete ? RecipeCosting.PercentageOfTotal(row.IngredientCost ?? 0, total) : null;
            rows[index] = row with { PercentageOfTotalCost = share };
        }
        return rows;
    }

    private static RecipeCostingSummary CostingOf(
        Recipe recipe, RecipeVersion? version, IReadOnlyList<RecipeIngredient> ingredients,
        IReadOnlyDictionary<Guid, Material> materials,
        IReadOnlyDictionary<Guid, IReadOnlyList<MaterialUomConversion>> conversions,
        IReadOnlyDictionary<Guid, MaterialChangeRequest> pending,
        RecipeReadinessInfo readiness)
    {
        var complete = readiness.ReadyForApproval;
        var missing = readiness.Issues.Count(item => item.Code == "PRICE_MISSING");
        var pendingCount = readiness.Issues.Count(item => item.Code == "PRICE_PENDING_APPROVAL");
        decimal total = 0;
        if (complete)
        {
            foreach (var item in ingredients)
            {
                if (item.MaterialId is null || !materials.TryGetValue(item.MaterialId.Value, out var material)) continue;
                conversions.TryGetValue(material.Id, out var listed);
                var consumption = RecipeUom.Convert(item.Quantity, item.Uom, material.BaseUom, RecipeUom.EffectiveConversions(material, listed));
                var cost = RecipeCosting.LineCost(consumption ?? 0, MaterialCosting.EffectiveUnitCost(material));
                total += cost ?? 0;
            }
            total = decimal.Round(total, 4, MidpointRounding.AwayFromZero);
        }
        var servingQty = recipe.ServingSize ?? 1;
        var profit = complete ? RecipeUom.Profitability(total, servingQty, recipe.PosItemMenuPrice) : null;
        var message = complete ? null : BuildCostingMessage(readiness);
        return new RecipeCostingSummary(profit?.RecipeCost, profit?.CostPerServing, profit?.CostPercent, profit?.MarginAmount, profit?.MarginPercent,
            servingQty, recipe.ServingUom ?? "EA", servingQty, recipe.ServingUom ?? "EA", recipe.Currency ?? "AED",
            profit?.RecipeCost ?? 0, profit?.RecipeCost ?? 0, complete ? 100m : null, profit?.CostPercent,
            complete, missing, pendingCount, message);
    }

    private static string BuildCostingMessage(RecipeReadinessInfo readiness)
    {
        var missing = readiness.Issues.Count(item => item.Code == "PRICE_MISSING");
        var pending = readiness.Issues.Count(item => item.Code == "PRICE_PENDING_APPROVAL");
        var parts = new List<string>();
        if (pending > 0) parts.Add($"{pending} MATERIAL PRICE{(pending == 1 ? "" : "S")} PENDING");
        if (missing > 0) parts.Add($"{missing} MATERIAL PRICE{(missing == 1 ? "" : "S")} MISSING");
        if (parts.Count == 0) parts.Add("COST PENDING MATERIAL PRICE APPROVAL");
        return string.Join(" · ", parts);
    }

    private static RecipeVersion? Latest(Recipe recipe) =>
        recipe.Versions.FirstOrDefault(item => item.Id == recipe.ActiveVersionId)
        ?? recipe.Versions.OrderByDescending(item => item.VersionNumber).FirstOrDefault();

    private static Dictionary<string, string?> ToExcelRow(Recipe recipe, RecipeVersionDetail? detail, RecipeIngredientRow? line) => new()
    {
        ["POS Code"] = recipe.PosCode,
        ["Title"] = recipe.Name,
        ["POS Item"] = recipe.PosItem,
        ["ServingUOM"] = recipe.ServingUom ?? detail?.ServingUom,
        ["Serving Size"] = (recipe.ServingSize ?? detail?.ServingUnit)?.ToString(CultureInfo.InvariantCulture),
        ["RecipeID"] = recipe.RecipeCode,
        ["RecipeIngredientID"] = line?.RecipeIngredientId,
        ["Family"] = recipe.Family,
        ["Category"] = recipe.Category?.Name ?? detail?.Category,
        ["Last Sale Date"] = recipe.LastSaleDate?.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
        ["POS Item Menu price"] = recipe.PosItemMenuPrice?.ToString(CultureInfo.InvariantCulture),
        ["Ingredient Cost"] = line?.IngredientCost?.ToString(CultureInfo.InvariantCulture) ?? detail?.Costing.IngredientCost.ToString(CultureInfo.InvariantCulture),
        ["Total POS Item Cost"] = detail?.Costing.TotalPosItemCost.ToString(CultureInfo.InvariantCulture),
        ["PercentageofTotalCost"] = line?.PercentageOfTotalCost?.ToString(CultureInfo.InvariantCulture) ?? detail?.Costing.PercentageOfTotalCost?.ToString(CultureInfo.InvariantCulture),
        ["POS Item Cost Percentage"] = detail?.Costing.PosItemCostPercentage?.ToString(CultureInfo.InvariantCulture),
        ["ERPMaterialID"] = line?.ErpMaterialId ?? line?.MaterialCode,
        ["Status"] = recipe.Status.ToString(),
        ["Material Group"] = line?.MaterialGroup,
    };

    private static MaterialImportRowResponse PreviewRow(int rowNumber, Dictionary<string, string?> values)
    {
        var errors = new List<string>();
        if (string.IsNullOrWhiteSpace(RecipeExcel.Get(values, "Family")) && string.IsNullOrWhiteSpace(RecipeExcel.Get(values, "Family Code")))
            errors.Add("Family is required.");
        if (string.IsNullOrWhiteSpace(RecipeExcel.Get(values, "Category"))) errors.Add("Category is required.");
        return new MaterialImportRowResponse(rowNumber, errors.Count == 0, errors.Count == 0 ? "OK" : "INVALID", values, errors);
    }

    private static RecipeIngredientInput ToIngredientInput(IReadOnlyDictionary<string, string?> values) =>
        new(null, RecipeExcel.Get(values, "MaterialID", "Material ID", "ERPMaterialID", "ERP Material ID"), null,
            RecipeExcel.Get(values, "Material Description", "Description"),
            RecipeExcel.Decimal(values, "RecipeQty", "Recipe Qty", "Quantity", "Qty") ?? 1,
            RecipeExcel.Get(values, "RecipeUOM", "Recipe UOM", "UOM") ?? "EA",
            0, 100, null,
            (int)(RecipeExcel.Decimal(values, "Sequence") ?? 10), null);

    private static string? FirstNonEmpty(params string?[] values) =>
        values.FirstOrDefault(item => !string.IsNullOrWhiteSpace(item))?.Trim();
}
