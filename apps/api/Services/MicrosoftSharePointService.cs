using System.Net;
using System.Net.Http.Headers;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.EntityFrameworkCore;
using SilaMe.Api.Auth;
using SilaMe.Api.Data;
using SilaMe.Api.DTOs;
using SilaMe.Api.Models;

namespace SilaMe.Api.Services;

public sealed class MicrosoftIntegrationException(string code, string message) : Exception(message)
{
    public string Code { get; } = code;
}

public sealed class MicrosoftSharePointService(
    SilaMeDbContext db,
    IHttpClientFactory httpClientFactory,
    IConfiguration configuration,
    ILogger<MicrosoftSharePointService> logger)
{
    private const string GraphBase = "https://graph.microsoft.com/v1.0";
    private static readonly TimeSpan StateLifetime = TimeSpan.FromMinutes(10);

    public MicrosoftReadinessResponse GetReadiness()
    {
        var tenant = Get("TenantId");
        var clientId = Get("ClientId");
        var clientSecret = Get("ClientSecret");
        var redirectUri = Get("RedirectUri");
        var encryptionKey = Get("TokenEncryptionKey");
        return new(
            !string.IsNullOrWhiteSpace(tenant),
            !string.IsNullOrWhiteSpace(clientId),
            !string.IsNullOrWhiteSpace(clientSecret),
            !string.IsNullOrWhiteSpace(redirectUri),
            !string.IsNullOrWhiteSpace(encryptionKey),
            !string.IsNullOrWhiteSpace(tenant) &&
                !string.IsNullOrWhiteSpace(clientId) &&
                !string.IsNullOrWhiteSpace(clientSecret) &&
                !string.IsNullOrWhiteSpace(redirectUri) &&
                !string.IsNullOrWhiteSpace(encryptionKey),
            redirectUri);
    }

    public async Task<MicrosoftConnectResponse> CreateAuthorizationAsync(
        Session session,
        Guid organizationId,
        string returnUrl,
        JsonElement draft,
        CancellationToken cancellationToken)
    {
        EnsureConfigured();
        var safeReturnUrl = NormalizeReturnUrl(returnUrl);
        var state = Base64Url(RandomNumberGenerator.GetBytes(32));
        var stateHash = Base64Url(SHA256.HashData(Encoding.UTF8.GetBytes(state)));
        var draftJson = SanitizeDraft(draft);
        if (draftJson.Length > 20_000)
            throw new MicrosoftIntegrationException("DRAFT_TOO_LARGE", "The provisioning draft is too large to continue.");
        var draftId = Guid.NewGuid();

        db.MicrosoftAuthorizationStates.Add(new MicrosoftAuthorizationState
        {
            Id = draftId,
            StateHash = stateHash,
            UserId = session.CustomerUserId(),
            OrganizationId = organizationId,
            ReturnUrl = safeReturnUrl,
            DraftJson = draftJson,
            ExpiresAt = DateTime.UtcNow.Add(StateLifetime),
            CreatedAt = DateTime.UtcNow,
        });
        await db.SaveChangesAsync(cancellationToken);

        var tenant = GetRequired("TenantId");
        var clientId = GetRequired("ClientId");
        var redirectUri = GetRequired("RedirectUri");
        var scope = Get("Scopes") ?? "openid profile offline_access User.Read Sites.ReadWrite.All";
        var authorizationUrl = $"https://login.microsoftonline.com/{Uri.EscapeDataString(tenant)}/oauth2/v2.0/authorize" +
            $"?client_id={Uri.EscapeDataString(clientId)}" +
            "&response_type=code" +
            $"&redirect_uri={Uri.EscapeDataString(redirectUri)}" +
            $"&response_mode=query&scope={Uri.EscapeDataString(scope)}" +
            $"&state={Uri.EscapeDataString(state)}&prompt=select_account";

        logger.LogInformation("[MS-SP] AUTH_URL_CREATED for organization {OrganizationId}", organizationId);
        return new(authorizationUrl, state, draftId);
    }

    public async Task<string> HandleCallbackAsync(
        string? code,
        string? state,
        string? error,
        string? errorDescription,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(state))
        {
            logger.LogWarning("[MS-SP] CALLBACK_RECEIVED without state");
            return "/admin/users?microsoft=error&reason=invalid_state";
        }

        var stateHash = Base64Url(SHA256.HashData(Encoding.UTF8.GetBytes(state)));
        var authorizationState = await db.MicrosoftAuthorizationStates
            .SingleOrDefaultAsync(item => item.StateHash == stateHash, cancellationToken);
        if (authorizationState is null ||
            authorizationState.UsedAt is not null ||
            authorizationState.ExpiresAt <= DateTime.UtcNow)
        {
            logger.LogWarning("[MS-SP] STATE_VALIDATION_FAILED");
            return "/admin/users?microsoft=error&reason=expired_or_reused_state";
        }

        authorizationState.UsedAt = DateTime.UtcNow;
        logger.LogInformation("[MS-SP] STATE_VALIDATED for organization {OrganizationId}", authorizationState.OrganizationId);
        if (!string.IsNullOrWhiteSpace(error))
        {
            await db.SaveChangesAsync(cancellationToken);
            logger.LogWarning("[MS-SP] CALLBACK_RECEIVED with Microsoft error {Error}", error);
            return AppendQuery(authorizationState.ReturnUrl, "microsoft=error&reason=consent_denied");
        }

        if (string.IsNullOrWhiteSpace(code))
        {
            await db.SaveChangesAsync(cancellationToken);
            return AppendQuery(authorizationState.ReturnUrl, "microsoft=error&reason=missing_code");
        }

        MicrosoftToken token;
        try
        {
            EnsureConfigured();
            token = await ExchangeCodeAsync(code, cancellationToken);
        }
        catch (MicrosoftIntegrationException exception)
        {
            await db.SaveChangesAsync(cancellationToken);
            logger.LogWarning("[MS-SP] TOKEN_EXCHANGE_FAILED: {Code}", exception.Code);
            return AppendQuery(authorizationState.ReturnUrl, "microsoft=error&reason=configuration_or_token_exchange");
        }
        logger.LogInformation("[MS-SP] TOKEN_ACQUIRED for organization {OrganizationId}", authorizationState.OrganizationId);
        var connection = await db.DocumentStorageConnections
            .SingleOrDefaultAsync(item =>
                item.OrganizationId == authorizationState.OrganizationId &&
                item.Provider == DocumentStorageProvider.MICROSOFT &&
                item.Name == "Microsoft SharePoint", cancellationToken);
        var now = DateTime.UtcNow;
        if (connection is null)
        {
            connection = new DocumentStorageConnection
            {
                Id = Guid.NewGuid(),
                OrganizationId = authorizationState.OrganizationId,
                Provider = DocumentStorageProvider.MICROSOFT,
                Name = "Microsoft SharePoint",
                CreatedAt = now,
                CreatedByUserId = authorizationState.UserId,
                UpdatedAt = now,
            };
            db.DocumentStorageConnections.Add(connection);
        }

        connection.ConnectionStatus = StorageConnectionStatus.AUTHENTICATED;
        connection.TenantIdentifier = GetRequired("TenantId");
        connection.CredentialReference = Protect(JsonSerializer.Serialize(token));
        connection.ConnectedAt = now;
        connection.UpdatedAt = now;
        await db.SaveChangesAsync(cancellationToken);
        logger.LogInformation("[MS-SP] CONNECTION_AUTHENTICATED for organization {OrganizationId}", authorizationState.OrganizationId);
        return AppendQuery(authorizationState.ReturnUrl,
            $"microsoft=connected&draft={authorizationState.Id}&connectionId={connection.Id}");
    }

    public async Task<MicrosoftDraftResponse?> GetDraftAsync(
        Guid draftId,
        Guid userId,
        CancellationToken cancellationToken)
    {
        var draft = await db.MicrosoftAuthorizationStates
            .AsNoTracking()
            .SingleOrDefaultAsync(item => item.Id == draftId && item.UserId == userId, cancellationToken);
        if (draft is null) return null;
        var draftJson = JsonDocument.Parse(draft.DraftJson).RootElement.Clone();
        var connection = await db.DocumentStorageConnections.AsNoTracking()
            .SingleOrDefaultAsync(item =>
                item.OrganizationId == draft.OrganizationId &&
                item.Provider == DocumentStorageProvider.MICROSOFT &&
                item.Name == "Microsoft SharePoint", cancellationToken);
        return new(
            draft.Id,
            draftJson,
            connection?.Id,
            connection?.ConnectionStatus ?? StorageConnectionStatus.AUTHENTICATION_REQUIRED,
            connection?.DisplayUrl,
            connection?.DisplayName,
            connection?.DriveIdentifier,
            connection?.DisplayName,
            connection?.FolderIdentifier,
            connection?.FolderPath,
            connection?.ValidatedAt);
    }

    public async Task<MicrosoftConnectionValidationResponse> ValidateConnectionAsync(
        Guid connectionId,
        Guid userId,
        MicrosoftValidateConnectionRequest request,
        CancellationToken cancellationToken)
    {
        EnsureConfigured();
        var connection = await db.DocumentStorageConnections
            .SingleOrDefaultAsync(item => item.Id == connectionId && item.Provider == DocumentStorageProvider.MICROSOFT, cancellationToken)
            ?? throw new MicrosoftIntegrationException("CONNECTION_NOT_FOUND", "The Microsoft connection was not found.");
        if (!await db.UserOrganizationMemberships.AnyAsync(item =>
                item.UserId == userId &&
                item.OrganizationId == connection.OrganizationId &&
                item.Status == StatusKind.ACTIVE, cancellationToken) &&
            !await db.UserRoleAssignments.AnyAsync(item =>
                item.UserId == userId &&
                item.OrganizationId == connection.OrganizationId &&
                item.Status == StatusKind.ACTIVE &&
                item.Role.Key == "SUPER_ADMIN", cancellationToken))
        {
            throw new MicrosoftIntegrationException("ORGANIZATION_SCOPE_DENIED", "The connection is outside your organization scope.");
        }

        connection.ConnectionStatus = StorageConnectionStatus.VALIDATING;
        connection.UpdatedAt = DateTime.UtcNow;
        await db.SaveChangesAsync(cancellationToken);
        logger.LogInformation("[MS-SP] ACCESS_TEST_STARTED for connection {ConnectionId}", connectionId);
        try
        {
            var token = JsonSerializer.Deserialize<MicrosoftToken>(Unprotect(connection.CredentialReference))
                ?? throw new MicrosoftIntegrationException("CREDENTIAL_NOT_FOUND", "Microsoft authorization must be completed again.");
            var site = await ResolveSiteAsync(token.AccessToken, request.SiteUrl, cancellationToken);
            logger.LogInformation("[MS-SP] SITE_RESOLVED for connection {ConnectionId}", connectionId);
            var drive = await ResolveDriveAsync(token.AccessToken, site.Id, request.DriveId, request.DriveName, cancellationToken);
            logger.LogInformation("[MS-SP] DRIVE_RESOLVED for connection {ConnectionId}", connectionId);
            var folder = await ResolveFolderAsync(token.AccessToken, drive.Id, request.FolderId, request.FolderPath, cancellationToken);
            logger.LogInformation("[MS-SP] FOLDER_RESOLVED for connection {ConnectionId}", connectionId);
            await TestWriteAsync(token.AccessToken, drive.Id, folder.Id, cancellationToken);

            var now = DateTime.UtcNow;
            connection.ConnectionStatus = StorageConnectionStatus.CONNECTED;
            connection.SiteIdentifier = site.Id;
            connection.DisplayName = site.DisplayName;
            connection.DisplayUrl = site.WebUrl;
            connection.DriveIdentifier = drive.Id;
            connection.DriveName = drive.Name;
            connection.FolderIdentifier = folder.Id;
            connection.FolderPath = folder.Path;
            connection.ValidatedAt = now;
            connection.LastTestedAt = now;
            connection.LastTestStatus = "PASSED";
            connection.UpdatedAt = now;
            await db.SaveChangesAsync(cancellationToken);
            await EnsureValidatedDestinationAsync(connection, userId, cancellationToken);
            logger.LogInformation("[MS-SP] ACCESS_TEST_PASSED for connection {ConnectionId}", connectionId);
            return ToValidationResponse(connection, "Microsoft SharePoint read/write access validated.");
        }
        catch (Exception exception) when (exception is HttpRequestException or MicrosoftIntegrationException or JsonException)
        {
            connection.ConnectionStatus = StorageConnectionStatus.VALIDATION_FAILED;
            connection.LastTestedAt = DateTime.UtcNow;
            connection.LastTestStatus = exception.Message;
            connection.UpdatedAt = DateTime.UtcNow;
            await db.SaveChangesAsync(cancellationToken);
            logger.LogWarning("[MS-SP] ACCESS_TEST_FAILED for connection {ConnectionId}: {Message}", connectionId, exception.Message);
            if (exception is MicrosoftIntegrationException integrationException) throw;
            throw new MicrosoftIntegrationException("GRAPH_VALIDATION_FAILED", "Microsoft Graph could not validate the selected SharePoint destination.");
        }
    }

    public async Task<MicrosoftSiteResponse> ResolveSiteAsync(
        Guid connectionId,
        Guid userId,
        string siteUrl,
        CancellationToken cancellationToken)
    {
        var (connection, token) = await GetAuthorizedConnectionAsync(connectionId, userId, cancellationToken);
        var site = await ResolveSiteAsync(token.AccessToken, siteUrl, cancellationToken);
        return new(site.Id, site.DisplayName, site.WebUrl);
    }

    public async Task<IReadOnlyList<MicrosoftLibraryResponse>> ListLibrariesAsync(
        Guid connectionId,
        Guid userId,
        string siteId,
        CancellationToken cancellationToken)
    {
        var (_, token) = await GetAuthorizedConnectionAsync(connectionId, userId, cancellationToken);
        var drives = await GetGraphCollectionAsync<GraphDrive>($"/sites/{Uri.EscapeDataString(siteId)}/drives", token.AccessToken, cancellationToken);
        return drives.Select(item => new MicrosoftLibraryResponse(item.Id, item.Name)).ToList();
    }

    public async Task<IReadOnlyList<MicrosoftFolderResponse>> ListFoldersAsync(
        Guid connectionId,
        Guid userId,
        MicrosoftFolderListRequest request,
        CancellationToken cancellationToken)
    {
        var (_, token) = await GetAuthorizedConnectionAsync(connectionId, userId, cancellationToken);
        GraphFolder parent;
        if (string.IsNullOrWhiteSpace(request.FolderPath))
            parent = await GetGraphAsync<GraphFolder>($"/drives/{Uri.EscapeDataString(request.DriveId)}/root", token.AccessToken, cancellationToken);
        else
            parent = await ResolveFolderAsync(token.AccessToken, request.DriveId, null, request.FolderPath, cancellationToken);
        var children = await GetGraphCollectionAsync<GraphFolder>(
            $"/drives/{Uri.EscapeDataString(request.DriveId)}/items/{Uri.EscapeDataString(parent.Id)}/children",
            token.AccessToken, cancellationToken);
        return children
            .Where(item => item.Folder is not null)
            .Select(item => new MicrosoftFolderResponse(item.Id, item.Name ?? item.Id, item.Path))
            .ToList();
    }

    public async Task<MicrosoftConnectionValidationResponse> GetConnectionAsync(
        Guid connectionId,
        Guid userId,
        CancellationToken cancellationToken)
    {
        var connection = await db.DocumentStorageConnections.AsNoTracking()
            .SingleOrDefaultAsync(item => item.Id == connectionId && item.Provider == DocumentStorageProvider.MICROSOFT, cancellationToken)
            ?? throw new MicrosoftIntegrationException("CONNECTION_NOT_FOUND", "The Microsoft connection was not found.");
        if (!await HasOrganizationAccessAsync(connection.OrganizationId, userId, cancellationToken))
            throw new MicrosoftIntegrationException("ORGANIZATION_SCOPE_DENIED", "The connection is outside your organization scope.");
        return ToValidationResponse(connection, connection.ConnectionStatus == StorageConnectionStatus.CONNECTED
            ? "Microsoft SharePoint read/write access is validated."
            : "Microsoft SharePoint still requires destination validation.");
    }

    public async Task DisconnectAsync(Guid connectionId, Guid userId, CancellationToken cancellationToken)
    {
        var connection = await db.DocumentStorageConnections
            .SingleOrDefaultAsync(item => item.Id == connectionId && item.Provider == DocumentStorageProvider.MICROSOFT, cancellationToken)
            ?? throw new MicrosoftIntegrationException("CONNECTION_NOT_FOUND", "The Microsoft connection was not found.");
        if (!await HasOrganizationAccessAsync(connection.OrganizationId, userId, cancellationToken))
            throw new MicrosoftIntegrationException("ORGANIZATION_SCOPE_DENIED", "The connection is outside your organization scope.");
        connection.ConnectionStatus = StorageConnectionStatus.DISCONNECTED;
        connection.CredentialReference = null;
        connection.SiteIdentifier = null;
        connection.DriveIdentifier = null;
        connection.FolderIdentifier = null;
        connection.FolderPath = null;
        connection.ValidatedAt = null;
        connection.LastTestedAt = DateTime.UtcNow;
        connection.LastTestStatus = "DISCONNECTED";
        connection.UpdatedAt = DateTime.UtcNow;
        var destinations = await db.DocumentStorageDestinations
            .Where(item => item.StorageConnectionId == connection.Id)
            .ToListAsync(cancellationToken);
        foreach (var destination in destinations)
        {
            destination.Status = StorageDestinationStatus.VALIDATION_REQUIRED;
            destination.ExternalTransferEnabled = false;
            destination.UpdatedAt = DateTime.UtcNow;
        }
        await db.SaveChangesAsync(cancellationToken);
        logger.LogInformation("[MS-SP] CONNECTION_DISCONNECTED for connection {ConnectionId}", connectionId);
    }

    public async Task<MicrosoftUploadedFile> UploadFileAsync(
        Guid connectionId,
        Guid userId,
        string siteId,
        string driveId,
        string folderId,
        string fileName,
        Stream content,
        CancellationToken cancellationToken)
    {
        var (_, token) = await GetAuthorizedConnectionAsync(connectionId, userId, cancellationToken);
        using var request = new HttpRequestMessage(
            HttpMethod.Put,
            $"{GraphBase}/drives/{Uri.EscapeDataString(driveId)}/items/{Uri.EscapeDataString(folderId)}:/{Uri.EscapeDataString(fileName)}:/content");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token.AccessToken);
        request.Content = new StreamContent(content);
        request.Content.Headers.ContentType = new MediaTypeHeaderValue("application/pdf");
        using var response = await httpClientFactory.CreateClient("microsoft-graph").SendAsync(request, cancellationToken);
        if (response.StatusCode == HttpStatusCode.Unauthorized)
            throw new MicrosoftIntegrationException("TOKEN_EXPIRED", "Microsoft authorization has expired.");
        if (response.StatusCode == HttpStatusCode.Forbidden)
            throw new MicrosoftIntegrationException("GRAPH_FORBIDDEN", "Microsoft denied access to the SharePoint destination.");
        if (!response.IsSuccessStatusCode)
            throw new MicrosoftIntegrationException("GRAPH_UPLOAD_FAILED", "Microsoft could not store the document.");
        var uploaded = await response.Content.ReadFromJsonAsync<GraphDriveItem>(cancellationToken: cancellationToken)
            ?? throw new MicrosoftIntegrationException("GRAPH_RESPONSE_INVALID", "Microsoft returned an invalid upload response.");
        return new(uploaded.Id ?? string.Empty, uploaded.Name ?? fileName, uploaded.WebUrl);
    }

    private async Task<MicrosoftToken> ExchangeCodeAsync(string code, CancellationToken cancellationToken)
    {
        var client = httpClientFactory.CreateClient();
        using var response = await client.PostAsync(
            $"https://login.microsoftonline.com/{Uri.EscapeDataString(GetRequired("TenantId"))}/oauth2/v2.0/token",
            new FormUrlEncodedContent(new Dictionary<string, string>
            {
                ["client_id"] = GetRequired("ClientId"),
                ["client_secret"] = GetRequired("ClientSecret"),
                ["grant_type"] = "authorization_code",
                ["code"] = code,
                ["redirect_uri"] = GetRequired("RedirectUri"),
                ["scope"] = Get("Scopes") ?? "openid profile offline_access User.Read Sites.ReadWrite.All",
            }), cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            throw new MicrosoftIntegrationException("TOKEN_EXCHANGE_FAILED", "Microsoft authorization could not be completed.");
        }
        var payload = await response.Content.ReadFromJsonAsync<MicrosoftTokenResponse>(cancellationToken: cancellationToken)
            ?? throw new MicrosoftIntegrationException("TOKEN_RESPONSE_INVALID", "Microsoft returned an invalid authorization response.");
        return new(payload.AccessToken, payload.RefreshToken, DateTime.UtcNow.AddSeconds(payload.ExpiresIn));
    }

    private async Task<GraphSite> ResolveSiteAsync(string accessToken, string siteUrl, CancellationToken cancellationToken)
    {
        if (!Uri.TryCreate(siteUrl, UriKind.Absolute, out var uri) ||
            uri.Scheme != Uri.UriSchemeHttps ||
            !uri.Host.EndsWith(".sharepoint.com", StringComparison.OrdinalIgnoreCase))
        {
            throw new MicrosoftIntegrationException("SITE_URL_INVALID", "Enter a valid HTTPS SharePoint site URL.");
        }
        var path = string.IsNullOrWhiteSpace(uri.AbsolutePath) ? "/" : uri.AbsolutePath.TrimEnd('/');
        return await GetGraphAsync<GraphSite>($"/sites/{uri.Host}:{path}", accessToken, cancellationToken);
    }

    private async Task<GraphDrive> ResolveDriveAsync(string accessToken, string siteId, string? driveId, string? driveName, CancellationToken cancellationToken)
    {
        var drives = await GetGraphCollectionAsync<GraphDrive>($"/sites/{Uri.EscapeDataString(siteId)}/drives", accessToken, cancellationToken);
        var drive = !string.IsNullOrWhiteSpace(driveId)
            ? drives.SingleOrDefault(item => item.Id == driveId)
            : drives.FirstOrDefault(item => string.Equals(item.Name, driveName ?? "Documents", StringComparison.OrdinalIgnoreCase))
                ?? drives.FirstOrDefault();
        return drive ?? throw new MicrosoftIntegrationException("LIBRARY_NOT_FOUND", "The selected SharePoint document library is not accessible.");
    }

    private async Task<GraphFolder> ResolveFolderAsync(string accessToken, string driveId, string? folderId, string? folderPath, CancellationToken cancellationToken)
    {
        if (!string.IsNullOrWhiteSpace(folderId))
        {
            return await GetGraphAsync<GraphFolder>($"/drives/{Uri.EscapeDataString(driveId)}/items/{Uri.EscapeDataString(folderId)}", accessToken, cancellationToken);
        }
        if (string.IsNullOrWhiteSpace(folderPath))
        {
            return await GetGraphAsync<GraphFolder>($"/drives/{Uri.EscapeDataString(driveId)}/root", accessToken, cancellationToken);
        }
        var path = "/" + folderPath.Trim().Trim('/');
        return await GetGraphAsync<GraphFolder>($"/drives/{Uri.EscapeDataString(driveId)}/root:{path}:", accessToken, cancellationToken);
    }

    private async Task<(DocumentStorageConnection Connection, MicrosoftToken Token)> GetAuthorizedConnectionAsync(
        Guid connectionId,
        Guid userId,
        CancellationToken cancellationToken)
    {
        var connection = await db.DocumentStorageConnections
            .SingleOrDefaultAsync(item => item.Id == connectionId && item.Provider == DocumentStorageProvider.MICROSOFT, cancellationToken)
            ?? throw new MicrosoftIntegrationException("CONNECTION_NOT_FOUND", "The Microsoft connection was not found.");
        if (!await HasOrganizationAccessAsync(connection.OrganizationId, userId, cancellationToken))
            throw new MicrosoftIntegrationException("ORGANIZATION_SCOPE_DENIED", "The connection is outside your organization scope.");
        var token = JsonSerializer.Deserialize<MicrosoftToken>(Unprotect(connection.CredentialReference))
            ?? throw new MicrosoftIntegrationException("CREDENTIAL_NOT_FOUND", "Microsoft authorization must be completed again.");
        if (token.ExpiresAt <= DateTime.UtcNow)
            throw new MicrosoftIntegrationException("TOKEN_EXPIRED", "Microsoft authorization has expired.");
        return (connection, token);
    }

    private async Task EnsureValidatedDestinationAsync(
        DocumentStorageConnection connection,
        Guid userId,
        CancellationToken cancellationToken)
    {
        if (connection.ConnectionStatus != StorageConnectionStatus.CONNECTED ||
            string.IsNullOrWhiteSpace(connection.FolderPath) ||
            string.IsNullOrWhiteSpace(connection.SiteIdentifier) ||
            string.IsNullOrWhiteSpace(connection.DriveIdentifier) ||
            string.IsNullOrWhiteSpace(connection.FolderIdentifier))
            return;

        var destination = await db.DocumentStorageDestinations
            .SingleOrDefaultAsync(item =>
                item.OrganizationId == connection.OrganizationId &&
                item.StorageConnectionId == connection.Id &&
                item.DocumentType == DocumentType.INVOICE &&
                item.OperatingUnitId == null, cancellationToken);
        var now = DateTime.UtcNow;
        if (destination is null)
        {
            destination = new DocumentStorageDestination
            {
                Id = Guid.NewGuid(),
                OrganizationId = connection.OrganizationId,
                StorageConnectionId = connection.Id,
                Provider = connection.Provider,
                DocumentType = DocumentType.INVOICE,
                FolderPath = connection.FolderPath,
                CreatedByUserId = userId,
                CreatedAt = now,
            };
            db.DocumentStorageDestinations.Add(destination);
        }
        destination.SiteIdentifier = connection.SiteIdentifier;
        destination.DriveIdentifier = connection.DriveIdentifier;
        destination.FolderIdentifier = connection.FolderIdentifier;
        destination.FolderPath = connection.FolderPath;
        destination.DisplayUrl = connection.DisplayUrl;
        destination.ExternalTransferEnabled = true;
        destination.Status = StorageDestinationStatus.ACTIVE;
        destination.ValidatedAt = connection.ValidatedAt;
        destination.UpdatedAt = now;
        await db.SaveChangesAsync(cancellationToken);
    }

    private async Task<bool> HasOrganizationAccessAsync(Guid organizationId, Guid userId, CancellationToken cancellationToken) =>
        await db.UserOrganizationMemberships.AnyAsync(item =>
            item.UserId == userId && item.OrganizationId == organizationId && item.Status == StatusKind.ACTIVE, cancellationToken) ||
        await db.UserRoleAssignments.AnyAsync(item =>
            item.UserId == userId && item.OrganizationId == organizationId &&
            item.Status == StatusKind.ACTIVE && item.Role.Key == "SUPER_ADMIN", cancellationToken);

    private async Task TestWriteAsync(string accessToken, string driveId, string folderId, CancellationToken cancellationToken)
    {
        var fileName = $"sila-me-connection-test-{Guid.NewGuid():N}.txt";
        var client = httpClientFactory.CreateClient();
        using var request = new HttpRequestMessage(HttpMethod.Put,
            $"{GraphBase}/drives/{Uri.EscapeDataString(driveId)}/items/{Uri.EscapeDataString(folderId)}:/{fileName}:/content")
        {
            Content = new StringContent("SILA ME SharePoint connection test", Encoding.UTF8, "text/plain"),
        };
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
        using var response = await client.SendAsync(request, cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            throw new MicrosoftIntegrationException("WRITE_ACCESS_DENIED", "SILA ME could not write to the selected SharePoint folder.");
        }
        var created = await response.Content.ReadFromJsonAsync<GraphDriveItem>(cancellationToken: cancellationToken);
        if (created?.Id is null) throw new MicrosoftIntegrationException("WRITE_TEST_INVALID", "Microsoft did not confirm the validation file.");
        using var delete = new HttpRequestMessage(HttpMethod.Delete,
            $"{GraphBase}/drives/{Uri.EscapeDataString(driveId)}/items/{Uri.EscapeDataString(created.Id)}");
        delete.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
        await client.SendAsync(delete, cancellationToken);
    }

    private async Task<T> GetGraphAsync<T>(string path, string accessToken, CancellationToken cancellationToken)
    {
        var client = httpClientFactory.CreateClient();
        using var request = new HttpRequestMessage(HttpMethod.Get, GraphBase + path);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
        using var response = await client.SendAsync(request, cancellationToken);
        if (response.StatusCode == HttpStatusCode.Forbidden) throw new MicrosoftIntegrationException("GRAPH_FORBIDDEN", "Microsoft denied access to the selected SharePoint destination.");
        if (!response.IsSuccessStatusCode) throw new MicrosoftIntegrationException("GRAPH_RESOURCE_NOT_FOUND", "Microsoft could not resolve the selected SharePoint resource.");
        return await response.Content.ReadFromJsonAsync<T>(cancellationToken: cancellationToken)
            ?? throw new MicrosoftIntegrationException("GRAPH_RESPONSE_INVALID", "Microsoft returned an invalid SharePoint response.");
    }

    private async Task<IReadOnlyList<T>> GetGraphCollectionAsync<T>(string path, string accessToken, CancellationToken cancellationToken)
    {
        var response = await GetGraphAsync<GraphCollection<T>>(path, accessToken, cancellationToken);
        return response.Value;
    }

    private void EnsureConfigured()
    {
        var readiness = GetReadiness();
        if (!readiness.GraphIntegrationReady)
            throw new MicrosoftIntegrationException("MICROSOFT_CONFIGURATION_REQUIRED", "Microsoft integration requires TenantId, ClientId, ClientSecret, RedirectUri, and TokenEncryptionKey configuration.");
    }

    private string GetRequired(string key) => Get(key) ?? throw new MicrosoftIntegrationException("MICROSOFT_CONFIGURATION_REQUIRED", $"Microsoft:{key} is not configured.");
    private string? Get(string key) => configuration[$"Microsoft:{key}"] ?? configuration[$"Microsoft__{key}"];

    private string Protect(string plaintext)
    {
        var key = SHA256.HashData(Encoding.UTF8.GetBytes(GetRequired("TokenEncryptionKey")));
        var nonce = RandomNumberGenerator.GetBytes(12);
        var plaintextBytes = Encoding.UTF8.GetBytes(plaintext);
        var ciphertext = new byte[plaintextBytes.Length];
        var tag = new byte[16];
        using var aes = new AesGcm(key, 16);
        aes.Encrypt(nonce, plaintextBytes, ciphertext, tag);
        return $"v1:{Base64Url(nonce)}:{Base64Url(tag)}:{Base64Url(ciphertext)}";
    }

    private string Unprotect(string? protectedValue)
    {
        if (string.IsNullOrWhiteSpace(protectedValue) || !protectedValue.StartsWith("v1:", StringComparison.Ordinal))
            throw new MicrosoftIntegrationException("CREDENTIAL_NOT_FOUND", "Microsoft authorization must be completed again.");
        var parts = protectedValue.Split(':');
        if (parts.Length != 4) throw new MicrosoftIntegrationException("CREDENTIAL_INVALID", "Microsoft authorization data is invalid.");
        var key = SHA256.HashData(Encoding.UTF8.GetBytes(GetRequired("TokenEncryptionKey")));
        var nonce = FromBase64Url(parts[1]);
        var tag = FromBase64Url(parts[2]);
        var ciphertext = FromBase64Url(parts[3]);
        var plaintext = new byte[ciphertext.Length];
        using var aes = new AesGcm(key, 16);
        aes.Decrypt(nonce, ciphertext, tag, plaintext);
        return Encoding.UTF8.GetString(plaintext);
    }

    private static string NormalizeReturnUrl(string returnUrl) =>
        Uri.TryCreate(returnUrl, UriKind.Relative, out var uri) && returnUrl.StartsWith("/", StringComparison.Ordinal) && !returnUrl.StartsWith("//", StringComparison.Ordinal)
            ? uri!.PathAndQuery
            : "/admin/users";

    private static string SanitizeDraft(JsonElement draft)
    {
        if (draft.ValueKind is JsonValueKind.Undefined or JsonValueKind.Null) return "{}";
        using var document = JsonDocument.Parse(draft.GetRawText());
        using var stream = new MemoryStream();
        using (var writer = new Utf8JsonWriter(stream))
        {
            WriteSanitized(writer, document.RootElement);
        }
        return Encoding.UTF8.GetString(stream.ToArray());
    }

    private static void WriteSanitized(Utf8JsonWriter writer, JsonElement element)
    {
        switch (element.ValueKind)
        {
            case JsonValueKind.Object:
                writer.WriteStartObject();
                foreach (var property in element.EnumerateObject())
                {
                    if (property.Name.Equals("password", StringComparison.OrdinalIgnoreCase) ||
                        property.Name.Equals("temporaryPassword", StringComparison.OrdinalIgnoreCase) ||
                        property.Name.Contains("token", StringComparison.OrdinalIgnoreCase) ||
                        property.Name.Contains("secret", StringComparison.OrdinalIgnoreCase))
                        continue;
                    writer.WritePropertyName(property.Name);
                    WriteSanitized(writer, property.Value);
                }
                writer.WriteEndObject();
                break;
            case JsonValueKind.Array:
                writer.WriteStartArray();
                foreach (var item in element.EnumerateArray()) WriteSanitized(writer, item);
                writer.WriteEndArray();
                break;
            default:
                element.WriteTo(writer);
                break;
        }
    }

    private static string AppendQuery(string url, string query) => $"{url}{(url.Contains('?') ? "&" : "?")}{query}";
    private static string Base64Url(byte[] bytes) => Convert.ToBase64String(bytes).TrimEnd('=').Replace('+', '-').Replace('/', '_');
    private static byte[] FromBase64Url(string value) => Convert.FromBase64String(value.Replace('-', '+').Replace('_', '/') + new string('=', (4 - value.Length % 4) % 4));
    private static MicrosoftConnectionValidationResponse ToValidationResponse(DocumentStorageConnection item, string message) => new(
        item.Id, item.ConnectionStatus, item.TenantIdentifier, item.SiteIdentifier, item.DisplayName,
        item.DisplayUrl, item.DriveIdentifier, item.DriveName, item.FolderIdentifier, item.FolderPath,
        item.ValidatedAt, message);

    private sealed record MicrosoftToken(string AccessToken, string? RefreshToken, DateTime ExpiresAt);
    public sealed record MicrosoftUploadedFile(string Id, string Name, string? WebUrl);
    private sealed record MicrosoftTokenResponse(
        [property: JsonPropertyName("access_token")] string AccessToken,
        [property: JsonPropertyName("refresh_token")] string? RefreshToken,
        [property: JsonPropertyName("expires_in")] int ExpiresIn);
    private sealed record GraphCollection<T>([property: JsonPropertyName("value")] IReadOnlyList<T> Value);
    private sealed record GraphSite(
        [property: JsonPropertyName("id")] string Id,
        [property: JsonPropertyName("displayName")] string DisplayName,
        [property: JsonPropertyName("webUrl")] string WebUrl);
    private sealed record GraphDrive(
        [property: JsonPropertyName("id")] string Id,
        [property: JsonPropertyName("name")] string Name);
    private sealed record GraphDriveItem(
        [property: JsonPropertyName("id")] string? Id,
        [property: JsonPropertyName("name")] string? Name,
        [property: JsonPropertyName("webUrl")] string? WebUrl);
    private sealed record GraphFolder(
        [property: JsonPropertyName("id")] string Id,
        [property: JsonPropertyName("name")] string? Name,
        [property: JsonPropertyName("parentReference")] GraphParentReference? ParentReference,
        [property: JsonPropertyName("folder")] JsonElement? Folder)
    {
        public string Path => ParentReference?.Path is { } parent && Name is { } name
            ? $"{parent.TrimStart('/')}/{name}"
            : Name ?? "/";
    }
    private sealed record GraphParentReference([property: JsonPropertyName("path")] string? Path);
}