using Microsoft.AspNetCore.Mvc;
using SilaMe.Api.Auth;
using SilaMe.Api.DTOs;
using SilaMe.Api.Models;
using SilaMe.Api.Services;

namespace SilaMe.Api.Controllers;

[ApiController]
[Route("api/v1/recipe-management")]
public sealed class RecipeManagementController(
    IAccessService access,
    MaterialMasterService materials,
    RecipeCatalogService recipes,
    RecipeReferenceDataService references,
    RecipeApprovalService approvals,
    MaterialApprovalService materialApprovals,
    PosSourceService pos,
    RecipePosIntakeService intake) : ControllerBase
{
    [HttpGet("dashboard")]
    public Task<IActionResult> Dashboard([FromQuery] Guid organizationId, CancellationToken cancellationToken) =>
        Read(organizationId, "VIEW_RECIPE", () => recipes.DashboardAsync(organizationId, cancellationToken), cancellationToken);

    [HttpGet("materials")]
    public Task<IActionResult> Materials(
        [FromQuery] Guid organizationId, [FromQuery] string? query, [FromQuery] string? status, [FromQuery] string? priceStatus,
        [FromQuery] string? category, [FromQuery] string? materialGroup, [FromQuery] string? materialType, [FromQuery] string? supplier,
        CancellationToken cancellationToken) =>
        Read(organizationId, "VIEW_MATERIAL", () => materials.ListAsync(organizationId, query, status, priceStatus, category, materialGroup, materialType, supplier, cancellationToken), cancellationToken);

    [HttpGet("materials/search-facets")]
    public Task<IActionResult> MaterialSearchFacets([FromQuery] Guid organizationId, CancellationToken cancellationToken) =>
        Read(organizationId, "VIEW_MATERIAL", () => materials.FacetsAsync(organizationId, cancellationToken), cancellationToken);

    [HttpGet("materials/{id:guid}")]
    public Task<IActionResult> Material(Guid id, [FromQuery] Guid organizationId, CancellationToken cancellationToken) =>
        Read(organizationId, "VIEW_MATERIAL", () => materials.GetAsync(organizationId, id, cancellationToken), cancellationToken);

    [HttpPost("materials")]
    public Task<IActionResult> CreateMaterial([FromQuery] Guid organizationId, [FromBody] MaterialUpsertRequest request, CancellationToken cancellationToken) =>
        Write(organizationId, "CREATE_MATERIAL", () => materials.CreateAsync(CurrentSession()!, organizationId, request, cancellationToken), cancellationToken);

    [HttpPut("materials/{id:guid}")]
    public Task<IActionResult> ChangeMaterial(Guid id, [FromQuery] Guid organizationId, [FromBody] MaterialUpsertRequest request, CancellationToken cancellationToken) =>
        Write(organizationId, "EDIT_MATERIAL", () => materials.ChangeAsync(CurrentSession()!, organizationId, id, request, cancellationToken), cancellationToken);

    [HttpPost("materials/{id:guid}/submit")]
    public Task<IActionResult> SubmitMaterial(Guid id, [FromQuery] Guid organizationId, CancellationToken cancellationToken) =>
        Write(organizationId, "EDIT_MATERIAL", async () => { await materials.SubmitAsync(CurrentSession()!, organizationId, id, cancellationToken); return "OK"; }, cancellationToken);

    [HttpDelete("materials/{id:guid}")]
    public Task<IActionResult> DeleteMaterial(Guid id, [FromQuery] Guid organizationId, [FromQuery] bool hardDelete, CancellationToken cancellationToken) =>
        Write(organizationId, "EDIT_MATERIAL", async () => { await materials.DeactivateOrDeleteAsync(organizationId, id, hardDelete, cancellationToken); return "OK"; }, cancellationToken);

    [HttpGet("materials/template")]
    public async Task<IActionResult> MaterialTemplate([FromQuery] Guid organizationId, CancellationToken cancellationToken)
    {
        if (await Guard(organizationId, "VIEW_MATERIAL", cancellationToken) is { } denied) return denied;
        return File(materials.Template(), "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", "material-master-template.xlsx");
    }

    [HttpGet("materials/export")]
    public async Task<IActionResult> MaterialExport([FromQuery] Guid organizationId, CancellationToken cancellationToken)
    {
        if (await Guard(organizationId, "VIEW_MATERIAL", cancellationToken) is { } denied) return denied;
        return File(await materials.ExportAsync(organizationId, cancellationToken), "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", "material-master.xlsx");
    }

    [HttpPost("materials/import/preview")]
    [RequestSizeLimit(10 * 1024 * 1024)]
    public async Task<IActionResult> MaterialPreview([FromQuery] Guid organizationId, IFormFile? file, CancellationToken cancellationToken)
    {
        if (await Guard(organizationId, "VIEW_MATERIAL", cancellationToken) is { } denied) return denied;
        if (file is null || file.Length == 0) return BadRequest(new ApiError("IMPORT_FILE_REQUIRED", "Choose an .xlsx file."));
        await using var stream = file.OpenReadStream();
        return await Execute(() => materials.PreviewAsync(organizationId, stream, file.FileName, cancellationToken));
    }

    [HttpPost("materials/import")]
    [RequestSizeLimit(10 * 1024 * 1024)]
    public async Task<IActionResult> MaterialImport([FromQuery] Guid organizationId, IFormFile? file, CancellationToken cancellationToken)
    {
        if (await Guard(organizationId, "EDIT_MATERIAL", cancellationToken) is { } denied) return denied;
        if (file is null || file.Length == 0) return BadRequest(new ApiError("IMPORT_FILE_REQUIRED", "Choose an .xlsx file."));
        await using var stream = file.OpenReadStream();
        return await Execute(() => materials.ImportAsync(CurrentSession()!, organizationId, stream, file.FileName, cancellationToken));
    }

    [HttpGet("materials/erp-route")]
    public Task<IActionResult> MaterialErpRoute([FromQuery] Guid organizationId, [FromQuery] string companyCode, CancellationToken cancellationToken) =>
        Read(organizationId, "VIEW_MATERIAL", () => materials.ResolveErpAsync(organizationId, companyCode, cancellationToken), cancellationToken);

    [HttpPost("materials/erp-pull")]
    public Task<IActionResult> MaterialErpPull([FromQuery] Guid organizationId, [FromBody] MaterialErpPullRequest request, CancellationToken cancellationToken) =>
        Write(organizationId, "EDIT_MATERIAL", () => materials.PullFromErpAsync(CurrentSession()!, organizationId, request.CompanyCode, cancellationToken), cancellationToken);

    [HttpPost("materials/{id:guid}/price-change")]
    public Task<IActionResult> ProposeMaterialPrice(Guid id, [FromQuery] Guid organizationId, [FromBody] MaterialPriceUpdateRequest request, CancellationToken cancellationToken) =>
        Write(organizationId, "EDIT_MATERIAL", () => materials.ProposePriceAsync(CurrentSession()!, organizationId, id, request, cancellationToken), cancellationToken);

    [HttpGet("materials/approvals")]
    public Task<IActionResult> MaterialApprovals([FromQuery] Guid organizationId, CancellationToken cancellationToken) =>
        Read(organizationId, "APPROVE_MATERIAL", () => materialApprovals.ListAsync(organizationId, cancellationToken), cancellationToken);

    [HttpPost("materials/approvals/{id:guid}/approve")]
    public Task<IActionResult> ApproveMaterial(Guid id, [FromQuery] Guid organizationId, [FromBody] RecipeApprovalDecisionRequest request, CancellationToken cancellationToken) =>
        Write(organizationId, "APPROVE_MATERIAL", async () => { await materialApprovals.DecideAsync(CurrentSession()!, organizationId, id, true, request, cancellationToken); return "OK"; }, cancellationToken);

    [HttpPost("materials/approvals/{id:guid}/reject")]
    public Task<IActionResult> RejectMaterial(Guid id, [FromQuery] Guid organizationId, [FromBody] RecipeApprovalDecisionRequest request, CancellationToken cancellationToken) =>
        Write(organizationId, "APPROVE_MATERIAL", async () => { await materialApprovals.DecideAsync(CurrentSession()!, organizationId, id, false, request, cancellationToken); return "OK"; }, cancellationToken);

    [HttpGet("uoms")]
    public Task<IActionResult> Uoms([FromQuery] Guid organizationId, [FromQuery] string? status, CancellationToken cancellationToken) =>
        Read(organizationId, "VIEW_RECIPE", () => references.ListUomsAsync(organizationId, status, cancellationToken), cancellationToken);

    [HttpPut("uoms/{id:guid?}")]
    public Task<IActionResult> UpsertUom(Guid? id, [FromQuery] Guid organizationId, [FromBody] UomMasterUpsertRequest request, CancellationToken cancellationToken) =>
        Write(organizationId, "EDIT_RECIPE", () => references.UpsertUomAsync(organizationId, id, request, cancellationToken), cancellationToken);

    [HttpGet("uom-conversions")]
    public Task<IActionResult> Conversions([FromQuery] Guid organizationId, [FromQuery] string? query, [FromQuery] string? cursor, [FromQuery] int pageSize, CancellationToken cancellationToken) =>
        Read(organizationId, "VIEW_MATERIAL", () => references.ListConversionsAsync(organizationId, query, cursor, pageSize, cancellationToken), cancellationToken);

    [HttpPut("uom-conversions/{id:guid?}")]
    public Task<IActionResult> UpsertConversion(Guid? id, [FromQuery] Guid organizationId, [FromBody] MaterialUomConversionUpsertRequest request, CancellationToken cancellationToken) =>
        Write(organizationId, "EDIT_MATERIAL", () => references.UpsertConversionAsync(organizationId, id, request, cancellationToken), cancellationToken);

    [HttpGet("categories")]
    public Task<IActionResult> Categories([FromQuery] Guid organizationId, [FromQuery] string? status, CancellationToken cancellationToken) =>
        Read(organizationId, "VIEW_RECIPE", () => recipes.ListCategoriesAsync(organizationId, status, cancellationToken), cancellationToken);

    [HttpPut("categories/{id:guid?}")]
    public Task<IActionResult> UpsertCategory(Guid? id, [FromQuery] Guid organizationId, [FromBody] RecipeCategoryUpsertRequest request, CancellationToken cancellationToken) =>
        Write(organizationId, "EDIT_RECIPE", () => recipes.UpsertCategoryAsync(organizationId, id, request, cancellationToken), cancellationToken);

    [HttpGet("categories/template")]
    public async Task<IActionResult> CategoryTemplate([FromQuery] Guid organizationId, CancellationToken cancellationToken)
    {
        if (await Guard(organizationId, "VIEW_RECIPE", cancellationToken) is { } denied) return denied;
        return File(recipes.MasterTemplate(), "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", "recipe-category-master-template.xlsx");
    }

    [HttpGet("categories/export")]
    public async Task<IActionResult> CategoryExport([FromQuery] Guid organizationId, CancellationToken cancellationToken)
    {
        if (await Guard(organizationId, "VIEW_RECIPE", cancellationToken) is { } denied) return denied;
        return File(await recipes.ExportCategoriesAsync(organizationId, cancellationToken), "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", "recipe-category-master.xlsx");
    }

    [HttpPost("categories/import/preview")]
    [RequestSizeLimit(10 * 1024 * 1024)]
    public async Task<IActionResult> CategoryPreview([FromQuery] Guid organizationId, IFormFile? file, CancellationToken cancellationToken)
    {
        if (await Guard(organizationId, "VIEW_RECIPE", cancellationToken) is { } denied) return denied;
        if (file is null || file.Length == 0) return BadRequest(new ApiError("IMPORT_FILE_REQUIRED", "Choose an .xlsx file."));
        await using var stream = file.OpenReadStream();
        return Ok(recipes.PreviewMaster(stream, file.FileName));
    }

    [HttpPost("categories/import")]
    [RequestSizeLimit(10 * 1024 * 1024)]
    public async Task<IActionResult> CategoryImport([FromQuery] Guid organizationId, IFormFile? file, CancellationToken cancellationToken)
    {
        if (await Guard(organizationId, "EDIT_RECIPE", cancellationToken) is { } denied) return denied;
        if (file is null || file.Length == 0) return BadRequest(new ApiError("IMPORT_FILE_REQUIRED", "Choose an .xlsx file."));
        await using var stream = file.OpenReadStream();
        return await Execute(() => recipes.ImportCategoriesAsync(organizationId, stream, file.FileName, cancellationToken));
    }

    [HttpGet("families")]
    public Task<IActionResult> Families([FromQuery] Guid organizationId, [FromQuery] string? status, CancellationToken cancellationToken) =>
        Read(organizationId, "VIEW_RECIPE", () => recipes.ListFamiliesAsync(organizationId, status, cancellationToken), cancellationToken);

    [HttpPut("families/{id:guid?}")]
    public Task<IActionResult> UpsertFamily(Guid? id, [FromQuery] Guid organizationId, [FromBody] RecipeFamilyUpsertRequest request, CancellationToken cancellationToken) =>
        Write(organizationId, "EDIT_RECIPE", () => recipes.UpsertFamilyAsync(organizationId, id, request, cancellationToken), cancellationToken);

    [HttpGet("families/template")]
    public async Task<IActionResult> FamilyTemplate([FromQuery] Guid organizationId, CancellationToken cancellationToken)
    {
        if (await Guard(organizationId, "VIEW_RECIPE", cancellationToken) is { } denied) return denied;
        return File(recipes.MasterTemplate(), "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", "recipe-family-master-template.xlsx");
    }

    [HttpGet("families/export")]
    public async Task<IActionResult> FamilyExport([FromQuery] Guid organizationId, CancellationToken cancellationToken)
    {
        if (await Guard(organizationId, "VIEW_RECIPE", cancellationToken) is { } denied) return denied;
        return File(await recipes.ExportFamiliesAsync(organizationId, cancellationToken), "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", "recipe-family-master.xlsx");
    }

    [HttpPost("families/import/preview")]
    [RequestSizeLimit(10 * 1024 * 1024)]
    public async Task<IActionResult> FamilyPreview([FromQuery] Guid organizationId, IFormFile? file, CancellationToken cancellationToken)
    {
        if (await Guard(organizationId, "VIEW_RECIPE", cancellationToken) is { } denied) return denied;
        if (file is null || file.Length == 0) return BadRequest(new ApiError("IMPORT_FILE_REQUIRED", "Choose an .xlsx file."));
        await using var stream = file.OpenReadStream();
        return Ok(recipes.PreviewMaster(stream, file.FileName));
    }

    [HttpPost("families/import")]
    [RequestSizeLimit(10 * 1024 * 1024)]
    public async Task<IActionResult> FamilyImport([FromQuery] Guid organizationId, IFormFile? file, CancellationToken cancellationToken)
    {
        if (await Guard(organizationId, "EDIT_RECIPE", cancellationToken) is { } denied) return denied;
        if (file is null || file.Length == 0) return BadRequest(new ApiError("IMPORT_FILE_REQUIRED", "Choose an .xlsx file."));
        await using var stream = file.OpenReadStream();
        return await Execute(() => recipes.ImportFamiliesAsync(organizationId, stream, file.FileName, cancellationToken));
    }

    [HttpGet("locations")]
    public Task<IActionResult> Locations([FromQuery] Guid organizationId, [FromQuery] string? kind, [FromQuery] string? status, CancellationToken cancellationToken) =>
        Read(organizationId, "VIEW_RECIPE", () => recipes.ListLocationsAsync(organizationId, kind, status, cancellationToken), cancellationToken);

    [HttpPut("locations/{id:guid?}")]
    public Task<IActionResult> UpsertLocation(Guid? id, [FromQuery] Guid organizationId, [FromBody] RecipeLocationUpsertRequest request, CancellationToken cancellationToken) =>
        Write(organizationId, "EDIT_RECIPE", () => recipes.UpsertLocationAsync(organizationId, id, request, cancellationToken), cancellationToken);

    [HttpGet("locations/template")]
    public async Task<IActionResult> LocationTemplate([FromQuery] Guid organizationId, CancellationToken cancellationToken)
    {
        if (await Guard(organizationId, "VIEW_RECIPE", cancellationToken) is { } denied) return denied;
        return File(recipes.LocationTemplate(), "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", "location-master-template.xlsx");
    }

    [HttpGet("locations/export")]
    public async Task<IActionResult> LocationExport([FromQuery] Guid organizationId, CancellationToken cancellationToken)
    {
        if (await Guard(organizationId, "VIEW_RECIPE", cancellationToken) is { } denied) return denied;
        return File(await recipes.ExportLocationsAsync(organizationId, cancellationToken), "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", "location-master.xlsx");
    }

    [HttpPost("locations/import/preview")]
    [RequestSizeLimit(10 * 1024 * 1024)]
    public async Task<IActionResult> LocationPreview([FromQuery] Guid organizationId, IFormFile? file, CancellationToken cancellationToken)
    {
        if (await Guard(organizationId, "VIEW_RECIPE", cancellationToken) is { } denied) return denied;
        if (file is null || file.Length == 0) return BadRequest(new ApiError("IMPORT_FILE_REQUIRED", "Choose an .xlsx file."));
        await using var stream = file.OpenReadStream();
        return Ok(recipes.PreviewLocations(stream, file.FileName));
    }

    [HttpPost("locations/import")]
    [RequestSizeLimit(10 * 1024 * 1024)]
    public async Task<IActionResult> LocationImport([FromQuery] Guid organizationId, IFormFile? file, CancellationToken cancellationToken)
    {
        if (await Guard(organizationId, "EDIT_RECIPE", cancellationToken) is { } denied) return denied;
        if (file is null || file.Length == 0) return BadRequest(new ApiError("IMPORT_FILE_REQUIRED", "Choose an .xlsx file."));
        await using var stream = file.OpenReadStream();
        return await Execute(() => recipes.ImportLocationsAsync(organizationId, stream, file.FileName, cancellationToken));
    }

    [HttpGet("locations/{id:guid}/recipes")]
    public Task<IActionResult> LocationRecipes(Guid id, [FromQuery] Guid organizationId, CancellationToken cancellationToken) =>
        Read(organizationId, "VIEW_RECIPE", () => recipes.RecipesAtLocationAsync(organizationId, id, cancellationToken), cancellationToken);

    [HttpPost("locations/{id:guid}/recipes")]
    public Task<IActionResult> AssignLocationRecipe(Guid id, [FromQuery] Guid organizationId, [FromBody] OutletMenuItemUpsertRequest request, CancellationToken cancellationToken) =>
        Write(organizationId, "EDIT_RECIPE", async () => { await recipes.AssignRecipeToLocationAsync(organizationId, id, request.RecipeId, cancellationToken); return "OK"; }, cancellationToken);

    [HttpDelete("locations/{id:guid}/recipes/{recipeId:guid}")]
    public Task<IActionResult> RemoveLocationRecipe(Guid id, Guid recipeId, [FromQuery] Guid organizationId, CancellationToken cancellationToken) =>
        Write(organizationId, "EDIT_RECIPE", async () => { await recipes.RemoveRecipeFromLocationAsync(organizationId, id, recipeId, cancellationToken); return "OK"; }, cancellationToken);

    [HttpGet("recipes/template")]
    public async Task<IActionResult> RecipeTemplate([FromQuery] Guid organizationId, CancellationToken cancellationToken)
    {
        if (await Guard(organizationId, "VIEW_RECIPE", cancellationToken) is { } denied) return denied;
        return File(recipes.Template(), "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", "recipe-master-template.xlsx");
    }

    [HttpGet("recipes/export")]
    public async Task<IActionResult> RecipeExport([FromQuery] Guid organizationId, CancellationToken cancellationToken)
    {
        if (await Guard(organizationId, "VIEW_RECIPE", cancellationToken) is { } denied) return denied;
        return File(await recipes.ExportAsync(organizationId, cancellationToken), "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", "recipe-master.xlsx");
    }

    [HttpPost("recipes/import/preview")]
    [RequestSizeLimit(10 * 1024 * 1024)]
    public async Task<IActionResult> RecipePreview([FromQuery] Guid organizationId, IFormFile? file, CancellationToken cancellationToken)
    {
        if (await Guard(organizationId, "VIEW_RECIPE", cancellationToken) is { } denied) return denied;
        if (file is null || file.Length == 0) return BadRequest(new ApiError("IMPORT_FILE_REQUIRED", "Choose an .xlsx file."));
        await using var stream = file.OpenReadStream();
        return Ok(recipes.Preview(stream, file.FileName));
    }

    [HttpPost("recipes/import")]
    [RequestSizeLimit(10 * 1024 * 1024)]
    public async Task<IActionResult> RecipeImport([FromQuery] Guid organizationId, IFormFile? file, CancellationToken cancellationToken)
    {
        if (await Guard(organizationId, "CREATE_RECIPE", cancellationToken) is { } denied) return denied;
        if (file is null || file.Length == 0) return BadRequest(new ApiError("IMPORT_FILE_REQUIRED", "Choose an .xlsx file."));
        await using var stream = file.OpenReadStream();
        return await Execute(async () =>
        {
            var ids = await recipes.ImportAsync(CurrentSession()!, organizationId, stream, file.FileName, cancellationToken);
            return new { imported = ids.Count, recipeIds = ids };
        });
    }

    [HttpGet("recipes")]
    public Task<IActionResult> Recipes([FromQuery] Guid organizationId, [FromQuery] string? query, [FromQuery] bool withPosItem, CancellationToken cancellationToken) =>
        Read(organizationId, "VIEW_RECIPE", () => recipes.ListRecipesAsync(organizationId, query, cancellationToken, withPosItem), cancellationToken);

    [HttpGet("recipes/{id:guid}")]
    public Task<IActionResult> Recipe(Guid id, [FromQuery] Guid organizationId, [FromQuery] Guid? versionId, CancellationToken cancellationToken) =>
        Read(organizationId, "VIEW_RECIPE", () => recipes.GetVersionAsync(organizationId, id, versionId, cancellationToken), cancellationToken);

    [HttpGet("recipes/{id:guid}/versions")]
    public Task<IActionResult> Versions(Guid id, [FromQuery] Guid organizationId, CancellationToken cancellationToken) =>
        Read(organizationId, "VIEW_RECIPE", () => recipes.ListVersionsAsync(organizationId, id, cancellationToken), cancellationToken);

    [HttpPost("recipes")]
    public Task<IActionResult> CreateRecipe([FromQuery] Guid organizationId, [FromBody] RecipeUpsertRequest request, CancellationToken cancellationToken) =>
        Write(organizationId, "CREATE_RECIPE", () => recipes.CreateOrReviseAsync(CurrentSession()!, organizationId, null, request, cancellationToken), cancellationToken);

    [HttpPost("recipes/{id:guid}/versions")]
    public Task<IActionResult> ChangeRecipe(Guid id, [FromQuery] Guid organizationId, [FromBody] RecipeUpsertRequest request, CancellationToken cancellationToken) =>
        Write(organizationId, "EDIT_RECIPE", () => recipes.CreateOrReviseAsync(CurrentSession()!, organizationId, id, request, cancellationToken), cancellationToken);

    [HttpPost("recipes/{id:guid}/submit")]
    public async Task<IActionResult> SubmitRecipe(Guid id, [FromQuery] Guid organizationId, CancellationToken cancellationToken)
    {
        var session = CurrentSession();
        if (session is null) return Unauthorized(new ApiError("SESSION_INVALID", "Your session is no longer valid."));
        var canCreate = await access.HasPermissionAsync(session, "CREATE_RECIPE", organizationId, null, cancellationToken);
        var canEdit = await access.HasPermissionAsync(session, "EDIT_RECIPE", organizationId, null, cancellationToken);
        if (!canCreate && !canEdit)
            return StatusCode(403, new ApiError("FORBIDDEN", "You do not have permission for Recipe Management."));
        return await Execute(() => recipes.SubmitDraftAsync(organizationId, id, cancellationToken));
    }

    [HttpGet("approvals/workflows")]
    public Task<IActionResult> Workflows([FromQuery] Guid organizationId, CancellationToken cancellationToken) =>
        Read(organizationId, "MANAGE_RECIPE_WORKFLOW", () => approvals.ListWorkflowsAsync(organizationId, cancellationToken), cancellationToken);

    [HttpPut("approvals/workflows")]
    public Task<IActionResult> UpsertWorkflow([FromQuery] Guid organizationId, [FromBody] RecipeApprovalWorkflowUpsertRequest request, CancellationToken cancellationToken) =>
        Write(organizationId, "MANAGE_RECIPE_WORKFLOW", () => approvals.UpsertWorkflowAsync(organizationId, request, cancellationToken), cancellationToken);

    [HttpGet("approvals")]
    public Task<IActionResult> Approvals([FromQuery] Guid organizationId, CancellationToken cancellationToken) =>
        Read(organizationId, "APPROVE_RECIPE", () => approvals.PendingAsync(organizationId, cancellationToken), cancellationToken);

    [HttpPost("approvals/{id:guid}/approve")]
    public Task<IActionResult> Approve(Guid id, [FromQuery] Guid organizationId, [FromBody] RecipeApprovalDecisionRequest request, CancellationToken cancellationToken) =>
        Write(organizationId, "APPROVE_RECIPE", async () => { await approvals.DecideAsync(CurrentSession()!, organizationId, id, true, request, cancellationToken); return "OK"; }, cancellationToken);

    [HttpPost("approvals/{id:guid}/reject")]
    public Task<IActionResult> Reject(Guid id, [FromQuery] Guid organizationId, [FromBody] RecipeApprovalDecisionRequest request, CancellationToken cancellationToken) =>
        Write(organizationId, "APPROVE_RECIPE", async () => { await approvals.DecideAsync(CurrentSession()!, organizationId, id, false, request, cancellationToken); return "OK"; }, cancellationToken);

    [HttpGet("pos-sources")]
    public Task<IActionResult> PosSources([FromQuery] Guid organizationId, CancellationToken cancellationToken) =>
        Read(organizationId, "VIEW_POS_INTEGRATION", () => pos.ListAsync(organizationId, cancellationToken), cancellationToken);

    [HttpPut("pos-sources/{id:guid?}")]
    public Task<IActionResult> UpsertPosSource(Guid? id, [FromQuery] Guid organizationId, [FromBody] PosSourceUpsertRequest request, CancellationToken cancellationToken) =>
        Write(organizationId, "MANAGE_POS_INTEGRATION", () => pos.UpsertAsync(organizationId, id, request, cancellationToken), cancellationToken);

    [HttpGet("pos-sources/{id:guid}/outlets")]
    public Task<IActionResult> Outlets(Guid id, [FromQuery] Guid organizationId, CancellationToken cancellationToken) =>
        Read(organizationId, "VIEW_POS_INTEGRATION", () => pos.ListOutletsAsync(organizationId, id, cancellationToken), cancellationToken);

    [HttpPut("pos-sources/{sourceId:guid}/outlets/{id:guid?}")]
    public Task<IActionResult> UpsertOutlet(Guid sourceId, Guid? id, [FromQuery] Guid organizationId, [FromBody] PosOutletMappingUpsertRequest request, CancellationToken cancellationToken) =>
        Write(organizationId, "MANAGE_POS_INTEGRATION", () => pos.UpsertOutletAsync(organizationId, sourceId, id, request, cancellationToken), cancellationToken);

    [HttpGet("pos-sources/{sourceId:guid}/outlets/{outletId:guid}/menu-items")]
    public Task<IActionResult> MenuItems(Guid sourceId, Guid outletId, [FromQuery] Guid organizationId, CancellationToken cancellationToken) =>
        Read(organizationId, "VIEW_POS_INTEGRATION", () => pos.ListMenuItemsAsync(organizationId, sourceId, outletId, cancellationToken), cancellationToken);

    [HttpPost("pos-sources/{sourceId:guid}/outlets/{outletId:guid}/menu-items")]
    public Task<IActionResult> AddMenuItem(Guid sourceId, Guid outletId, [FromQuery] Guid organizationId, [FromBody] OutletMenuItemUpsertRequest request, CancellationToken cancellationToken) =>
        Write(organizationId, "MANAGE_POS_INTEGRATION", () => pos.AddMenuItemAsync(organizationId, sourceId, outletId, request, cancellationToken), cancellationToken);

    [HttpDelete("pos-sources/{sourceId:guid}/outlets/{outletId:guid}/menu-items/{id:guid}")]
    public Task<IActionResult> RemoveMenuItem(Guid sourceId, Guid outletId, Guid id, [FromQuery] Guid organizationId, CancellationToken cancellationToken) =>
        Write(organizationId, "MANAGE_POS_INTEGRATION", async () => { await pos.RemoveMenuItemAsync(organizationId, sourceId, outletId, id, cancellationToken); return "OK"; }, cancellationToken);

    [HttpGet("pos-sources/{id:guid}/items")]
    public Task<IActionResult> Items(Guid id, [FromQuery] Guid organizationId, CancellationToken cancellationToken) =>
        Read(organizationId, "VIEW_POS_INTEGRATION", () => pos.ListItemsAsync(organizationId, id, cancellationToken), cancellationToken);

    [HttpPut("pos-sources/{sourceId:guid}/items/{id:guid?}")]
    public Task<IActionResult> UpsertItem(Guid sourceId, Guid? id, [FromQuery] Guid organizationId, [FromBody] PosItemRecipeMappingUpsertRequest request, CancellationToken cancellationToken) =>
        Write(organizationId, "MANAGE_POS_INTEGRATION", () => pos.UpsertItemAsync(CurrentSession()!, organizationId, sourceId, id, request, cancellationToken), cancellationToken);

    [HttpPost("pos-transactions")]
    public Task<IActionResult> Intake([FromQuery] Guid organizationId, [FromBody] PosSaleIntakeRequest request, CancellationToken cancellationToken) =>
        Write(organizationId, "MANAGE_POS_INTEGRATION", () => intake.ReceiveAsync(organizationId, request, false, cancellationToken), cancellationToken);

    [HttpGet("pos-sales/template")]
    public async Task<IActionResult> PosSalesTemplate([FromQuery] Guid organizationId, CancellationToken cancellationToken)
    {
        if (await Guard(organizationId, "VIEW_POS_INTEGRATION", cancellationToken) is { } denied) return denied;
        return File(intake.SalesTemplate(), "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", "recipe-sales-upload-template.xlsx");
    }

    [HttpPost("pos-sales/import/preview")]
    [RequestSizeLimit(10 * 1024 * 1024)]
    public async Task<IActionResult> PosSalesPreview([FromQuery] Guid organizationId, [FromQuery] Guid? posSourceId, IFormFile? file, CancellationToken cancellationToken)
    {
        if (await Guard(organizationId, "MANAGE_POS_INTEGRATION", cancellationToken) is { } denied) return denied;
        if (file is null || file.Length == 0) return BadRequest(new ApiError("IMPORT_FILE_REQUIRED", "Choose an .xlsx file."));
        await using var stream = file.OpenReadStream();
        return await Execute(() => intake.PreviewSalesAsync(organizationId, stream, file.FileName, posSourceId, CurrentSession()?.UserId, cancellationToken));
    }

    [HttpPost("pos-sales/import")]
    [RequestSizeLimit(10 * 1024 * 1024)]
    public async Task<IActionResult> PosSalesImport([FromQuery] Guid organizationId, [FromQuery] Guid? posSourceId, IFormFile? file, CancellationToken cancellationToken)
    {
        if (await Guard(organizationId, "MANAGE_POS_INTEGRATION", cancellationToken) is { } denied) return denied;
        if (file is null || file.Length == 0) return BadRequest(new ApiError("IMPORT_FILE_REQUIRED", "Choose an .xlsx file."));
        await using var stream = file.OpenReadStream();
        return await Execute(() => intake.ImportSalesAsync(organizationId, stream, posSourceId, CurrentSession()?.UserId, cancellationToken, file.FileName));
    }

    [HttpGet("pos-sales/uploads")]
    public Task<IActionResult> PosSalesUploads([FromQuery] Guid organizationId, CancellationToken cancellationToken) =>
        Read(organizationId, "VIEW_POS_INTEGRATION", () => intake.ListUploadsAsync(organizationId, cancellationToken), cancellationToken);

    [HttpGet("pos-sales/uploads/{id:guid}")]
    public Task<IActionResult> PosSalesUpload(Guid id, [FromQuery] Guid organizationId, CancellationToken cancellationToken) =>
        Read(organizationId, "VIEW_POS_INTEGRATION", () => intake.GetUploadAsync(organizationId, id, cancellationToken), cancellationToken);

    [HttpPost("pos-sales/uploads/{id:guid}/process")]
    public Task<IActionResult> ProcessPosSalesUpload(Guid id, [FromQuery] Guid organizationId, CancellationToken cancellationToken) =>
        Write(organizationId, "MANAGE_POS_INTEGRATION", () => intake.ProcessUploadAsync(organizationId, id, cancellationToken), cancellationToken);

    [HttpGet("transactions")]
    public Task<IActionResult> Transactions(
        [FromQuery] Guid organizationId, [FromQuery] string? status, [FromQuery] DateOnly? businessDate,
        [FromQuery] string? outlet, [FromQuery] string? posCode, [FromQuery] string? material, [FromQuery] string? transactionId,
        CancellationToken cancellationToken) =>
        Read(organizationId, "VIEW_RECIPE_TRANSACTION", () => intake.ListAsync(organizationId, status, businessDate, outlet, posCode, material, transactionId, cancellationToken), cancellationToken);

    [HttpGet("transactions/{id:guid}")]
    public Task<IActionResult> Transaction(Guid id, [FromQuery] Guid organizationId, CancellationToken cancellationToken) =>
        Read(organizationId, "VIEW_RECIPE_TRANSACTION", () => intake.DetailAsync(organizationId, id, cancellationToken), cancellationToken);

    [HttpPost("transactions/{id:guid}/reprocess")]
    public Task<IActionResult> Reprocess(Guid id, [FromQuery] Guid organizationId, CancellationToken cancellationToken) =>
        Write(organizationId, "REPROCESS_RECIPE_TRANSACTION", () => intake.ReprocessAsync(organizationId, id, cancellationToken), cancellationToken);

    private async Task<IActionResult> Read<T>(Guid organizationId, string permission, Func<Task<T>> work, CancellationToken cancellationToken)
    {
        if (await Guard(organizationId, permission, cancellationToken) is { } denied) return denied;
        return await Execute(work);
    }

    private async Task<IActionResult> Write<T>(Guid organizationId, string permission, Func<Task<T>> work, CancellationToken cancellationToken)
    {
        if (await Guard(organizationId, permission, cancellationToken) is { } denied) return denied;
        return await Execute(work);
    }

    private async Task<IActionResult?> Guard(Guid organizationId, string permission, CancellationToken cancellationToken)
    {
        var session = CurrentSession();
        if (session is null) return Unauthorized(new ApiError("SESSION_INVALID", "Your session is no longer valid."));
        if (!await access.HasPermissionAsync(session, permission, organizationId, null, cancellationToken))
            return StatusCode(403, new ApiError("FORBIDDEN", "You do not have permission for Recipe Management."));
        return null;
    }

    private async Task<IActionResult> Execute<T>(Func<Task<T>> work)
    {
        try
        {
            var result = await work();
            return result is string ? NoContent() : Ok(result);
        }
        catch (RecipeManagementException exception)
        {
            return StatusCode(exception.Status, new ApiError(exception.Code, exception.Message));
        }
        catch (IntegrationException exception)
        {
            return StatusCode(exception.Status, new ApiError(exception.Code, exception.Message));
        }
        catch (Microsoft.EntityFrameworkCore.DbUpdateException exception)
        {
            return StatusCode(400, new ApiError("MATERIAL_SAVE_FAILED", exception.InnerException?.Message ?? exception.Message));
        }
    }

    private Session? CurrentSession() => HttpContext.Items[SessionContext.ItemKey] as Session;
}
