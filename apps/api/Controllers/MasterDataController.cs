using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SilaMe.Api.Auth;
using SilaMe.Api.Data;
using SilaMe.Api.DTOs;
using SilaMe.Api.Models;
using SilaMe.Api.Services;

namespace SilaMe.Api.Controllers;

[ApiController]
[Route("api/v1/master-data")]
public sealed class MasterDataController(SilaMeDbContext db, IAccessService access, IntegrationService integrations, MasterRecordService masters) : ControllerBase
{
    [HttpGet("suppliers")]
    public async Task<IActionResult> Suppliers([FromQuery] Guid organizationId, [FromQuery] string? entityCode, [FromQuery] string? query, [FromQuery] string? status, CancellationToken cancellationToken)
    {
        var session = CurrentSession();
        if (session is null) return Unauthorized(new ApiError("SESSION_INVALID", "Your session is no longer valid."));
        if (!await CanAsync(session, organizationId, PermissionKeys.ViewPurchaseOrder, cancellationToken)) return Forbid();
        var suppliers = db.Suppliers.AsNoTracking().Include(item => item.Aliases)
            .Where(item => item.OrganizationId == organizationId && !item.IsDeleted &&
                (string.IsNullOrWhiteSpace(entityCode) || item.EntityCode == entityCode || item.EntityCode == "ALL" || entityCode == "ALL"));
        if (Enum.TryParse<StatusKind>(status, true, out var parsedStatus))
            suppliers = suppliers.Where(item => item.Status == parsedStatus);
        if (string.IsNullOrWhiteSpace(query))
        {
            var rows = await suppliers.OrderBy(item => item.Name).Take(500).ToListAsync(cancellationToken);
            return Ok(rows.Select(ToSupplier));
        }
        var matches = await SupplierSearch.Filter(suppliers, query).Take(80).ToListAsync(cancellationToken);
        return Ok(SupplierSearch.Rank(matches, query).Select(ToSupplier));
    }

    [HttpGet("suppliers/{id:guid}")]
    public async Task<IActionResult> Supplier(Guid id, [FromQuery] Guid organizationId, CancellationToken cancellationToken)
    {
        var session = CurrentSession();
        if (session is null) return Unauthorized(new ApiError("SESSION_INVALID", "Your session is no longer valid."));
        if (!await CanAsync(session, organizationId, PermissionKeys.ViewPurchaseOrder, cancellationToken)) return Forbid();
        var supplier = await db.Suppliers.AsNoTracking().Include(item => item.Aliases)
            .SingleOrDefaultAsync(item => item.Id == id && item.OrganizationId == organizationId, cancellationToken);
        return supplier is null ? NotFound(new ApiError("SUPPLIER_NOT_FOUND", "The supplier was not found.")) : Ok(ToSupplier(supplier));
    }

    [HttpPut("suppliers/{id:guid?}")]
    public async Task<IActionResult> UpsertSupplier(Guid? id, [FromQuery] Guid organizationId, [FromBody] SupplierUpsertRequest request, CancellationToken cancellationToken)
    {
        var session = CurrentSession();
        if (session is null) return Unauthorized(new ApiError("SESSION_INVALID", "Your session is no longer valid."));
        if (!await CanAsync(session, organizationId, PermissionKeys.ManageIntegration, cancellationToken)) return Forbid();
        if (string.IsNullOrWhiteSpace(request.EntityCode)) request.EntityCode = "DEFAULT";
        var supplier = id is null
            ? null
            : await db.Suppliers.Include(item => item.Aliases).SingleOrDefaultAsync(item => item.Id == id && item.OrganizationId == organizationId, cancellationToken);
        if (id is not null && supplier is null) return NotFound(new ApiError("SUPPLIER_NOT_FOUND", "The supplier was not found."));
        var duplicate = await db.Suppliers.AnyAsync(item => item.OrganizationId == organizationId && item.EntityCode == request.EntityCode &&
            item.SupplierCode == request.SupplierCode && item.Id != id, cancellationToken);
        if (duplicate) return Conflict(new ApiError("SUPPLIER_ALREADY_EXISTS", "A supplier with this code already exists in this entity."));
        supplier ??= new Supplier
        {
            Id = Guid.NewGuid(), OrganizationId = organizationId, CreatedAt = DateTime.UtcNow,
            SourceSystem = "SILA", Status = StatusKind.ACTIVE,
            SupplierCode = request.SupplierCode.Trim(), Name = request.Name.Trim(), NormalizedName = Normalize(request.Name)
        };
        supplier.EntityCode = request.EntityCode.Trim();
        supplier.SupplierCode = request.SupplierCode.Trim();
        supplier.Name = request.Name.Trim();
        supplier.NormalizedName = Normalize(request.Name);
        supplier.SearchName = request.SearchName?.Trim();
        supplier.BusinessPartnerId = request.BusinessPartnerId?.Trim();
        supplier.LegalName = request.LegalName?.Trim();
        supplier.TaxNumber = request.TaxNumber?.Trim();
        supplier.Trn = request.Trn?.Trim();
        supplier.Email = request.Email?.Trim();
        supplier.Phone = request.Phone?.Trim();
        supplier.Country = request.Country?.Trim();
        supplier.City = request.City?.Trim();
        supplier.PostalCode = request.PostalCode?.Trim();
        supplier.Street = request.Street?.Trim();
        supplier.Currency = request.Currency?.Trim().ToUpperInvariant();
        supplier.IsBlocked = request.IsBlocked;
        supplier.IsDeleted = request.IsDeleted;
        supplier.Status = request.Status;
        supplier.UpdatedAt = DateTime.UtcNow;
        if (id is null) db.Suppliers.Add(supplier);
        var aliases = request.Aliases.Where(item => !string.IsNullOrWhiteSpace(item)).Select(item => item.Trim()).Distinct(StringComparer.OrdinalIgnoreCase).ToList();
        foreach (var alias in aliases)
        {
            var normalized = Normalize(alias);
            if (!await db.SupplierAliases.AnyAsync(item => item.OrganizationId == organizationId && item.EntityCode == supplier.EntityCode && item.NormalizedAlias == normalized && item.SupplierId != supplier.Id, cancellationToken))
                supplier.Aliases.Add(new SupplierAlias { Id = Guid.NewGuid(), OrganizationId = organizationId, SupplierId = supplier.Id, EntityCode = supplier.EntityCode, Alias = alias, NormalizedAlias = normalized, SourceSystem = "SILA", IsConfirmed = true, CreatedAt = DateTime.UtcNow });
        }
        await db.SaveChangesAsync(cancellationToken);
        return Ok(ToSupplier(supplier));
    }

    [HttpGet("import/template")]
    public async Task<IActionResult> ImportTemplate([FromQuery] Guid organizationId, [FromQuery] IntegrationImportKind kind = IntegrationImportKind.SUPPLIERS, CancellationToken cancellationToken = default)
    {
        var session = CurrentSession();
        if (session is null) return Unauthorized(new ApiError("SESSION_INVALID", "Your session is no longer valid."));
        if (!await CanAsync(session, organizationId, PermissionKeys.ViewPurchaseOrder, cancellationToken)) return Forbid();
        var bytes = await integrations.TemplateAsync(kind, cancellationToken);
        return File(bytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", $"{kind.ToString().ToLowerInvariant()}-template.xlsx");
    }

    [HttpGet("export")]
    public async Task<IActionResult> Export(
        [FromQuery] Guid organizationId,
        [FromQuery] string? entityCode,
        [FromQuery] IntegrationImportKind kind = IntegrationImportKind.SUPPLIERS,
        CancellationToken cancellationToken = default)
    {
        var session = CurrentSession();
        if (session is null) return Unauthorized(new ApiError("SESSION_INVALID", "Your session is no longer valid."));
        if (!await CanAsync(session, organizationId, PermissionKeys.ViewPurchaseOrder, cancellationToken)) return Forbid();
        var config = await integrations.GetOrCreateExcelConfigurationAsync(organizationId, string.IsNullOrWhiteSpace(entityCode) ? "ALL" : entityCode, kind, cancellationToken);
        var bytes = await integrations.ExportAsync(organizationId, config.Id, kind, null, null, null, false, cancellationToken);
        return File(bytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", $"{kind.ToString().ToLowerInvariant()}-export.xlsx");
    }

    [HttpPost("import/preview")]
    [RequestSizeLimit(10 * 1024 * 1024)]
    public async Task<IActionResult> PreviewImport(
        [FromQuery] Guid organizationId,
        [FromQuery] string? entityCode,
        [FromQuery] IntegrationImportKind kind,
        [FromForm] IFormFile? file,
        CancellationToken cancellationToken)
    {
        var session = CurrentSession();
        if (session is null) return Unauthorized(new ApiError("SESSION_INVALID", "Your session is no longer valid."));
        if (!await CanAsync(session, organizationId, PermissionKeys.ViewPurchaseOrder, cancellationToken)) return Forbid();
        if (file is null || file.Length == 0) return BadRequest(new ApiError("IMPORT_FILE_REQUIRED", "Choose an .xlsx file to preview."));
        try
        {
            await using var stream = file.OpenReadStream();
            return Ok(await integrations.PreviewMasterImportAsync(organizationId, string.IsNullOrWhiteSpace(entityCode) ? "ALL" : entityCode, kind, stream, file.FileName, cancellationToken));
        }
        catch (IntegrationException exception)
        {
            return StatusCode(exception.Status, new ApiError(exception.Code, exception.Message));
        }
    }

    [HttpPost("import")]
    public async Task<IActionResult> CommitImport(
        [FromQuery] Guid organizationId,
        [FromQuery] string? entityCode,
        [FromBody] IntegrationImportCommitInput input,
        CancellationToken cancellationToken)
    {
        var session = CurrentSession();
        if (session is null) return Unauthorized(new ApiError("SESSION_INVALID", "Your session is no longer valid."));
        if (!await CanAsync(session, organizationId, PermissionKeys.ManageIntegration, cancellationToken) &&
            !await CanAsync(session, organizationId, "CREATE_SUPPLIER", cancellationToken))
            return Forbid();
        try
        {
            return Ok(await integrations.CommitMasterImportAsync(organizationId, string.IsNullOrWhiteSpace(entityCode) ? "ALL" : entityCode, input, cancellationToken));
        }
        catch (IntegrationException exception)
        {
            return StatusCode(exception.Status, new ApiError(exception.Code, exception.Message));
        }
    }

    [HttpGet("company-codes")]
    public Task<IActionResult> CompanyCodes([FromQuery] Guid organizationId, [FromQuery] string? query, [FromQuery] string? status, CancellationToken cancellationToken) =>
        QueryAsync(organizationId, () => masters.ListCompanyCodesAsync(organizationId, query, status, cancellationToken), cancellationToken);

    [HttpPut("company-codes/{id:guid?}")]
    public Task<IActionResult> UpsertCompanyCode(Guid? id, [FromQuery] Guid organizationId, [FromBody] CompanyCodeUpsertRequest request, CancellationToken cancellationToken) =>
        MutateAsync(organizationId, () => masters.UpsertCompanyCodeAsync(organizationId, id, request, cancellationToken), cancellationToken);

    [HttpPost("company-codes/{id:guid}/suspend")]
    public Task<IActionResult> SuspendCompanyCode(Guid id, [FromQuery] Guid organizationId, CancellationToken cancellationToken) =>
        MutateAsync(organizationId, async () => { await masters.SetStatusAsync(organizationId, OperationalMasterKind.COMPANY_CODES, id, StatusKind.INACTIVE, cancellationToken); return (object)"OK"; }, cancellationToken);

    [HttpPost("company-codes/{id:guid}/activate")]
    public Task<IActionResult> ActivateCompanyCode(Guid id, [FromQuery] Guid organizationId, CancellationToken cancellationToken) =>
        MutateAsync(organizationId, async () => { await masters.SetStatusAsync(organizationId, OperationalMasterKind.COMPANY_CODES, id, StatusKind.ACTIVE, cancellationToken); return (object)"OK"; }, cancellationToken);

    [HttpDelete("company-codes/{id:guid}")]
    public Task<IActionResult> DeleteCompanyCode(Guid id, [FromQuery] Guid organizationId, CancellationToken cancellationToken) =>
        MutateAsync(organizationId, async () => { await masters.DeleteAsync(organizationId, OperationalMasterKind.COMPANY_CODES, id, cancellationToken); return (object)"OK"; }, cancellationToken);

    [HttpGet("properties")]
    public Task<IActionResult> Properties([FromQuery] Guid organizationId, [FromQuery] string? query, [FromQuery] string? status, CancellationToken cancellationToken) =>
        QueryAsync(organizationId, () => masters.ListPropertiesAsync(organizationId, query, status, cancellationToken), cancellationToken);

    [HttpPut("properties/{id:guid?}")]
    public Task<IActionResult> UpsertProperty(Guid? id, [FromQuery] Guid organizationId, [FromBody] PropertyUpsertRequest request, CancellationToken cancellationToken) =>
        MutateAsync(organizationId, () => masters.UpsertPropertyAsync(organizationId, id, request, cancellationToken), cancellationToken);

    [HttpPost("properties/{id:guid}/suspend")]
    public Task<IActionResult> SuspendProperty(Guid id, [FromQuery] Guid organizationId, CancellationToken cancellationToken) =>
        MutateAsync(organizationId, async () => { await masters.SetStatusAsync(organizationId, OperationalMasterKind.PROPERTIES, id, StatusKind.INACTIVE, cancellationToken); return (object)"OK"; }, cancellationToken);

    [HttpPost("properties/{id:guid}/activate")]
    public Task<IActionResult> ActivateProperty(Guid id, [FromQuery] Guid organizationId, CancellationToken cancellationToken) =>
        MutateAsync(organizationId, async () => { await masters.SetStatusAsync(organizationId, OperationalMasterKind.PROPERTIES, id, StatusKind.ACTIVE, cancellationToken); return (object)"OK"; }, cancellationToken);

    [HttpDelete("properties/{id:guid}")]
    public Task<IActionResult> DeleteProperty(Guid id, [FromQuery] Guid organizationId, CancellationToken cancellationToken) =>
        MutateAsync(organizationId, async () => { await masters.DeleteAsync(organizationId, OperationalMasterKind.PROPERTIES, id, cancellationToken); return (object)"OK"; }, cancellationToken);

    [HttpGet("plants")]
    public IActionResult Plants() => StatusCode(410, new ApiError("MASTER_REMOVED", "Plant master was removed. Use Location Master."));

    [HttpPut("plants/{id:guid?}")]
    public IActionResult UpsertPlant() => StatusCode(410, new ApiError("MASTER_REMOVED", "Plant master was removed. Use Location Master."));

    [HttpPost("plants/{id:guid}/suspend")]
    public IActionResult SuspendPlant() => StatusCode(410, new ApiError("MASTER_REMOVED", "Plant master was removed. Use Location Master."));

    [HttpPost("plants/{id:guid}/activate")]
    public IActionResult ActivatePlant() => StatusCode(410, new ApiError("MASTER_REMOVED", "Plant master was removed. Use Location Master."));

    [HttpDelete("plants/{id:guid}")]
    public IActionResult DeletePlant() => StatusCode(410, new ApiError("MASTER_REMOVED", "Plant master was removed. Use Location Master."));

    [HttpGet("storage-locations")]
    public IActionResult StorageLocations() => StatusCode(410, new ApiError("MASTER_REMOVED", "Storage Location master was removed. Use Location Master."));

    [HttpPut("storage-locations/{id:guid?}")]
    public IActionResult UpsertStorageLocation() => StatusCode(410, new ApiError("MASTER_REMOVED", "Storage Location master was removed. Use Location Master."));

    [HttpPost("storage-locations/{id:guid}/suspend")]
    public IActionResult SuspendStorageLocation() => StatusCode(410, new ApiError("MASTER_REMOVED", "Storage Location master was removed. Use Location Master."));

    [HttpPost("storage-locations/{id:guid}/activate")]
    public IActionResult ActivateStorageLocation() => StatusCode(410, new ApiError("MASTER_REMOVED", "Storage Location master was removed. Use Location Master."));

    [HttpDelete("storage-locations/{id:guid}")]
    public IActionResult DeleteStorageLocation() => StatusCode(410, new ApiError("MASTER_REMOVED", "Storage Location master was removed. Use Location Master."));

    [HttpGet("records/import/template")]
    public async Task<IActionResult> RecordTemplate([FromQuery] Guid organizationId, [FromQuery] OperationalMasterKind kind, CancellationToken cancellationToken)
    {
        var session = CurrentSession();
        if (session is null) return Unauthorized(new ApiError("SESSION_INVALID", "Your session is no longer valid."));
        if (!await CanAsync(session, organizationId, PermissionKeys.ViewPurchaseOrder, cancellationToken)) return Forbid();
        try
        {
            return File(masters.Template(kind), "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", $"{kind.ToString().ToLowerInvariant()}-template.xlsx");
        }
        catch (IntegrationException exception)
        {
            return StatusCode(exception.Status, new ApiError(exception.Code, exception.Message));
        }
    }

    [HttpGet("records/export")]
    public async Task<IActionResult> RecordExport([FromQuery] Guid organizationId, [FromQuery] OperationalMasterKind kind, CancellationToken cancellationToken)
    {
        var session = CurrentSession();
        if (session is null) return Unauthorized(new ApiError("SESSION_INVALID", "Your session is no longer valid."));
        if (!await CanAsync(session, organizationId, PermissionKeys.ViewPurchaseOrder, cancellationToken)) return Forbid();
        try
        {
            var bytes = await masters.ExportAsync(organizationId, kind, cancellationToken);
            return File(bytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", $"{kind.ToString().ToLowerInvariant()}-export.xlsx");
        }
        catch (IntegrationException exception)
        {
            return StatusCode(exception.Status, new ApiError(exception.Code, exception.Message));
        }
    }

    [HttpPost("records/import/preview")]
    [RequestSizeLimit(10 * 1024 * 1024)]
    public async Task<IActionResult> RecordPreview([FromQuery] Guid organizationId, [FromQuery] OperationalMasterKind kind, [FromForm] IFormFile? file, CancellationToken cancellationToken)
    {
        var session = CurrentSession();
        if (session is null) return Unauthorized(new ApiError("SESSION_INVALID", "Your session is no longer valid."));
        if (!await CanAsync(session, organizationId, PermissionKeys.ViewPurchaseOrder, cancellationToken)) return Forbid();
        if (file is null || file.Length == 0) return BadRequest(new ApiError("IMPORT_FILE_REQUIRED", "Choose an .xlsx file to preview."));
        try
        {
            await using var stream = file.OpenReadStream();
            return Ok(await masters.PreviewAsync(organizationId, kind, stream, file.FileName, cancellationToken));
        }
        catch (IntegrationException exception)
        {
            return StatusCode(exception.Status, new ApiError(exception.Code, exception.Message));
        }
    }

    [HttpPost("records/import")]
    public async Task<IActionResult> RecordCommit([FromQuery] Guid organizationId, [FromQuery] OperationalMasterKind kind, [FromBody] OperationalMasterCommitInput input, CancellationToken cancellationToken)
    {
        var session = CurrentSession();
        if (session is null) return Unauthorized(new ApiError("SESSION_INVALID", "Your session is no longer valid."));
        if (!await CanAsync(session, organizationId, PermissionKeys.ManageIntegration, cancellationToken) &&
            !await CanAsync(session, organizationId, "CREATE_SUPPLIER", cancellationToken) &&
            !await CanAsync(session, organizationId, PermissionKeys.OrganizationManage, cancellationToken) &&
            !await CanAsync(session, organizationId, PermissionKeys.ViewPurchaseOrder, cancellationToken))
            return StatusCode(403, new ApiError("FORBIDDEN", "You do not have permission to change this master data."));
        try
        {
            return Ok(await masters.CommitAsync(organizationId, kind, input.Rows, cancellationToken));
        }
        catch (IntegrationException exception)
        {
            return StatusCode(exception.Status, new ApiError(exception.Code, exception.Message));
        }
    }

    private async Task<IActionResult> QueryAsync<T>(Guid organizationId, Func<Task<T>> work, CancellationToken cancellationToken)
    {
        var session = CurrentSession();
        if (session is null) return Unauthorized(new ApiError("SESSION_INVALID", "Your session is no longer valid."));
        if (!await CanAsync(session, organizationId, PermissionKeys.ViewPurchaseOrder, cancellationToken)) return Forbid();
        return Ok(await work());
    }

    private async Task<IActionResult> MutateAsync<T>(Guid organizationId, Func<Task<T>> work, CancellationToken cancellationToken)
    {
        var session = CurrentSession();
        if (session is null) return Unauthorized(new ApiError("SESSION_INVALID", "Your session is no longer valid."));
        if (!await CanAsync(session, organizationId, PermissionKeys.ManageIntegration, cancellationToken) &&
            !await CanAsync(session, organizationId, "CREATE_SUPPLIER", cancellationToken) &&
            !await CanAsync(session, organizationId, PermissionKeys.OrganizationManage, cancellationToken) &&
            !await CanAsync(session, organizationId, PermissionKeys.ViewPurchaseOrder, cancellationToken))
            return StatusCode(403, new ApiError("FORBIDDEN", "You do not have permission to change this master data."));
        try
        {
            var result = await work();
            return result is string ? NoContent() : Ok(result);
        }
        catch (IntegrationException exception)
        {
            return StatusCode(exception.Status, new ApiError(exception.Code, exception.Message));
        }
    }

    private async Task<bool> CanAsync(Session session, Guid organizationId, string permission, CancellationToken cancellationToken) =>
        await db.Organizations.AnyAsync(item => item.Id == organizationId && item.Status == StatusKind.ACTIVE, cancellationToken) &&
        await access.HasPermissionAsync(session, permission, organizationId, null, cancellationToken);

    private Session? CurrentSession() => HttpContext.Items[SessionContext.ItemKey] as Session;
    private static string Normalize(string value) => System.Text.RegularExpressions.Regex.Replace(value.Trim().ToUpperInvariant(), @"[^A-Z0-9]+", " ").Trim();
    private static SupplierResponse ToSupplier(Supplier item) => new(
        item.Id, item.SupplierCode, item.Name, item.LegalName, item.TaxNumber, item.Email, item.Phone, item.EntityCode,
        item.Country, item.Currency, item.IsBlocked, item.IsDeleted, item.Status,
        item.Aliases.OrderBy(alias => alias.Alias).Select(alias => alias.Alias).ToList(), item.LastSyncedAt, item.UpdatedAt,
        item.SearchName, item.BusinessPartnerId, item.Trn, item.City, item.PostalCode, item.Street,
        item.IsActive, item.SourceSystem, item.SourceLastChangedAt);
}