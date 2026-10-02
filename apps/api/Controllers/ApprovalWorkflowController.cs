using Microsoft.AspNetCore.Mvc;
using SilaMe.Api.Auth;
using SilaMe.Api.DTOs;
using SilaMe.Api.Models;
using SilaMe.Api.Services;

namespace SilaMe.Api.Controllers;

[ApiController]
[Route("api/v1/approval-workflows")]
public sealed class ApprovalWorkflowController(IAccessService access, ApprovalWorkflowService workflows) : ControllerBase
{
    [HttpGet("catalog")]
    public async Task<IActionResult> Catalog([FromQuery] Guid organizationId, CancellationToken cancellationToken)
    {
        if (await Guard(organizationId, write: false, cancellationToken) is { } denied) return denied;
        return Ok(workflows.Catalog(await workflows.RolesAsync(cancellationToken)));
    }

    [HttpGet("scope-options")]
    public async Task<IActionResult> ScopeOptions([FromQuery] Guid organizationId, [FromQuery] string scopeKind, CancellationToken cancellationToken)
    {
        if (await Guard(organizationId, write: false, cancellationToken) is { } denied) return denied;
        if (!Enum.TryParse<ApprovalScopeKind>(scopeKind, true, out var kind))
            return BadRequest(new ApiError("APPROVAL_SCOPE_INVALID", "Use ALL, PROPERTY, COMPANY_CODE, OUTLET, or STORE."));
        return Ok(await workflows.ScopeOptionsAsync(organizationId, kind, cancellationToken));
    }

    [HttpGet]
    public Task<IActionResult> List([FromQuery] Guid organizationId, CancellationToken cancellationToken) =>
        Read(organizationId, () => workflows.ListAsync(organizationId, cancellationToken), cancellationToken);

    [HttpPost]
    public Task<IActionResult> Create([FromQuery] Guid organizationId, [FromBody] ApprovalWorkflowUpsertRequest request, CancellationToken cancellationToken) =>
        Write(organizationId, () => workflows.UpsertAsync(organizationId, null, request, cancellationToken), cancellationToken);

    [HttpPut("{id:guid}")]
    public Task<IActionResult> Update(Guid id, [FromQuery] Guid organizationId, [FromBody] ApprovalWorkflowUpsertRequest request, CancellationToken cancellationToken) =>
        Write(organizationId, () => workflows.UpsertAsync(organizationId, id, request, cancellationToken), cancellationToken);

    [HttpDelete("{id:guid}")]
    public Task<IActionResult> Delete(Guid id, [FromQuery] Guid organizationId, CancellationToken cancellationToken) =>
        Write(organizationId, async () => { await workflows.DeleteAsync(organizationId, id, cancellationToken); return "OK"; }, cancellationToken);

    private Task<IActionResult> Read<T>(Guid organizationId, Func<Task<T>> work, CancellationToken cancellationToken) =>
        Execute(organizationId, false, work, cancellationToken);

    private Task<IActionResult> Write<T>(Guid organizationId, Func<Task<T>> work, CancellationToken cancellationToken) =>
        Execute(organizationId, true, work, cancellationToken);

    private async Task<IActionResult> Execute<T>(Guid organizationId, bool write, Func<Task<T>> work, CancellationToken cancellationToken)
    {
        if (await Guard(organizationId, write, cancellationToken) is { } denied) return denied;
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

    private async Task<IActionResult?> Guard(Guid organizationId, bool write, CancellationToken cancellationToken)
    {
        var session = HttpContext.Items[SessionContext.ItemKey] as Session;
        if (session is null) return Unauthorized(new ApiError("SESSION_INVALID", "Your session is no longer valid."));
        var keys = write
            ? new[] { "EDIT_CONFIGURATION", "MANAGE_RECIPE_WORKFLOW" }
            : new[] { "VIEW_CONFIGURATION", "EDIT_CONFIGURATION", "MANAGE_RECIPE_WORKFLOW", "VIEW_INTEGRATION" };
        foreach (var key in keys)
        {
            if (await access.HasPermissionAsync(session, key, organizationId, null, cancellationToken))
                return null;
        }
        return StatusCode(403, new ApiError("FORBIDDEN", "You do not have permission to manage workflow configuration."));
    }
}
