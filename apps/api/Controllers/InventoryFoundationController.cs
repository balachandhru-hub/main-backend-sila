using Microsoft.AspNetCore.Mvc;
using SilaMe.Api.Auth;
using SilaMe.Api.DTOs;
using SilaMe.Api.Models;
using SilaMe.Api.Services;

namespace SilaMe.Api.Controllers;

[ApiController]
[Route("api/v1/inventory")]
public sealed class InventoryFoundationController(
    IAccessService access,
    InventoryLocationService locations,
    InventoryWorkspaceService workspace,
    InternalTransferService transfers,
    MaterialLocationService materialLocations) : ControllerBase
{
    [HttpGet("locations")]
    public Task<IActionResult> Locations([FromQuery] Guid organizationId, [FromQuery] string? query, [FromQuery] string? locationType, [FromQuery] Guid? propertyLocationId, [FromQuery] bool? active, CancellationToken cancellationToken) =>
        Read(organizationId, ["VIEW_INVENTORY", "VIEW_LOCATION", "MANAGE_LOCATION"], () => locations.ListAsync(organizationId, query, locationType, propertyLocationId, active, cancellationToken), cancellationToken);

    [HttpGet("locations/options")]
    public Task<IActionResult> LocationOptions([FromQuery] Guid organizationId, CancellationToken cancellationToken) =>
        Read(organizationId, ["VIEW_INVENTORY", "VIEW_LOCATION", "MANAGE_LOCATION"], () => locations.OptionsAsync(organizationId, cancellationToken), cancellationToken);

    [HttpGet("locations/assignable-users")]
    public Task<IActionResult> AssignableUsers([FromQuery] Guid organizationId, CancellationToken cancellationToken) =>
        Read(organizationId, ["MANAGE_LOCATION"], () => workspace.AssignableUsersAsync(organizationId, cancellationToken), cancellationToken);

    [HttpGet("locations/{id:guid}/users")]
    public Task<IActionResult> LocationUsers(Guid id, [FromQuery] Guid organizationId, CancellationToken cancellationToken) =>
        Read(organizationId, ["VIEW_INVENTORY", "VIEW_LOCATION", "MANAGE_LOCATION"], () => workspace.LocationUsersAsync(organizationId, id, cancellationToken), cancellationToken);

    [HttpPost("locations/{id:guid}/users")]
    public Task<IActionResult> AssignLocationUser(Guid id, [FromQuery] Guid organizationId, [FromBody] AssignLocationUserRequest request, CancellationToken cancellationToken) =>
        Write(organizationId, ["MANAGE_LOCATION"], async () => { await workspace.AssignUserAsync(organizationId, id, request, cancellationToken); return "OK"; }, cancellationToken);

    [HttpGet("locations/{id:guid}")]
    public Task<IActionResult> Location(Guid id, [FromQuery] Guid organizationId, CancellationToken cancellationToken) =>
        Read(organizationId, ["VIEW_INVENTORY", "VIEW_LOCATION"], () => locations.GetAsync(organizationId, id, cancellationToken), cancellationToken);

    [HttpPut("locations")]
    public Task<IActionResult> UpsertLocation([FromQuery] Guid organizationId, [FromBody] InventoryLocationUpsertRequest request, CancellationToken cancellationToken) =>
        Write(organizationId, ["MANAGE_LOCATION"], () => locations.UpsertAsync(CurrentSession()!, organizationId, request, cancellationToken), cancellationToken);

    [HttpPost("locations/{id:guid}/activate")]
    public Task<IActionResult> Activate(Guid id, [FromQuery] Guid organizationId, [FromQuery] bool active, CancellationToken cancellationToken) =>
        Write(organizationId, ["MANAGE_LOCATION"], async () => { await locations.ActivateAsync(CurrentSession()!, organizationId, id, active, cancellationToken); return "OK"; }, cancellationToken);

    [HttpGet("locations/template")]
    public async Task<IActionResult> LocationTemplate([FromQuery] Guid organizationId, CancellationToken cancellationToken)
    {
        if (await GuardAny(organizationId, ["VIEW_INVENTORY", "VIEW_LOCATION", "MANAGE_LOCATION"], cancellationToken) is { } denied) return denied;
        return File(locations.Template(), "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", "inventory-location-master-template.xlsx");
    }

    [HttpGet("locations/export")]
    public async Task<IActionResult> LocationExport([FromQuery] Guid organizationId, CancellationToken cancellationToken)
    {
        if (await GuardAny(organizationId, ["VIEW_INVENTORY", "VIEW_LOCATION", "MANAGE_LOCATION"], cancellationToken) is { } denied) return denied;
        return File(await locations.ExportAsync(organizationId, cancellationToken), "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", "inventory-location-master.xlsx");
    }

    [HttpPost("locations/import/preview")]
    [RequestSizeLimit(10 * 1024 * 1024)]
    public async Task<IActionResult> LocationPreview([FromQuery] Guid organizationId, IFormFile? file, CancellationToken cancellationToken)
    {
        if (await GuardAny(organizationId, ["MANAGE_LOCATION"], cancellationToken) is { } denied) return denied;
        if (file is null || file.Length == 0) return BadRequest(new ApiError("IMPORT_FILE_REQUIRED", "Choose an .xlsx file."));
        await using var stream = file.OpenReadStream();
        return await Execute(() => locations.ImportAsync(CurrentSession()!, organizationId, stream, file.FileName, false, cancellationToken));
    }

    [HttpPost("locations/import")]
    [RequestSizeLimit(10 * 1024 * 1024)]
    public async Task<IActionResult> LocationImport([FromQuery] Guid organizationId, IFormFile? file, CancellationToken cancellationToken)
    {
        if (await GuardAny(organizationId, ["MANAGE_LOCATION"], cancellationToken) is { } denied) return denied;
        if (file is null || file.Length == 0) return BadRequest(new ApiError("IMPORT_FILE_REQUIRED", "Choose an .xlsx file."));
        await using var stream = file.OpenReadStream();
        return await Execute(() => locations.ImportAsync(CurrentSession()!, organizationId, stream, file.FileName, true, cancellationToken));
    }

    [HttpGet("locations/{id:guid}/materials")]
    public Task<IActionResult> LocationMaterials(Guid id, [FromQuery] Guid organizationId, CancellationToken cancellationToken) =>
        Read(organizationId, ["VIEW_INVENTORY", "VIEW_LOCATION", "MANAGE_LOCATION"], () => materialLocations.ListForLocationAsync(organizationId, id, cancellationToken), cancellationToken);

    [HttpPut("locations/{id:guid}/materials")]
    public Task<IActionResult> AssignLocationMaterial(Guid id, [FromQuery] Guid organizationId, [FromBody] MaterialLocationUpsertRequest request, CancellationToken cancellationToken) =>
        Write(organizationId, ["MANAGE_LOCATION"], () => materialLocations.UpsertAsync(CurrentSession()!, organizationId, request with { InventoryLocationId = id }, cancellationToken), cancellationToken);

    [HttpGet("materials/{id:guid}/locations")]
    public Task<IActionResult> MaterialLocations(Guid id, [FromQuery] Guid organizationId, CancellationToken cancellationToken) =>
        Read(organizationId, ["VIEW_INVENTORY", "VIEW_LOCATION", "MANAGE_LOCATION"], () => materialLocations.ListForMaterialAsync(organizationId, id, cancellationToken), cancellationToken);

    [HttpPut("materials/{id:guid}/locations")]
    public Task<IActionResult> AssignMaterialLocation(Guid id, [FromQuery] Guid organizationId, [FromBody] MaterialLocationUpsertRequest request, CancellationToken cancellationToken) =>
        Write(organizationId, ["MANAGE_LOCATION"], () => materialLocations.UpsertAsync(CurrentSession()!, organizationId, request with { MaterialId = id }, cancellationToken), cancellationToken);

    [HttpPost("opening-stock")]
    public Task<IActionResult> OpeningStock([FromQuery] Guid organizationId, [FromBody] OpeningStockRequest request, CancellationToken cancellationToken) =>
        Write(organizationId, ["POST_OPENING_STOCK", "CREATE_STOCK_ADJUSTMENT", "POST_STOCK_ADJUSTMENT", "MANAGE_LOCATION"], () => materialLocations.PostOpeningStockAsync(CurrentSession()!, organizationId, request, cancellationToken), cancellationToken);

    [HttpGet("dashboard")]
    public Task<IActionResult> Dashboard([FromQuery] Guid organizationId, [FromQuery] DateTime? businessDate, [FromQuery] Guid? propertyId, [FromQuery] string? locationType, [FromQuery] Guid? locationId, [FromQuery] string? materialGroup, CancellationToken cancellationToken) =>
        Read(organizationId, ["VIEW_INVENTORY", "VIEW_STOCK_BALANCE"], () => workspace.DashboardAsync(organizationId, businessDate, propertyId, locationType, locationId, materialGroup, cancellationToken), cancellationToken);

    [HttpGet("alerts")]
    public Task<IActionResult> Alerts([FromQuery] Guid organizationId, CancellationToken cancellationToken) =>
        Read(organizationId, ["VIEW_INVENTORY", "VIEW_OPERATION_ALERTS"], () => workspace.AlertsAsync(organizationId, cancellationToken), cancellationToken);

    [HttpPost("alerts/{id:guid}/actions")]
    public Task<IActionResult> AlertAction(Guid id, [FromQuery] Guid organizationId, [FromBody] InventoryAlertActionRequest request, CancellationToken cancellationToken) =>
        Write(organizationId, ["VIEW_INVENTORY"], async () => { await workspace.AlertActionAsync(CurrentSession()!, organizationId, id, request, cancellationToken); return "OK"; }, cancellationToken);

    [HttpGet("live/search")]
    public Task<IActionResult> LiveSearch([FromQuery] Guid organizationId, [FromQuery] string? query, [FromQuery] string? materialGroup, [FromQuery] string? category, [FromQuery] string? cursor, [FromQuery] int pageSize, CancellationToken cancellationToken) =>
        Read(organizationId, ["VIEW_INVENTORY", "VIEW_STOCK_BALANCE"], () => workspace.SearchMaterialsAsync(organizationId, query, materialGroup, category, cursor, pageSize, cancellationToken), cancellationToken);

    [HttpGet("live/{materialId:guid}")]
    public Task<IActionResult> Live(Guid materialId, [FromQuery] Guid organizationId, [FromQuery] decimal requiredQty, [FromQuery] Guid? currentLocationId, CancellationToken cancellationToken) =>
        Read(organizationId, ["VIEW_INVENTORY", "VIEW_STOCK_BALANCE"], async () =>
        {
            var mine = CurrentSession()?.UserId is Guid userId ? await workspace.MyLocationAsync(organizationId, userId, cancellationToken) : null;
            return await workspace.LiveAsync(organizationId, materialId, new LiveDecisionRequest(requiredQty, currentLocationId), mine?.Id, cancellationToken);
        }, cancellationToken);

    [HttpGet("mobile/home")]
    public Task<IActionResult> MobileHome([FromQuery] Guid organizationId, CancellationToken cancellationToken) =>
        Read(organizationId, ["VIEW_INVENTORY", "VIEW_STOCK_BALANCE"], () => workspace.MobileHomeAsync(organizationId, CurrentSession()!.UserId ?? Guid.Empty, cancellationToken), cancellationToken);

    [HttpGet("my-location")]
    public Task<IActionResult> MyLocation([FromQuery] Guid organizationId, CancellationToken cancellationToken) =>
        Read(organizationId, ["VIEW_INVENTORY"], async () =>
        {
            var mine = await workspace.MyLocationAsync(organizationId, CurrentSession()!.UserId ?? Guid.Empty, cancellationToken)
                ?? throw new RecipeManagementException("LOCATION_NOT_ASSIGNED", "No operational inventory location is assigned.", 404);
            return new MyLocationRow(mine.Id, mine.LocationCode, mine.LocationName, mine.LocationType.ToString(), true);
        }, cancellationToken);

    [HttpPost("my-location")]
    public Task<IActionResult> AssignMyLocation([FromQuery] Guid organizationId, [FromQuery] Guid locationId, CancellationToken cancellationToken) =>
        Write(organizationId, ["VIEW_INVENTORY"], async () => { await workspace.AssignMyLocationAsync(organizationId, CurrentSession()!.UserId ?? Guid.Empty, locationId, cancellationToken); return "OK"; }, cancellationToken);

    [HttpGet("quick-transfer/policy")]
    public Task<IActionResult> Policy([FromQuery] Guid organizationId, CancellationToken cancellationToken) =>
        Read(organizationId, ["VIEW_INVENTORY"], () => workspace.PolicyAsync(organizationId, cancellationToken), cancellationToken);

    [HttpPut("quick-transfer/policy")]
    public Task<IActionResult> SavePolicy([FromQuery] Guid organizationId, [FromBody] QuickTransferPolicyRow request, CancellationToken cancellationToken) =>
        Write(organizationId, ["MANAGE_LOCATION"], () => workspace.SavePolicyAsync(organizationId, request, cancellationToken), cancellationToken);

    [HttpPost("physical-inventory-requests")]
    public async Task<IActionResult> PhysicalInventory([FromQuery] Guid organizationId, [FromBody] PhysicalInventoryRequestInput request, CancellationToken cancellationToken)
    {
        if (await GuardAny(organizationId, ["CREATE_INVENTORY_COUNT", "APPROVE_INVENTORY_COUNT"], cancellationToken) is { } denied) return denied;
        var session = CurrentSession()!;
        var surprise = await access.HasPermissionAsync(session, "APPROVE_INVENTORY_COUNT", organizationId, null, cancellationToken);
        return await Execute(() => workspace.RequestPhysicalInventoryAsync(session, organizationId, request, surprise, cancellationToken));
    }

    [HttpPost("purchase-requests")]
    public Task<IActionResult> PurchaseRequest([FromQuery] Guid organizationId, [FromBody] InternalPurchaseRequestInput request, CancellationToken cancellationToken) =>
        Write(organizationId, ["VIEW_INVENTORY", "VIEW_PURCHASE_SUGGESTION"], () => workspace.CreatePurchaseRequestAsync(CurrentSession()!, organizationId, request, cancellationToken), cancellationToken);

    [HttpGet("transfers")]
    public Task<IActionResult> Transfers([FromQuery] Guid organizationId, [FromQuery] string? mode, [FromQuery] string? status, [FromQuery] Guid? propertyId, [FromQuery] Guid? fromLocationId, [FromQuery] Guid? toLocationId, [FromQuery] DateTime? fromDate, [FromQuery] DateTime? toDate, CancellationToken cancellationToken) =>
        Read(organizationId, ["VIEW_INVENTORY", "VIEW_STOCK_TRANSACTION"], () => transfers.ListAsync(organizationId, mode, status, propertyId, fromLocationId, toLocationId, fromDate, toDate, cancellationToken), cancellationToken);

    [HttpGet("transfers/{id:guid}")]
    public Task<IActionResult> Transfer(Guid id, [FromQuery] Guid organizationId, CancellationToken cancellationToken) =>
        Read(organizationId, ["VIEW_INVENTORY"], async () => await transfers.GetAsync(organizationId, id, await Granted(organizationId, cancellationToken), cancellationToken), cancellationToken);

    [HttpPost("transfers")]
    public Task<IActionResult> CreateTransfer([FromQuery] Guid organizationId, [FromBody] ItoCreateRequest request, CancellationToken cancellationToken) =>
        Write(organizationId, ["CREATE_INTERNAL_TRANSFER", "CREATE_STOCK_TRANSFER"], async () => await transfers.CreateAsync(CurrentSession()!, organizationId, request, await Granted(organizationId, cancellationToken), cancellationToken), cancellationToken);

    [HttpPost("transfers/quick")]
    public Task<IActionResult> QuickGetOne([FromQuery] Guid organizationId, [FromQuery] Guid materialId, [FromQuery] Guid fromLocationId, [FromQuery] Guid toLocationId, CancellationToken cancellationToken) =>
        Write(organizationId, ["CREATE_INTERNAL_TRANSFER", "CREATE_STOCK_TRANSFER"], async () => await transfers.QuickGetOneAsync(CurrentSession()!, organizationId, materialId, fromLocationId, toLocationId, await Granted(organizationId, cancellationToken), cancellationToken), cancellationToken);

    [HttpPost("transfers/{id:guid}/approve")]
    public Task<IActionResult> Approve(Guid id, [FromQuery] Guid organizationId, [FromBody] ItoApproveRequest request, CancellationToken cancellationToken) =>
        Write(organizationId, ["APPROVE_STOCK_TRANSFER"], async () => await transfers.ApproveAsync(CurrentSession()!, organizationId, id, request, await Granted(organizationId, cancellationToken), cancellationToken), cancellationToken);

    [HttpPost("transfers/{id:guid}/reject")]
    public Task<IActionResult> Reject(Guid id, [FromQuery] Guid organizationId, [FromBody] ItoApproveRequest request, CancellationToken cancellationToken) =>
        Write(organizationId, ["APPROVE_STOCK_TRANSFER"], async () => await transfers.RejectAsync(CurrentSession()!, organizationId, id, request.Comment, await Granted(organizationId, cancellationToken), cancellationToken), cancellationToken);

    [HttpPost("transfers/{id:guid}/dispatch")]
    public Task<IActionResult> Dispatch(Guid id, [FromQuery] Guid organizationId, [FromBody] ItoQtyRequest request, CancellationToken cancellationToken) =>
        Write(organizationId, ["DISPATCH_INTERNAL_TRANSFER", "DISPATCH_STOCK_TRANSFER"], async () => await transfers.DispatchAsync(CurrentSession()!, organizationId, id, request, await Granted(organizationId, cancellationToken), cancellationToken), cancellationToken);

    [HttpPost("transfers/{id:guid}/handover")]
    public Task<IActionResult> Handover(Guid id, [FromQuery] Guid organizationId, [FromBody] ItoQtyRequest request, CancellationToken cancellationToken) =>
        Write(organizationId, ["DISPATCH_INTERNAL_TRANSFER", "DISPATCH_STOCK_TRANSFER"], async () => await transfers.DispatchAsync(CurrentSession()!, organizationId, id, request, await Granted(organizationId, cancellationToken), cancellationToken), cancellationToken);

    [HttpPost("transfers/{id:guid}/receive")]
    public Task<IActionResult> Receive(Guid id, [FromQuery] Guid organizationId, [FromBody] ItoQtyRequest request, CancellationToken cancellationToken) =>
        Write(organizationId, ["RECEIVE_INTERNAL_TRANSFER", "RECEIVE_STOCK_TRANSFER"], async () => await transfers.ReceiveAsync(CurrentSession()!, organizationId, id, request, await Granted(organizationId, cancellationToken), cancellationToken), cancellationToken);

    [HttpPost("transfers/{id:guid}/discrepancy")]
    public Task<IActionResult> Discrepancy(Guid id, [FromQuery] Guid organizationId, [FromBody] ItoApproveRequest request, CancellationToken cancellationToken) =>
        Write(organizationId, ["RECEIVE_INTERNAL_TRANSFER", "RECEIVE_STOCK_TRANSFER", "APPROVE_STOCK_TRANSFER"], async () => await transfers.ReportDiscrepancyAsync(CurrentSession()!, organizationId, id, request.Comment, await Granted(organizationId, cancellationToken), cancellationToken), cancellationToken);

    [HttpPost("transfers/{id:guid}/dispute-already-collected")]
    public Task<IActionResult> DisputeAlreadyCollected(Guid id, [FromQuery] Guid organizationId, [FromBody] ItoApproveRequest request, CancellationToken cancellationToken) =>
        Write(organizationId, ["DISPATCH_INTERNAL_TRANSFER", "DISPATCH_STOCK_TRANSFER", "APPROVE_STOCK_TRANSFER"], async () => await transfers.DisputeAlreadyCollectedAsync(CurrentSession()!, organizationId, id, request.Comment, await Granted(organizationId, cancellationToken), cancellationToken), cancellationToken);

    private async Task<HashSet<string>> Granted(Guid organizationId, CancellationToken cancellationToken)
    {
        string[] keys =
        [
            "APPROVE_STOCK_TRANSFER", "DISPATCH_INTERNAL_TRANSFER", "DISPATCH_STOCK_TRANSFER",
            "RECEIVE_INTERNAL_TRANSFER", "RECEIVE_STOCK_TRANSFER", "CREATE_INTERNAL_TRANSFER", "CREATE_STOCK_TRANSFER"
        ];
        var set = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var session = CurrentSession();
        if (session is null) return set;
        foreach (var key in keys)
        {
            if (await access.HasPermissionAsync(session, key, organizationId, null, cancellationToken))
                set.Add(key);
        }
        return set;
    }

    private Task<IActionResult> Read<T>(Guid organizationId, string[] permissions, Func<Task<T>> work, CancellationToken cancellationToken) =>
        Run(organizationId, permissions, work, cancellationToken);

    private Task<IActionResult> Write<T>(Guid organizationId, string[] permissions, Func<Task<T>> work, CancellationToken cancellationToken) =>
        Run(organizationId, permissions, work, cancellationToken);

    private async Task<IActionResult> Run<T>(Guid organizationId, string[] permissions, Func<Task<T>> work, CancellationToken cancellationToken)
    {
        if (await GuardAny(organizationId, permissions, cancellationToken) is { } denied) return denied;
        return await Execute(work);
    }

    private async Task<IActionResult?> GuardAny(Guid organizationId, IReadOnlyList<string> permissions, CancellationToken cancellationToken)
    {
        var session = CurrentSession();
        if (session is null) return Unauthorized(new ApiError("SESSION_INVALID", "Your session is no longer valid."));
        foreach (var permission in permissions)
        {
            if (await access.HasPermissionAsync(session, permission, organizationId, null, cancellationToken))
                return null;
        }
        return StatusCode(403, new ApiError("FORBIDDEN", "You do not have permission for Inventory."));
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
    }

    private Session? CurrentSession() => HttpContext.Items[SessionContext.ItemKey] as Session;
}
