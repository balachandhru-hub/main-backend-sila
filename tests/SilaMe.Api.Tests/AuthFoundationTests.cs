using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Collections.Concurrent;
using System.Text;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using SilaMe.Api.Data;
using SilaMe.Api.Models;
using SilaMe.Api.Services;
using Xunit;

namespace SilaMe.Api.Tests;

public sealed class ApiFactory : WebApplicationFactory<Program>
{
    public RecordingGraphHandler GraphHandler { get; } = new();
    public RecordingIntegrationHandler IntegrationHandler { get; } = new();

    protected override void ConfigureWebHost(Microsoft.AspNetCore.Hosting.IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");
        builder.UseSetting("Microsoft:TokenEncryptionKey", "test-token-encryption-key");
        Environment.SetEnvironmentVariable("SILA_ME_PLATFORM_ADMIN_PASSWORD", "PlatformAdmin123!");
        builder.ConfigureServices(services =>
        {
            services.AddHttpClient("microsoft-graph")
                .ConfigurePrimaryHttpMessageHandler(() => GraphHandler);
            services.AddHttpClient("api-integrations")
                .ConfigurePrimaryHttpMessageHandler(() => IntegrationHandler);
        });
    }
}

public sealed record RecordedGraphRequest(HttpMethod Method, Uri Uri, byte[] Content);

public sealed class RecordingGraphHandler : HttpMessageHandler
{
    private readonly ConcurrentQueue<HttpResponseMessage> responses = new();
    private readonly ConcurrentQueue<RecordedGraphRequest> requests = new();

    public IReadOnlyList<RecordedGraphRequest> Requests => requests.ToArray();

    public void Reset()
    {
        while (responses.TryDequeue(out var response)) response.Dispose();
        while (requests.TryDequeue(out _)) { }
    }

    public void EnqueueResponse(HttpStatusCode statusCode, string body) =>
        responses.Enqueue(new HttpResponseMessage(statusCode)
        {
            Content = new StringContent(body, Encoding.UTF8, "application/json"),
        });

    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        var content = request.Content is null
            ? []
            : await request.Content.ReadAsByteArrayAsync(cancellationToken);
        requests.Enqueue(new RecordedGraphRequest(request.Method, request.RequestUri!, content));
        if (responses.TryDequeue(out var response)) return response;
        return new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(
                """{"id":"default-graph-file","name":"default.pdf","webUrl":"https://sharepoint.test/default.pdf"}""",
                Encoding.UTF8,
                "application/json"),
        };
    }
}

public sealed class AuthFoundationTests(ApiFactory factory) : IClassFixture<ApiFactory>, IAsyncLifetime
{
    private const string CloudEmail = "cloud-test@silame.local";
    private const string MobileEmail = "mobile-test@silame.local";
    private const string Password = "test-password-strong";
    private const string TestOrganizationCode = "TEST-PHASE2";

    public async Task InitializeAsync()
    {
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<SilaMeDbContext>();
        await db.MicrosoftAuthorizationStates
            .Where(state => state.User.Email == CloudEmail || state.User.Email == MobileEmail)
            .ExecuteDeleteAsync();
        await db.DocumentStorageConnections
            .Where(connection => connection.CreatedByUser.Email == CloudEmail || connection.CreatedByUser.Email == MobileEmail)
            .ExecuteDeleteAsync();
        await db.Users
            .Where(user => user.Email == CloudEmail || user.Email == MobileEmail)
            .ExecuteDeleteAsync();
        await db.Organizations
            .Where(organization => organization.Code == TestOrganizationCode)
            .ExecuteDeleteAsync();
        await db.Roles
            .Where(role => role.Key == "TEST_SUPER_ADMIN" || role.Key == "TEST_VIEWER")
            .ExecuteDeleteAsync();

        var passwordService = scope.ServiceProvider.GetRequiredService<IPasswordService>();
        var cloudUser = CreateUser(CloudEmail, "Cloud Test User", passwordService);
        var mobileUser = CreateUser(MobileEmail, "Mobile Test User", passwordService);
        var organization = new Organization
        {
            Id = Guid.NewGuid(),
            Code = TestOrganizationCode,
            Name = "Phase 2 Test Organization",
            Kind = OrganizationKind.CUSTOMER,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
        };
        var organizationUnit = new OrganizationUnit
        {
            Id = Guid.NewGuid(),
            OrganizationId = organization.Id,
            Code = "TEST-STORE",
            Name = "Test Store",
            Kind = OrganizationUnitKind.STORE,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
        };
        var existingPermissions = await db.Permissions
            .Where(permission => PermissionKeys.All.Select(definition => definition.Key).Contains(permission.Key))
            .ToDictionaryAsync(permission => permission.Key);
        var permissions = new List<Permission>();
        var newPermissions = new List<Permission>();
        foreach (var definition in PermissionKeys.All)
        {
            if (existingPermissions.TryGetValue(definition.Key, out var existingPermission))
            {
                permissions.Add(existingPermission);
                continue;
            }

            var permission = new Permission
            {
                Id = Guid.NewGuid(),
                Key = definition.Key,
                Name = definition.Name,
                Description = definition.Description,
                CreatedAt = DateTime.UtcNow,
            };
            permissions.Add(permission);
            newPermissions.Add(permission);
        }
        var superAdmin = new Role
        {
            Id = Guid.NewGuid(),
            Key = "TEST_SUPER_ADMIN",
            Name = "Test super administrator",
            Description = "Test role",
            IsSystem = false,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
        };
        var viewer = new Role
        {
            Id = Guid.NewGuid(),
            Key = "TEST_VIEWER",
            Name = "Test viewer",
            Description = "Test role",
            IsSystem = false,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
        };
        superAdmin.Permissions = permissions.Select(permission => new RolePermission
        {
            RoleId = superAdmin.Id,
            PermissionId = permission.Id,
        }).ToList();
        viewer.Permissions = permissions
            .Where(permission => permission.Key == PermissionKeys.OrganizationRead)
            .Select(permission => new RolePermission
            {
                RoleId = viewer.Id,
                PermissionId = permission.Id,
            }).ToList();

        db.Users.AddRange(cloudUser, mobileUser);
        db.Organizations.Add(organization);
        db.OrganizationUnits.Add(organizationUnit);
        db.Permissions.AddRange(newPermissions);
        db.Roles.AddRange(superAdmin, viewer);
        db.UserApplicationAccess.AddRange(
            Access(cloudUser, ApplicationKind.CLOUD),
            Access(mobileUser, ApplicationKind.MOBILE));
        db.UserOrganizationMemberships.AddRange(
            Membership(cloudUser, organization, organizationUnit),
            Membership(mobileUser, organization, null));
        db.UserRoleAssignments.AddRange(
            RoleAssignment(cloudUser, superAdmin, organization, null),
            RoleAssignment(mobileUser, viewer, organization, null));
        await db.SaveChangesAsync();
    }

    public Task DisposeAsync() => Task.CompletedTask;

    [Fact]
    public async Task Health_endpoint_is_public_and_healthy()
    {
        var response = await factory.CreateClient().GetAsync("/api/healthz");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("ok", (await response.Content.ReadFromJsonAsync<HealthResponse>())?.Status);
    }

    [Fact]
    public async Task Cloud_login_creates_cookie_session_and_me_profile()
    {
        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions { HandleCookies = true });
        client.DefaultRequestHeaders.Add("X-Sila-Route-Slug", "sila-dev");
        var login = await client.PostAsJsonAsync("/api/auth/cloud/login", new { email = CloudEmail, password = Password });
        Assert.Equal(HttpStatusCode.OK, login.StatusCode);
        Assert.Contains("sila_me_session", string.Join(";", login.Headers.GetValues("Set-Cookie")));
        Assert.Contains("max-age=7776000", string.Join(";", login.Headers.GetValues("Set-Cookie")), StringComparison.OrdinalIgnoreCase);

        var me = await client.GetFromJsonAsync<UserResponse>("/api/me");
        Assert.Equal(CloudEmail, me?.Email);
        Assert.Equal("CLOUD", me?.Application);
    }

    [Fact]
    public async Task Mobile_login_returns_bearer_token_and_me_profile()
    {
        using var client = factory.CreateClient();
        client.DefaultRequestHeaders.Add("X-Sila-Route-Slug", "sila-dev");
        var login = await client.PostAsJsonAsync("/api/auth/mobile/login", new { email = MobileEmail, password = Password });
        Assert.Equal(HttpStatusCode.OK, login.StatusCode);
        var session = await login.Content.ReadFromJsonAsync<MobileSession>();
        Assert.False(string.IsNullOrWhiteSpace(session?.Token));

        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", session!.Token);
        var me = await client.GetFromJsonAsync<UserResponse>("/api/me");
        Assert.Equal(MobileEmail, me?.Email);
        Assert.Equal("MOBILE", me?.Application);
    }

    [Fact]
    public async Task Mobile_login_preflight_allows_expo_web_origin()
    {
        using var client = factory.CreateClient();
        using var request = new HttpRequestMessage(HttpMethod.Options, "/api/auth/mobile/login");
        request.Headers.Add("Origin", "http://localhost:8081");
        request.Headers.Add("Access-Control-Request-Method", "POST");
        request.Headers.Add("Access-Control-Request-Headers", "content-type");

        var response = await client.SendAsync(request);

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        Assert.Equal("http://localhost:8081", response.Headers.GetValues("Access-Control-Allow-Origin").Single());
    }

    [Fact]
    public async Task Wrong_password_is_rejected()
    {
        var client = factory.CreateClient();
        client.DefaultRequestHeaders.Add("X-Sila-Route-Slug", "sila-dev");
        var response = await client.PostAsJsonAsync(
            "/api/auth/cloud/login",
            new { email = CloudEmail, password = "wrong-password" });
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Application_access_is_isolated()
    {
        var mobileClient = factory.CreateClient();
        mobileClient.DefaultRequestHeaders.Add("X-Sila-Route-Slug", "sila-dev");
        var cloudOnMobile = await mobileClient.PostAsJsonAsync(
            "/api/auth/mobile/login",
            new { email = CloudEmail, password = Password });
        var mobileOnCloud = factory.CreateClient();
        mobileOnCloud.DefaultRequestHeaders.Add("X-Sila-Route-Slug", "sila-dev");
        var mobileOnCloudResponse = await mobileOnCloud.PostAsJsonAsync(
            "/api/auth/cloud/login",
            new { email = MobileEmail, password = Password });

        Assert.Equal(HttpStatusCode.Unauthorized, cloudOnMobile.StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, mobileOnCloudResponse.StatusCode);
    }

    [Fact]
    public async Task Mobile_logout_revokes_old_token()
    {
        using var client = factory.CreateClient();
        client.DefaultRequestHeaders.Add("X-Sila-Route-Slug", "sila-dev");
        var login = await client.PostAsJsonAsync("/api/auth/mobile/login", new { email = MobileEmail, password = Password });
        var session = await login.Content.ReadFromJsonAsync<MobileSession>();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", session!.Token);

        Assert.Equal(HttpStatusCode.NoContent, (await client.PostAsync("/api/auth/mobile/logout", null)).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await client.GetAsync("/api/me")).StatusCode);
    }

    [Fact]
    public async Task Cloud_access_context_returns_scoped_organization_and_permissions()
    {
        using var client = await CloudClientAsync();
        var response = await client.GetAsync("/api/v1/access/context");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var context = await response.Content.ReadFromJsonAsync<AccessContext>();
        Assert.Contains(context!.Organizations, organization => organization.Code == TestOrganizationCode);
        Assert.Contains(context.Units, unit => unit.Code == "TEST-STORE");
        Assert.Contains(context.Permissions, permission => permission.Key == PermissionKeys.UserManage);
    }

    [Fact]
    public async Task Mobile_access_context_is_read_only_and_user_list_is_cloud_only()
    {
        using var client = await MobileClientAsync();
        var context = await client.GetFromJsonAsync<AccessContext>("/api/v1/access/context");
        Assert.Contains(context!.Roles, role => role.Key == "TEST_VIEWER");
        Assert.DoesNotContain(context.Permissions, permission => permission.Key == PermissionKeys.UserManage);

        Assert.Equal(HttpStatusCode.Forbidden, (await client.GetAsync("/api/v1/access/users")).StatusCode);
    }

    [Fact]
    public async Task Cloud_access_user_list_is_limited_to_accessible_organizations()
    {
        using var client = await CloudClientAsync();
        var response = await client.GetAsync("/api/v1/access/users");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var users = await response.Content.ReadFromJsonAsync<List<AccessUser>>();
        Assert.Contains(users!, user => user.Email == CloudEmail);
        Assert.Contains(users!, user => user.Email == MobileEmail);
    }

    [Fact]
    public async Task Duplicate_email_is_rejected_by_user_provisioning()
    {
        using var client = await CloudClientAsync();
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<SilaMeDbContext>();
        var organizationId = await db.Organizations.Where(item => item.Code == TestOrganizationCode).Select(item => item.Id).SingleAsync();
        var response = await client.PostAsJsonAsync("/api/v1/access/users", new
        {
            email = CloudEmail,
            displayName = "Duplicate Cloud User",
            applications = new[] { "CLOUD" },
            organizationId,
        });
        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
        Assert.Contains("USER_ALREADY_EXISTS", await response.Content.ReadAsStringAsync());
    }

    [Fact]
    public async Task Customer_admin_cannot_assign_platform_role()
    {
        using var client = await CloudClientAsync();
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<SilaMeDbContext>();
        var organizationId = await db.Organizations.Where(item => item.Code == TestOrganizationCode).Select(item => item.Id).SingleAsync();
        var platformRoleId = await db.Roles.Where(item => item.Key == "SUPER_ADMIN").Select(item => item.Id).SingleAsync();
        var response = await client.PostAsJsonAsync("/api/v1/access/users", new
        {
            email = $"platform-attempt-{Guid.NewGuid():N}@silame.local",
            displayName = "Platform Attempt",
            applications = new[] { "CLOUD" },
            organizationId,
            roleIds = new[] { platformRoleId },
        });
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        Assert.Contains("PROTECTED_ROLE_ASSIGNMENT_DENIED", await response.Content.ReadAsStringAsync());
    }

    [Fact]
    public async Task Deny_override_wins_over_multiple_role_grants()
    {
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<SilaMeDbContext>();
        var access = scope.ServiceProvider.GetRequiredService<IAccessService>();
        var user = await db.Users.SingleAsync(item => item.Email == CloudEmail);
        var organizationId = await db.Organizations.Where(item => item.Code == TestOrganizationCode).Select(item => item.Id).SingleAsync();
        var permission = await db.Permissions.SingleAsync(item => item.Key == PermissionKeys.UserManage);
        var role = new Role
        {
            Id = Guid.NewGuid(), Key = $"TEST_GRANT_{Guid.NewGuid():N}"[..24].ToUpperInvariant(),
            Name = "Second grant role", CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow,
        };
        role.Permissions.Add(new RolePermission { RoleId = role.Id, PermissionId = permission.Id });
        db.Roles.Add(role);
        db.UserRoleAssignments.Add(new UserRoleAssignment
        {
            Id = Guid.NewGuid(), UserId = user.Id, RoleId = role.Id, OrganizationId = organizationId, CreatedAt = DateTime.UtcNow,
        });
        db.UserAuthorizationOverrides.Add(new UserAuthorizationOverride
        {
            Id = Guid.NewGuid(), UserId = user.Id, AuthorizationId = permission.Id,
            OverrideType = AuthorizationOverrideType.DENY, Reason = "Regression test", CreatedAt = DateTime.UtcNow, CreatedByUserId = user.Id,
        });
        await db.SaveChangesAsync();

        var allowed = await access.HasPermissionAsync(new Session
        {
            Id = Guid.NewGuid(), UserId = user.Id, Application = ApplicationKind.CLOUD,
            TokenHash = "test", CreatedAt = DateTime.UtcNow, ExpiresAt = DateTime.UtcNow.AddMinutes(5), LastUsedAt = DateTime.UtcNow,
        }, PermissionKeys.UserManage, organizationId, null, CancellationToken.None);
        Assert.False(allowed);
        db.UserAuthorizationOverrides.RemoveRange(db.UserAuthorizationOverrides.Where(item => item.UserId == user.Id && item.AuthorizationId == permission.Id));
        db.UserRoleAssignments.RemoveRange(db.UserRoleAssignments.Where(item => item.UserId == user.Id && item.RoleId == role.Id));
        db.Roles.Remove(role);
        await db.SaveChangesAsync();
    }

    [Fact]
    public async Task Users_from_another_organization_are_not_listed()
    {
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<SilaMeDbContext>();
        var organization = new Organization
        {
            Id = Guid.NewGuid(), Code = $"ISOLATED-{Guid.NewGuid():N}"[..20].ToUpperInvariant(),
            Name = "Isolated customer", Kind = OrganizationKind.CUSTOMER, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow,
        };
        var user = new User
        {
            Id = Guid.NewGuid(), Email = $"isolated-{Guid.NewGuid():N}@silame.local",
            NormalizedEmail = string.Empty, DisplayName = "Isolated customer user", PasswordHash = "not-used",
            CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow,
        };
        user.NormalizedEmail = user.Email.ToUpperInvariant();
        db.Organizations.Add(organization);
        db.Users.Add(user);
        db.UserOrganizationMemberships.Add(new UserOrganizationMembership
        {
            Id = Guid.NewGuid(), UserId = user.Id, OrganizationId = organization.Id, CreatedAt = DateTime.UtcNow,
        });
        await db.SaveChangesAsync();
        scope.Dispose();

        using var client = await CloudClientAsync();
        var response = await client.GetFromJsonAsync<List<AccessUser>>("/api/v1/access/users");
        Assert.DoesNotContain(response!, item => item.Email == user.Email);
    }

    [Fact]
    public async Task Microsoft_readiness_does_not_report_connected_without_backend_configuration()
    {
        using var client = await CloudClientAsync();
        var response = await client.GetAsync("/api/v1/integrations/microsoft/readiness");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var readiness = await response.Content.ReadFromJsonAsync<MicrosoftReadiness>();
        Assert.False(readiness?.GraphIntegrationReady);
    }

    [Fact]
    public async Task Microsoft_connect_requires_server_configuration()
    {
        using var client = await CloudClientAsync();
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<SilaMeDbContext>();
        var organizationId = await db.Organizations.Where(item => item.Code == TestOrganizationCode).Select(item => item.Id).SingleAsync();
        var response = await client.PostAsJsonAsync("/api/v1/integrations/microsoft/connect", new
        {
            organizationId,
            returnUrl = "/admin/users",
            draft = new { form = new { email = "draft@example.com", password = "must-not-be-stored" } },
        });
        Assert.Equal(HttpStatusCode.ServiceUnavailable, response.StatusCode);
        Assert.Contains("MICROSOFT_CONFIGURATION_REQUIRED", await response.Content.ReadAsStringAsync());
    }

    [Fact]
    public async Task Microsoft_callback_rejects_invalid_and_replayed_state()
    {
        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });
        var response = await client.GetAsync("/api/v1/integrations/microsoft/callback?state=not-a-real-state&code=unused");
        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        Assert.Contains("expired_or_reused_state", response.Headers.Location?.ToString() ?? string.Empty);
    }

    [Fact]
    public async Task User_creation_rejects_an_unvalidated_microsoft_connection()
    {
        using var client = await CloudClientAsync();
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<SilaMeDbContext>();
        var user = await db.Users.SingleAsync(item => item.Email == CloudEmail);
        var organizationId = await db.Organizations.Where(item => item.Code == TestOrganizationCode).Select(item => item.Id).SingleAsync();
        var connection = new DocumentStorageConnection
        {
            Id = Guid.NewGuid(),
            OrganizationId = organizationId,
            Provider = DocumentStorageProvider.MICROSOFT,
            Name = $"Unvalidated Microsoft {Guid.NewGuid():N}",
            ConnectionStatus = StorageConnectionStatus.AUTHENTICATED,
            CreatedAt = DateTime.UtcNow,
            CreatedByUserId = user.Id,
            UpdatedAt = DateTime.UtcNow,
        };
        db.DocumentStorageConnections.Add(connection);
        await db.SaveChangesAsync();

        var response = await client.PostAsJsonAsync("/api/v1/access/users", new
        {
            email = $"microsoft-pending-{Guid.NewGuid():N}@silame.local",
            displayName = "Pending Microsoft User",
            applications = new[] { "CLOUD" },
            organizationId,
            storageProvider = "MICROSOFT",
            storageConnectionId = connection.Id,
            externalStorageTransferEnabled = true,
        });
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Contains("MICROSOFT_STORAGE_NOT_VALIDATED", await response.Content.ReadAsStringAsync());
    }

    private async Task<HttpClient> CloudClientAsync()
    {
        var client = factory.CreateClient(new WebApplicationFactoryClientOptions { HandleCookies = true });
        client.DefaultRequestHeaders.Add("X-Sila-Route-Slug", "sila-dev");
        var response = await client.PostAsJsonAsync("/api/auth/cloud/login", new { email = CloudEmail, password = Password });
        response.EnsureSuccessStatusCode();
        return client;
    }

    private async Task<HttpClient> MobileClientAsync()
    {
        var client = factory.CreateClient();
        client.DefaultRequestHeaders.Add("X-Sila-Route-Slug", "sila-dev");
        var response = await client.PostAsJsonAsync("/api/auth/mobile/login", new { email = MobileEmail, password = Password });
        response.EnsureSuccessStatusCode();
        var session = await response.Content.ReadFromJsonAsync<MobileSession>();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", session!.Token);
        return client;
    }

    private static User CreateUser(string email, string displayName, IPasswordService passwordService)
    {
        var user = new User
        {
            Id = Guid.NewGuid(),
            Email = email,
            NormalizedEmail = email.ToUpperInvariant(),
            DisplayName = displayName,
            PasswordHash = string.Empty,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
        };
        user.PasswordHash = passwordService.HashPassword(user, Password);
        return user;
    }

    private static UserApplicationAccess Access(User user, ApplicationKind application) => new()
    {
        Id = Guid.NewGuid(),
        UserId = user.Id,
        Application = application,
        Status = StatusKind.ACTIVE,
        CreatedAt = DateTime.UtcNow,
    };

    private static UserOrganizationMembership Membership(User user, Organization organization, OrganizationUnit? unit) => new()
    {
        Id = Guid.NewGuid(),
        UserId = user.Id,
        OrganizationId = organization.Id,
        OrganizationUnitId = unit?.Id,
        CreatedAt = DateTime.UtcNow,
    };

    private static UserRoleAssignment RoleAssignment(User user, Role role, Organization organization, OrganizationUnit? unit) => new()
    {
        Id = Guid.NewGuid(),
        UserId = user.Id,
        RoleId = role.Id,
        OrganizationId = organization.Id,
        OrganizationUnitId = unit?.Id,
        CreatedAt = DateTime.UtcNow,
    };

    private sealed record HealthResponse(string Status);
    private sealed record UserResponse(Guid Id, string DisplayName, string Email, string Application, string Status);
    private sealed record MobileSession(UserResponse User, string Token);
    private sealed record AccessContext(
        UserResponse User,
        List<OrganizationResponse> Organizations,
        List<OrganizationUnitResponse> Units,
        List<RoleResponse> Roles,
        List<PermissionResponse> Permissions);
    private sealed record AccessUser(Guid Id, string DisplayName, string Email, string Status, List<string> Applications, List<OrganizationResponse> Organizations, List<OrganizationUnitResponse> Units, List<RoleResponse> Roles);
    private sealed record MicrosoftReadiness(bool TenantConfigured, bool ClientIdConfigured, bool ClientSecretConfigured, bool RedirectUriConfigured, bool TokenEncryptionConfigured, bool GraphIntegrationReady, string? RedirectUri);
    private sealed record OrganizationResponse(Guid Id, Guid? ParentOrganizationId, string Code, string Name, string Kind, string Status);
    private sealed record OrganizationUnitResponse(Guid Id, Guid OrganizationId, Guid? ParentUnitId, string Code, string Name, string Kind, string Status);
    private sealed record PermissionResponse(Guid Id, string Key, string Name, string? Description);
    private sealed record RoleResponse(Guid Id, string Key, string Name, string? Description, bool IsSystem, string Status, List<PermissionResponse> Permissions);
}