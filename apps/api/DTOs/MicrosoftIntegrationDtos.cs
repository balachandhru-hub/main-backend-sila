using System.ComponentModel.DataAnnotations;
using System.Text.Json;
using SilaMe.Api.Models;

namespace SilaMe.Api.DTOs;

public sealed class MicrosoftConnectRequest
{
    [Required]
    public Guid OrganizationId { get; set; }

    [Required]
    public string ReturnUrl { get; set; } = "/admin/users";

    [Required]
    public JsonElement Draft { get; set; }
}

public sealed record MicrosoftConnectResponse(
    string AuthorizationUrl,
    string State,
    Guid DraftId);

public sealed record MicrosoftReadinessResponse(
    bool TenantConfigured,
    bool ClientIdConfigured,
    bool ClientSecretConfigured,
    bool RedirectUriConfigured,
    bool TokenEncryptionConfigured,
    bool GraphIntegrationReady,
    string? RedirectUri);

public sealed class MicrosoftValidateConnectionRequest
{
    [Required, Url]
    public string SiteUrl { get; set; } = string.Empty;

    public string? DriveId { get; set; }
    public string? DriveName { get; set; }
    public string? FolderId { get; set; }
    public string? FolderPath { get; set; }
}

public sealed class MicrosoftSiteRequest
{
    [Required, Url]
    public string SiteUrl { get; set; } = string.Empty;
}

public sealed class MicrosoftFolderListRequest
{
    [Required]
    public string DriveId { get; set; } = string.Empty;

    public string? FolderPath { get; set; }
}

public sealed record MicrosoftSiteResponse(string Id, string DisplayName, string WebUrl);
public sealed record MicrosoftLibraryResponse(string Id, string Name);
public sealed record MicrosoftFolderResponse(string Id, string Name, string Path);

public sealed record MicrosoftDraftResponse(
    Guid DraftId,
    JsonElement Draft,
    Guid? ConnectionId,
    StorageConnectionStatus ConnectionStatus,
    string? SiteUrl,
    string? SiteDisplayName,
    string? DriveId,
    string? DriveName,
    string? FolderId,
    string? FolderPath,
    DateTime? ValidatedAt);

public sealed record MicrosoftConnectionValidationResponse(
    Guid ConnectionId,
    StorageConnectionStatus Status,
    string? TenantId,
    string? SiteId,
    string? SiteDisplayName,
    string? SiteWebUrl,
    string? DriveId,
    string? DriveName,
    string? FolderId,
    string? FolderPath,
    DateTime? ValidatedAt,
    string? Message);