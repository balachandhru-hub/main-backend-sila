using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SilaMe.Api.Auth;
using SilaMe.Api.Data;
using SilaMe.Api.DTOs;
using SilaMe.Api.Models;
using SilaMe.Api.Services;

namespace SilaMe.Api.Controllers;

[ApiController]
[Route("api/v1/integrations/microsoft")]
public sealed class MicrosoftIntegrationController(
    SilaMeDbContext db,
    IAccessService accessService,
    MicrosoftSharePointService microsoft) : ControllerBase
{
    [HttpGet("readiness")]
    public IActionResult GetReadiness()
    {
        if (CurrentSession() is not { Application: ApplicationKind.CLOUD })
            return Unauthorized(new ApiError("SESSION_INVALID", "Your Cloud session is no longer valid."));
        return Ok(microsoft.GetReadiness());
    }

    [HttpPost("connect")]
    public async Task<IActionResult> Connect(
        [FromBody] MicrosoftConnectRequest request,
        CancellationToken cancellationToken)
    {
        var session = await RequireCloudPermissionAsync(PermissionKeys.ManageDocumentStorage, request.OrganizationId, cancellationToken);
        if (session is null) return PermissionDeniedOrUnauthorized();
        if (!await db.Organizations.AnyAsync(item => item.Id == request.OrganizationId && item.Status == StatusKind.ACTIVE, cancellationToken))
            return NotFound(new ApiError("ORGANIZATION_NOT_FOUND", "The organization was not found."));

        try
        {
            var result = await microsoft.CreateAuthorizationAsync(
                session, request.OrganizationId, request.ReturnUrl, request.Draft, cancellationToken);
            return Ok(result);
        }
        catch (MicrosoftIntegrationException exception)
        {
            return StatusCode(StatusCodes.Status503ServiceUnavailable, new ApiError(exception.Code, exception.Message));
        }
    }

    [HttpGet("callback")]
    public async Task<IActionResult> Callback(
        [FromQuery] string? code,
        [FromQuery] string? state,
        [FromQuery] string? error,
        [FromQuery(Name = "error_description")] string? errorDescription,
        CancellationToken cancellationToken)
    {
        try
        {
            var redirect = await microsoft.HandleCallbackAsync(code, state, error, errorDescription, cancellationToken);
            return Redirect(redirect);
        }
        catch (MicrosoftIntegrationException)
        {
            return Redirect("/admin/users?microsoft=error&reason=configuration_or_token_exchange");
        }
    }

    [HttpGet("drafts/{draftId:guid}")]
    public async Task<IActionResult> GetDraft(Guid draftId, CancellationToken cancellationToken)
    {
        var session = CurrentSession();
        if (session is null || session.Application != ApplicationKind.CLOUD)
            return Unauthorized(new ApiError("SESSION_INVALID", "Your Cloud session is no longer valid."));
        var organizationId = await db.MicrosoftAuthorizationStates
            .Where(item => item.Id == draftId && item.UserId == session.UserId)
            .Select(item => (Guid?)item.OrganizationId)
            .SingleOrDefaultAsync(cancellationToken);
        if (organizationId is null) return NotFound(new ApiError("DRAFT_NOT_FOUND", "The Microsoft connection draft was not found or is not available to this user."));
        if (await RequireCloudPermissionAsync(PermissionKeys.ViewDocumentStorage, organizationId.Value, cancellationToken) is null)
            return PermissionDeniedOrUnauthorized();
        var draft = await microsoft.GetDraftAsync(draftId, session.CustomerUserId(), cancellationToken);
        return draft is null
            ? NotFound(new ApiError("DRAFT_NOT_FOUND", "The Microsoft connection draft was not found or is not available to this user."))
            : Ok(draft);
    }

    [HttpGet("connections/{connectionId:guid}")]
    public async Task<IActionResult> GetConnection(Guid connectionId, CancellationToken cancellationToken)
    {
        var session = await RequireConnectionPermissionAsync(connectionId, PermissionKeys.ViewDocumentStorage, cancellationToken);
        if (session is null) return PermissionDeniedOrUnauthorized();
        try
        {
            return Ok(await microsoft.GetConnectionAsync(connectionId, session.CustomerUserId(), cancellationToken));
        }
        catch (MicrosoftIntegrationException exception)
        {
            return exception.Code == "CONNECTION_NOT_FOUND"
                ? NotFound(new ApiError(exception.Code, exception.Message))
                : StatusCode(StatusCodes.Status403Forbidden, new ApiError(exception.Code, exception.Message));
        }
    }

    [HttpPost("connections/{connectionId:guid}/site")]
    public async Task<IActionResult> ResolveSite(
        Guid connectionId,
        [FromBody] MicrosoftSiteRequest request,
        CancellationToken cancellationToken)
    {
        var session = await RequireConnectionPermissionAsync(connectionId, PermissionKeys.ViewDocumentStorage, cancellationToken);
        if (session is null) return PermissionDeniedOrUnauthorized();
        try
        {
            return Ok(await microsoft.ResolveSiteAsync(connectionId, session.CustomerUserId(), request.SiteUrl, cancellationToken));
        }
        catch (MicrosoftIntegrationException exception)
        {
            return StatusCode(StatusCodes.Status422UnprocessableEntity, new ApiError(exception.Code, exception.Message));
        }
    }

    [HttpPost("connections/{connectionId:guid}/libraries")]
    public async Task<IActionResult> ListLibraries(
        Guid connectionId,
        [FromQuery] string siteId,
        CancellationToken cancellationToken)
    {
        var session = await RequireConnectionPermissionAsync(connectionId, PermissionKeys.ViewDocumentStorage, cancellationToken);
        if (session is null) return PermissionDeniedOrUnauthorized();
        try
        {
            return Ok(await microsoft.ListLibrariesAsync(connectionId, session.CustomerUserId(), siteId, cancellationToken));
        }
        catch (MicrosoftIntegrationException exception)
        {
            return StatusCode(StatusCodes.Status422UnprocessableEntity, new ApiError(exception.Code, exception.Message));
        }
    }

    [HttpPost("connections/{connectionId:guid}/folders")]
    public async Task<IActionResult> ListFolders(
        Guid connectionId,
        [FromBody] MicrosoftFolderListRequest request,
        CancellationToken cancellationToken)
    {
        var session = await RequireConnectionPermissionAsync(connectionId, PermissionKeys.ViewDocumentStorage, cancellationToken);
        if (session is null) return PermissionDeniedOrUnauthorized();
        try
        {
            return Ok(await microsoft.ListFoldersAsync(connectionId, session.CustomerUserId(), request, cancellationToken));
        }
        catch (MicrosoftIntegrationException exception)
        {
            return StatusCode(StatusCodes.Status422UnprocessableEntity, new ApiError(exception.Code, exception.Message));
        }
    }

    [HttpPost("connections/{connectionId:guid}/validate")]
    public async Task<IActionResult> Validate(
        Guid connectionId,
        [FromBody] MicrosoftValidateConnectionRequest request,
        CancellationToken cancellationToken)
    {
        var organizationId = await db.DocumentStorageConnections
            .Where(item => item.Id == connectionId && item.Provider == DocumentStorageProvider.MICROSOFT)
            .Select(item => (Guid?)item.OrganizationId)
            .SingleOrDefaultAsync(cancellationToken);
        if (organizationId is null) return NotFound(new ApiError("CONNECTION_NOT_FOUND", "The Microsoft connection was not found."));
        var session = await RequireCloudPermissionAsync(PermissionKeys.ValidateDocumentStorage, organizationId.Value, cancellationToken);
        if (session is null) return PermissionDeniedOrUnauthorized();

        try
        {
            return Ok(await microsoft.ValidateConnectionAsync(connectionId, session.CustomerUserId(), request, cancellationToken));
        }
        catch (MicrosoftIntegrationException exception)
        {
            return StatusCode(StatusCodes.Status422UnprocessableEntity, new ApiError(exception.Code, exception.Message));
        }
    }

    [HttpPost("connections/{connectionId:guid}/disconnect")]
    public async Task<IActionResult> Disconnect(Guid connectionId, CancellationToken cancellationToken)
    {
        var session = await RequireConnectionPermissionAsync(connectionId, PermissionKeys.DisconnectDocumentStorage, cancellationToken);
        if (session is null) return PermissionDeniedOrUnauthorized();
        try
        {
            await microsoft.DisconnectAsync(connectionId, session.CustomerUserId(), cancellationToken);
            return NoContent();
        }
        catch (MicrosoftIntegrationException exception)
        {
            return StatusCode(StatusCodes.Status422UnprocessableEntity, new ApiError(exception.Code, exception.Message));
        }
    }

    private Session? CurrentSession() => HttpContext.Items[SessionContext.ItemKey] as Session;

    private async Task<Session?> RequireCloudPermissionAsync(
        string permission,
        Guid organizationId,
        CancellationToken cancellationToken)
    {
        var session = CurrentSession();
        if (session is null || session.Application != ApplicationKind.CLOUD) return null;
        return await accessService.HasPermissionAsync(session, permission, organizationId, null, cancellationToken)
            ? session
            : null;
    }

    private async Task<Session?> RequireConnectionPermissionAsync(
        Guid connectionId,
        string permission,
        CancellationToken cancellationToken)
    {
        var organizationId = await db.DocumentStorageConnections
            .Where(item => item.Id == connectionId && item.Provider == DocumentStorageProvider.MICROSOFT)
            .Select(item => (Guid?)item.OrganizationId)
            .SingleOrDefaultAsync(cancellationToken);
        if (organizationId is null) return null;
        return await RequireCloudPermissionAsync(permission, organizationId.Value, cancellationToken);
    }

    private IActionResult PermissionDeniedOrUnauthorized() =>
        CurrentSession() is null
            ? Unauthorized(new ApiError("SESSION_INVALID", "Your session is no longer valid."))
            : StatusCode(StatusCodes.Status403Forbidden, new ApiError("ACCESS_DENIED", "You are not authorized for this Microsoft storage action."));
}