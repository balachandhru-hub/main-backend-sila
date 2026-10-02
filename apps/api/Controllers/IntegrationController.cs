using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SilaMe.Api.Auth;
using SilaMe.Api.Data;
using SilaMe.Api.DTOs;
using SilaMe.Api.Models;
using SilaMe.Api.Services;

namespace SilaMe.Api.Controllers;

[ApiController]
[Route("api/v1/integrations")]
public sealed class IntegrationController(SilaMeDbContext db, IntegrationService integrations, IntegrationDesignerService designer, IntegrationConnectionTestService connections, IntegrationRouteService routes, IAccessService access) : ControllerBase
{
    [HttpGet("target-fields")]
    public IActionResult TargetFields() => Ok(IntegrationTargetFieldRegistry.Fields);

    [HttpGet("designer/catalog")]
    public IActionResult DesignerCatalog() => Ok(designer.Catalog());

    [HttpGet("designer/sample/five-s4-post-grn")]
    public IActionResult FiveSample() => Ok(IntegrationDesignerCatalog.FiveS4PostGrnSample());

    [HttpGet]
    public async Task<IActionResult> List([FromQuery] Guid organizationId, CancellationToken cancellationToken)
    {
        var session = CurrentSession();
        if (session is null) return Unauthorized();
        if (!await CanAsync(session, PermissionKeys.ViewIntegration, organizationId, cancellationToken)) return Forbid();
        return Ok(await integrations.ListAsync(organizationId, cancellationToken, designer.CurrentEnvironment()));
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> Get(Guid id, [FromQuery] Guid organizationId, CancellationToken cancellationToken)
    {
        var session = CurrentSession();
        if (session is null) return Unauthorized();
        if (!await CanAsync(session, PermissionKeys.ViewIntegration, organizationId, cancellationToken)) return Forbid();
        var response = await integrations.GetAsync(organizationId, id, cancellationToken);
        return response is null ? NotFound(new { code = "INTEGRATION_NOT_FOUND", message = "The integration configuration was not found." }) : Ok(response);
    }

    [HttpPost("test-connection")]
    public async Task<IActionResult> TestConnection([FromQuery] Guid organizationId, [FromQuery] Guid? configurationId, [FromBody] IntegrationDesignerDraft draft, CancellationToken cancellationToken)
    {
        var session = CurrentSession();
        if (session is null) return Unauthorized();
        if (!await CanAsync(session, PermissionKeys.TestIntegration, organizationId, cancellationToken)) return Forbid();
        return await Execute(() => connections.TestConnectionAsync(organizationId, configurationId, session.UserId, draft, cancellationToken));
    }

    [HttpPost("designer/test")]
    public async Task<IActionResult> TestDraft([FromQuery] Guid organizationId, [FromQuery] string testType, [FromQuery] Guid? configurationId, [FromBody] IntegrationDesignerDraft draft, CancellationToken cancellationToken)
    {
        var session = CurrentSession();
        if (session is null) return Unauthorized();
        if (!await CanAsync(session, PermissionKeys.TestIntegration, organizationId, cancellationToken)) return Forbid();
        return await Execute(() => designer.TestDraftAsync(organizationId, draft, testType ?? "AUTH", configurationId, cancellationToken));
    }

    [HttpPost("designer/preview")]
    public async Task<IActionResult> Preview([FromQuery] Guid organizationId, [FromBody] IntegrationDesignerDraft draft, CancellationToken cancellationToken)
    {
        var session = CurrentSession();
        if (session is null) return Unauthorized();
        if (!await CanAsync(session, PermissionKeys.ViewIntegration, organizationId, cancellationToken)) return Forbid();
        try { return Ok(designer.Preview(draft)); }
        catch (IntegrationException exception) { return StatusCode(exception.Status, new { code = exception.Code, message = exception.Message }); }
    }

    [HttpPost("designer/draft")]
    public async Task<IActionResult> SaveDraft([FromQuery] Guid organizationId, [FromQuery] Guid? id, [FromBody] IntegrationDesignerDraft draft, CancellationToken cancellationToken)
    {
        var session = CurrentSession();
        if (session is null) return Unauthorized();
        if (!await CanAsync(session, PermissionKeys.ManageIntegration, organizationId, cancellationToken)) return Forbid();
        return await Execute(() => designer.SaveDraftAsync(organizationId, id, draft, cancellationToken));
    }

    [HttpPost("designer")]
    public async Task<IActionResult> SaveDesigner([FromQuery] Guid organizationId, [FromQuery] Guid? id, [FromBody] IntegrationDesignerDraft draft, CancellationToken cancellationToken)
    {
        var session = CurrentSession();
        if (session is null) return Unauthorized();
        if (!await CanAsync(session, PermissionKeys.ManageIntegration, organizationId, cancellationToken)) return Forbid();
        try
        {
            await connections.EnsureValidProofAsync(organizationId, session.UserId, draft, id, cancellationToken);
        }
        catch (IntegrationException exception) { return StatusCode(exception.Status, new { code = exception.Code, message = exception.Message }); }
        return await Execute(() => designer.SaveValidatedAsync(organizationId, id, draft, session.UserId.ToString(), cancellationToken));
    }

    [HttpPost("{id:guid}/designer/activate")]
    public async Task<IActionResult> ActivateDesigner(Guid id, [FromQuery] Guid organizationId, CancellationToken cancellationToken)
    {
        var session = CurrentSession();
        if (session is null) return Unauthorized();
        if (!await CanAsync(session, PermissionKeys.ManageIntegration, organizationId, cancellationToken)) return Forbid();
        return await Execute(() => designer.ActivateAsync(organizationId, id, true, cancellationToken));
    }

    [HttpPost("{id:guid}/designer/disable")]
    public async Task<IActionResult> DisableDesigner(Guid id, [FromQuery] Guid organizationId, CancellationToken cancellationToken)
    {
        var session = CurrentSession();
        if (session is null) return Unauthorized();
        if (!await CanAsync(session, PermissionKeys.ManageIntegration, organizationId, cancellationToken)) return Forbid();
        return await Execute(() => designer.ActivateAsync(organizationId, id, false, cancellationToken));
    }

    [HttpPost("{id:guid}/duplicate")]
    public async Task<IActionResult> Duplicate(Guid id, [FromQuery] Guid organizationId, CancellationToken cancellationToken)
    {
        var session = CurrentSession();
        if (session is null) return Unauthorized();
        if (!await CanAsync(session, PermissionKeys.ManageIntegration, organizationId, cancellationToken)) return Forbid();
        return await Execute(() => designer.DuplicateAsync(organizationId, id, cancellationToken));
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromQuery] Guid organizationId, [FromBody] IntegrationConfigurationInput input, CancellationToken cancellationToken)
    {
        var session = CurrentSession();
        if (session is null) return Unauthorized();
        if (!await CanAsync(session, PermissionKeys.ManageIntegration, organizationId, cancellationToken)) return Forbid();
        return await Execute(() => integrations.SaveAsync(organizationId, null, input, cancellationToken));
    }

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(Guid id, [FromQuery] Guid organizationId, [FromBody] IntegrationConfigurationInput input, CancellationToken cancellationToken)
    {
        var session = CurrentSession();
        if (session is null) return Unauthorized();
        if (!await CanAsync(session, PermissionKeys.ManageIntegration, organizationId, cancellationToken)) return Forbid();
        return await Execute(() => integrations.SaveAsync(organizationId, id, input, cancellationToken));
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id, [FromQuery] Guid organizationId, CancellationToken cancellationToken)
    {
        var session = CurrentSession();
        if (session is null) return Unauthorized();
        if (!await CanManageAsync(session, organizationId, cancellationToken)) return Forbid();
        return await Execute(() => integrations.DeleteAsync(organizationId, id, cancellationToken));
    }

    [HttpGet("routes")]
    public async Task<IActionResult> ListRoutes([FromQuery] Guid organizationId, CancellationToken cancellationToken)
    {
        var session = CurrentSession();
        if (session is null) return Unauthorized();
        if (!await CanViewAsync(session, organizationId, cancellationToken)) return Forbid();
        return Ok(await routes.ListAsync(organizationId, cancellationToken));
    }

    [HttpGet("routes/configurations")]
    public async Task<IActionResult> MatchingRouteConfigurations(
        [FromQuery] Guid organizationId,
        [FromQuery] IntegrationProcessType processType,
        [FromQuery] IntegrationSystemKind systemKind,
        CancellationToken cancellationToken)
    {
        var session = CurrentSession();
        if (session is null) return Unauthorized();
        if (!await CanViewAsync(session, organizationId, cancellationToken)) return Forbid();
        return Ok(await routes.MatchingConfigurationsAsync(organizationId, processType, systemKind, cancellationToken));
    }

    [HttpPost("routes")]
    public async Task<IActionResult> CreateRoute([FromQuery] Guid organizationId, [FromBody] IntegrationRouteInput input, CancellationToken cancellationToken)
    {
        var session = CurrentSession();
        if (session is null) return Unauthorized();
        if (!await CanManageAsync(session, organizationId, cancellationToken)) return Forbid();
        return await Execute(() => routes.SaveAsync(organizationId, null, input, session.UserId, cancellationToken));
    }

    [HttpPut("routes/{id:guid}")]
    public async Task<IActionResult> UpdateRoute(Guid id, [FromQuery] Guid organizationId, [FromBody] IntegrationRouteInput input, CancellationToken cancellationToken)
    {
        var session = CurrentSession();
        if (session is null) return Unauthorized();
        if (!await CanManageAsync(session, organizationId, cancellationToken)) return Forbid();
        return await Execute(() => routes.SaveAsync(organizationId, id, input, session.UserId, cancellationToken));
    }

    [HttpDelete("routes/{id:guid}")]
    public async Task<IActionResult> DeleteRoute(Guid id, [FromQuery] Guid organizationId, CancellationToken cancellationToken)
    {
        var session = CurrentSession();
        if (session is null) return Unauthorized();
        if (!await CanManageAsync(session, organizationId, cancellationToken)) return Forbid();
        return await Execute(() => routes.DeleteAsync(organizationId, id, session.UserId, cancellationToken));
    }

    [HttpPost("{id:guid}/test")]
    public async Task<IActionResult> Test(Guid id, [FromQuery] Guid organizationId, CancellationToken cancellationToken)
    {
        var session = CurrentSession();
        if (session is null) return Unauthorized();
        if (!await CanAsync(session, PermissionKeys.TestIntegration, organizationId, cancellationToken)) return Forbid();
        return await Execute(() => integrations.TestAsync(organizationId, id, cancellationToken));
    }

    [HttpPost("{id:guid}/schema")]
    public async Task<IActionResult> Schema(Guid id, [FromQuery] Guid organizationId, CancellationToken cancellationToken)
    {
        var session = CurrentSession();
        if (session is null) return Unauthorized();
        if (!await CanAsync(session, PermissionKeys.TestIntegration, organizationId, cancellationToken)) return Forbid();
        return await Execute(() => integrations.DiscoverSchemaAsync(organizationId, id, cancellationToken));
    }

    [HttpGet("{id:guid}/schema")]
    public async Task<IActionResult> LatestSchema(Guid id, [FromQuery] Guid organizationId, CancellationToken cancellationToken)
    {
        var session = CurrentSession();
        if (session is null) return Unauthorized();
        if (!await CanAsync(session, PermissionKeys.ViewIntegration, organizationId, cancellationToken)) return Forbid();
        var response = await integrations.LatestSchemaAsync(organizationId, id, cancellationToken);
        return response is null ? NotFound() : Ok(response);
    }

    [HttpGet("{id:guid}/mappings")]
    public async Task<IActionResult> Mappings(Guid id, [FromQuery] Guid organizationId, CancellationToken cancellationToken)
    {
        var session = CurrentSession();
        if (session is null) return Unauthorized();
        if (!await CanAsync(session, PermissionKeys.ViewIntegration, organizationId, cancellationToken)) return Forbid();
        return await Execute(() => integrations.GetMappingsAsync(organizationId, id, cancellationToken));
    }

    [HttpPut("{id:guid}/mappings")]
    public async Task<IActionResult> SaveMappings(Guid id, [FromQuery] Guid organizationId, [FromBody] List<FieldMappingInput> input, CancellationToken cancellationToken)
    {
        var session = CurrentSession();
        if (session is null) return Unauthorized();
        if (!await CanAsync(session, PermissionKeys.ManageIntegrationMapping, organizationId, cancellationToken)) return Forbid();
        return await Execute(() => integrations.SaveMappingsAsync(organizationId, id, input, cancellationToken));
    }

    [HttpPost("{id:guid}/activate")]
    public async Task<IActionResult> Activate(Guid id, [FromQuery] Guid organizationId, CancellationToken cancellationToken)
    {
        var session = CurrentSession();
        if (session is null) return Unauthorized();
        if (!await CanAsync(session, PermissionKeys.ManageIntegration, organizationId, cancellationToken)) return Forbid();
        return await Execute(() => integrations.ActivateAsync(organizationId, id, true, cancellationToken));
    }

    [HttpPost("{id:guid}/deactivate")]
    public async Task<IActionResult> Deactivate(Guid id, [FromQuery] Guid organizationId, CancellationToken cancellationToken)
    {
        var session = CurrentSession();
        if (session is null) return Unauthorized();
        if (!await CanAsync(session, PermissionKeys.ManageIntegration, organizationId, cancellationToken)) return Forbid();
        return await Execute(() => integrations.ActivateAsync(organizationId, id, false, cancellationToken));
    }

    [HttpPost("{id:guid}/pull")]
    public async Task<IActionResult> Pull(Guid id, [FromQuery] Guid organizationId, [FromBody] IntegrationExecutionRequest? request, CancellationToken cancellationToken)
    {
        var session = CurrentSession();
        if (session is null) return Unauthorized();
        if (!await CanAsync(session, PermissionKeys.RunIntegration, organizationId, cancellationToken)) return Forbid();
        return await Execute(async () =>
        {
            var result = await integrations.RunAsync(organizationId, id, IntegrationExecutionTrigger.MANUAL, request?.FullSync ?? false, cancellationToken);
            return result ?? throw new IntegrationException("INTEGRATION_BUSY", "This integration is already running.", 409);
        });
    }

    [HttpGet("executions")]
    public async Task<IActionResult> Executions([FromQuery] Guid organizationId, [FromQuery] Guid? configurationId, CancellationToken cancellationToken)
    {
        var session = CurrentSession();
        if (session is null) return Unauthorized();
        if (!await CanAsync(session, PermissionKeys.ViewIntegrationLogs, organizationId, cancellationToken)) return Forbid();
        return Ok(await integrations.ExecutionsAsync(organizationId, configurationId, cancellationToken));
    }

    [HttpGet("{id:guid}/data-update")]
    public async Task<IActionResult> DataUpdate(
        Guid id,
        [FromQuery] Guid organizationId,
        [FromQuery] IntegrationImportKind kind = IntegrationImportKind.PURCHASE_ORDERS,
        [FromQuery] string? search = null,
        [FromQuery] string? status = null,
        [FromQuery] string? sortBy = null,
        [FromQuery] bool descending = false,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 25,
        CancellationToken cancellationToken = default)
    {
        var session = CurrentSession();
        if (session is null) return Unauthorized();
        if (!await CanAsync(session, PermissionKeys.ViewIntegrationData, organizationId, cancellationToken)) return Forbid();
        var result = await integrations.DataUpdateAsync(organizationId, id, kind, search, status, sortBy, descending, page, pageSize, cancellationToken);
        return result is null ? NotFound() : Ok(result);
    }

    [HttpGet("{id:guid}/data-update/template")]
    public async Task<IActionResult> Template(Guid id, [FromQuery] Guid organizationId, [FromQuery] IntegrationImportKind kind = IntegrationImportKind.PURCHASE_ORDERS, CancellationToken cancellationToken = default)
    {
        var session = CurrentSession();
        if (session is null) return Unauthorized();
        if (!await CanAsync(session, PermissionKeys.ViewIntegrationData, organizationId, cancellationToken)) return Forbid();
        var bytes = await integrations.TemplateAsync(kind, cancellationToken);
        return File(bytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", $"{kind.ToString().ToLowerInvariant()}-template.xlsx");
    }

    [HttpGet("data-update/template")]
    public async Task<IActionResult> StandaloneTemplate([FromQuery] Guid organizationId, [FromQuery] IntegrationImportKind kind = IntegrationImportKind.PURCHASE_ORDERS, CancellationToken cancellationToken = default)
    {
        var session = CurrentSession();
        if (session is null) return Unauthorized();
        if (!await CanAsync(session, PermissionKeys.ViewIntegrationData, organizationId, cancellationToken)) return Forbid();
        var bytes = await integrations.TemplateAsync(kind, cancellationToken);
        return File(bytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", $"{kind.ToString().ToLowerInvariant()}-template.xlsx");
    }

    [HttpGet("{id:guid}/data-update/export")]
    public async Task<IActionResult> Export(
        Guid id,
        [FromQuery] Guid organizationId,
        [FromQuery] IntegrationImportKind kind = IntegrationImportKind.PURCHASE_ORDERS,
        [FromQuery] string? search = null,
        [FromQuery] string? status = null,
        [FromQuery] string? sortBy = null,
        [FromQuery] bool descending = false,
        CancellationToken cancellationToken = default)
    {
        var session = CurrentSession();
        if (session is null) return Unauthorized();
        if (!await CanAsync(session, PermissionKeys.ViewIntegrationData, organizationId, cancellationToken)) return Forbid();
        var bytes = await integrations.ExportAsync(organizationId, id, kind, search, status, sortBy, descending, cancellationToken);
        return File(bytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", $"{kind.ToString().ToLowerInvariant()}-export.xlsx");
    }

    [HttpPost("{id:guid}/data-update/import/preview")]
    [RequestSizeLimit(10 * 1024 * 1024)]
    public async Task<IActionResult> PreviewImport(
        Guid id,
        [FromQuery] Guid organizationId,
        [FromQuery] IntegrationImportKind kind,
        [FromForm] IFormFile? file,
        CancellationToken cancellationToken)
    {
        var session = CurrentSession();
        if (session is null) return Unauthorized();
        if (!await CanAsync(session, PermissionKeys.ViewIntegrationData, organizationId, cancellationToken)) return Forbid();
        if (file is null || file.Length == 0) return BadRequest(new { code = "IMPORT_FILE_REQUIRED", message = "Choose an .xlsx or .csv file to preview." });
        try
        {
            await using var stream = file.OpenReadStream();
            return Ok(await integrations.PreviewImportAsync(organizationId, id, kind, stream, file.FileName, cancellationToken));
        }
        catch (IntegrationException exception) { return StatusCode(exception.Status, new { code = exception.Code, message = exception.Message }); }
    }

    [HttpPost("{id:guid}/data-update/import/correction-report")]
    public async Task<IActionResult> CorrectionReport(
        Guid id,
        [FromQuery] Guid organizationId,
        [FromBody] IntegrationImportCorrectionReportInput input,
        CancellationToken cancellationToken)
    {
        var session = CurrentSession();
        if (session is null) return Unauthorized();
        if (!await CanAsync(session, PermissionKeys.ViewIntegrationData, organizationId, cancellationToken)) return Forbid();
        try
        {
            var bytes = await integrations.CorrectionReportAsync(organizationId, id, input, cancellationToken);
            return File(bytes, "text/csv; charset=utf-8", $"{input.Kind.ToString().ToLowerInvariant()}-correction-report.csv");
        }
        catch (IntegrationException exception) { return StatusCode(exception.Status, new { code = exception.Code, message = exception.Message }); }
    }

    [HttpPost("{id:guid}/data-update/import")]
    public async Task<IActionResult> CommitImport(Guid id, [FromQuery] Guid organizationId, [FromBody] IntegrationImportCommitInput input, CancellationToken cancellationToken)
    {
        var session = CurrentSession();
        if (session is null) return Unauthorized();
        if (!await CanAsync(session, PermissionKeys.RunIntegration, organizationId, cancellationToken)) return Forbid();
        return await Execute(() => integrations.CommitImportAsync(organizationId, id, input, cancellationToken));
    }

    private async Task<bool> CanAsync(Session session, string permission, Guid organizationId, CancellationToken cancellationToken) =>
        await db.Organizations.AnyAsync(item => item.Id == organizationId && item.Status == StatusKind.ACTIVE, cancellationToken) &&
        await access.HasPermissionAsync(session, permission, organizationId, null, cancellationToken);

    private async Task<bool> CanViewAsync(Session session, Guid organizationId, CancellationToken cancellationToken) =>
        await CanAsync(session, PermissionKeys.ViewIntegration, organizationId, cancellationToken) ||
        await CanAsync(session, "VIEW_CONFIGURATION", organizationId, cancellationToken);

    private async Task<bool> CanManageAsync(Session session, Guid organizationId, CancellationToken cancellationToken) =>
        await CanAsync(session, PermissionKeys.ManageIntegration, organizationId, cancellationToken) ||
        await CanAsync(session, "EDIT_CONFIGURATION", organizationId, cancellationToken);

    private async Task<IActionResult> Execute<T>(Func<Task<T>> action)
    {
        try { return Ok(await action()); }
        catch (IntegrationException exception) { return StatusCode(exception.Status, new { code = exception.Code, message = exception.Message }); }
    }

    private Session? CurrentSession() => HttpContext.Items[SessionContext.ItemKey] as Session;
}