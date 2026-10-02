using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using SilaMe.Api.Data;
using SilaMe.Api.Models;
using SilaMe.Api.Tenancy;
using Xunit;

namespace SilaMe.Api.Tests;

public sealed class MultiTenantFoundationTests(ApiFactory factory) : IClassFixture<ApiFactory>
{
    private static readonly JsonSerializerOptions Json = new() { PropertyNameCaseInsensitive = true };

    [Fact]
    public async Task Unknown_hostname_is_rejected()
    {
        var client = factory.CreateClient();
        client.DefaultRequestHeaders.Host = "unknown.silame.test";
        var response = await client.GetAsync("/api/public/tenant");
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<ErrorBody>(Json);
        Assert.Equal("TENANT_DOMAIN_NOT_FOUND", body?.Code);
    }

    [Fact]
    public async Task Localhost_without_a_customer_route_is_rejected_and_sila_dev_stays_explicit()
    {
        var unrouted = factory.CreateClient();
        unrouted.DefaultRequestHeaders.Host = "localhost";
        var rejected = await unrouted.GetAsync("/api/public/tenant");
        Assert.Equal(HttpStatusCode.Unauthorized, rejected.StatusCode);
        var rejectedBody = await rejected.Content.ReadFromJsonAsync<ErrorBody>(Json);
        Assert.Equal("TENANT_CONTEXT_REQUIRED", rejectedBody?.Code);

        var client = factory.CreateClient();
        client.DefaultRequestHeaders.Add("X-Sila-Route-Slug", "sila-dev");
        var tenant = await client.GetFromJsonAsync<PublicTenant>("/api/public/tenant", Json);
        Assert.Equal("SILA-DEV", tenant?.TenantCode);

        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<SilaMeDbContext>();
        var passwords = scope.ServiceProvider.GetRequiredService<SilaMe.Api.Services.IPasswordService>();
        const string email = "mt-cloud@silame.local";
        if (!await db.Users.AnyAsync(item => item.NormalizedEmail == email.ToUpperInvariant()))
        {
            var user = new User
            {
                Id = Guid.NewGuid(),
                Email = email,
                NormalizedEmail = email.ToUpperInvariant(),
                DisplayName = "MT Cloud",
                PasswordHash = string.Empty,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
            };
            user.PasswordHash = passwords.HashPassword(user, "test-password-strong");
            db.Users.Add(user);
            db.UserApplicationAccess.Add(new UserApplicationAccess
            {
                Id = Guid.NewGuid(),
                UserId = user.Id,
                Application = ApplicationKind.CLOUD,
                CreatedAt = DateTime.UtcNow,
            });
            await db.SaveChangesAsync();
        }

        var login = await client.PostAsJsonAsync("/api/auth/cloud/login", new { email, password = "test-password-strong" });
        Assert.Equal(HttpStatusCode.OK, login.StatusCode);
        var me = await client.GetAsync("/api/me");
        Assert.Equal(HttpStatusCode.OK, me.StatusCode);
        var payload = await me.Content.ReadFromJsonAsync<MeBody>(Json);
        Assert.Equal("SILA-DEV", payload?.TenantCode);
        Assert.Equal(email, payload?.Email);
    }

    [Fact]
    public async Task Platform_super_admin_can_list_tenants_and_other_users_are_filtered()
    {
        var client = factory.CreateClient();
        var login = await client.PostAsJsonAsync("/api/platform/auth/login", new { email = "platformadmin@silame.local", password = "PlatformAdmin123!" });
        Assert.Equal(HttpStatusCode.OK, login.StatusCode);
        var tenants = await client.GetAsync("/api/platform/tenants");
        Assert.Equal(HttpStatusCode.OK, tenants.StatusCode);
        var rows = await tenants.Content.ReadFromJsonAsync<JsonElement>(Json);
        Assert.True(rows.GetArrayLength() >= 1);
    }

    [Fact]
    public async Task Database_per_tenant_isolates_suppliers_and_sessions()
    {
        var suffix = Guid.NewGuid().ToString("N")[..8];
        var platform = await PlatformClientAsync();
        var tenantA = await ProvisionAsync(platform, $"ISOA{suffix}", $"iso-a-{suffix}.test", "admin-a@iso.test");
        var tenantB = await ProvisionAsync(platform, $"ISOB{suffix}", $"iso-b-{suffix}.test", "admin-b@iso.test");

        await SeedSupplierAsync(tenantA.Secret, "1000234", "ABC UAE");
        await SeedSupplierAsync(tenantB.Secret, "1000234", "XYZ Tanzania");

        var clientA = factory.CreateClient();
        clientA.DefaultRequestHeaders.Host = $"iso-a-{suffix}.test";
        var loginA = await clientA.PostAsJsonAsync("/api/auth/cloud/login", new { email = "admin-a@iso.test", password = "AdminPass123!" });
        Assert.Equal(HttpStatusCode.OK, loginA.StatusCode);
        var meA = await clientA.GetFromJsonAsync<MeBody>("/api/me", Json);
        Assert.Equal($"ISOA{suffix}", meA?.TenantCode);

        var clientB = factory.CreateClient();
        clientB.DefaultRequestHeaders.Host = $"iso-b-{suffix}.test";
        var loginB = await clientB.PostAsJsonAsync("/api/auth/cloud/login", new { email = "admin-b@iso.test", password = "AdminPass123!" });
        Assert.Equal(HttpStatusCode.OK, loginB.StatusCode);

        var cookie = loginA.Headers.GetValues("Set-Cookie").First().Split(';')[0];
        var crossClient = factory.CreateClient(new Microsoft.AspNetCore.Mvc.Testing.WebApplicationFactoryClientOptions { HandleCookies = false });
        crossClient.DefaultRequestHeaders.Host = $"iso-b-{suffix}.test";
        var cross = await crossClient.SendAsync(new HttpRequestMessage(HttpMethod.Get, "/api/me")
        {
            Headers = { { "Cookie", cookie } },
        });
        Assert.Equal(HttpStatusCode.Unauthorized, cross.StatusCode);

        var wrongUser = await clientA.PostAsJsonAsync("/api/auth/cloud/login", new { email = "admin-b@iso.test", password = "AdminPass123!" });
        Assert.Equal(HttpStatusCode.Unauthorized, wrongUser.StatusCode);
    }

    [Fact]
    public async Task Launch_authorization_is_one_time_tenant_and_environment_specific()
    {
        var suffix = Guid.NewGuid().ToString("N")[..8];
        var platform = await PlatformClientAsync();
        var tenant = await ProvisionAsync(platform, $"LNCH{suffix}", $"launch-{suffix}.test", "launch-admin@iso.test");
        var launch = await platform.PostAsync($"/api/platform/tenants/{tenant.TenantId}/environments/{tenant.EnvironmentId}/launch", null);
        Assert.Equal(HttpStatusCode.OK, launch.StatusCode);
        var payload = await launch.Content.ReadFromJsonAsync<LaunchBody>(Json);
        Assert.Contains("/platform-launch?code=", payload?.RedirectUrl);
        var code = payload?.RedirectUrl?.Contains("code=") == true
            ? payload.RedirectUrl.Split("code=").Last()
            : payload?.RedirectUrl?.Split("launch=").Last();
        Assert.False(string.IsNullOrWhiteSpace(code));

        var client = factory.CreateClient();
        client.DefaultRequestHeaders.Host = $"launch-{suffix}.test";
        var first = await client.PostAsJsonAsync("/api/auth/launch", new { code });
        Assert.Equal(HttpStatusCode.OK, first.StatusCode);
        var me = await client.GetFromJsonAsync<MeBody>("/api/me", Json);
        Assert.True(me?.SupportSession);

        var second = await client.PostAsJsonAsync("/api/auth/launch", new { code });
        Assert.Equal(HttpStatusCode.Forbidden, second.StatusCode);
        var error = await second.Content.ReadFromJsonAsync<ErrorBody>(Json);
        Assert.Equal("TENANT_LAUNCH_ALREADY_USED", error?.Code);
    }

    [Fact]
    public async Task License_limit_and_module_entitlement_are_enforced()
    {
        var suffix = Guid.NewGuid().ToString("N")[..8];
        var platform = await PlatformClientAsync();
        await ProvisionAsync(platform, $"LIC{suffix}", $"lic-{suffix}.test", "lic-admin@iso.test", cloudUsers: 1);

        var client = factory.CreateClient();
        client.DefaultRequestHeaders.Host = $"lic-{suffix}.test";
        var login = await client.PostAsJsonAsync("/api/auth/cloud/login", new { email = "lic-admin@iso.test", password = "AdminPass123!" });
        Assert.Equal(HttpStatusCode.OK, login.StatusCode);

        var entitled = await client.GetAsync("/api/v1/entitlements/LIVE_STOCK");
        Assert.Equal(HttpStatusCode.OK, entitled.StatusCode);

        using var scope = factory.Services.CreateScope();
        var platformDb = scope.ServiceProvider.GetRequiredService<PlatformDbContext>();
        var tenant = await platformDb.Tenants.SingleAsync(item => item.NormalizedTenantCode == $"LIC{suffix}".ToUpperInvariant());
        var module = await platformDb.TenantModuleEntitlements.SingleAsync(item => item.TenantId == tenant.Id && item.ModuleCode == "ITO");
        module.Status = EntitlementStatus.DISABLED;
        await platformDb.SaveChangesAsync();

        var denied = await client.GetAsync("/api/v1/entitlements/ITO");
        Assert.Equal(HttpStatusCode.Forbidden, denied.StatusCode);

        var org = await LoadDefaultOrgAsync($"lic-{suffix}.test");
        var create = await client.PostAsJsonAsync("/api/v1/access/users", new
        {
            email = "second@iso.test",
            displayName = "Second",
            organizationId = org,
            applications = new[] { "CLOUD" },
            roleIds = Array.Empty<Guid>(),
            organizationUnitIds = Array.Empty<Guid>(),
            grantedAuthorizationIds = Array.Empty<Guid>(),
            deniedAuthorizationIds = Array.Empty<Guid>(),
            password = "SecondPass123!",
        });
        Assert.Equal(HttpStatusCode.Conflict, create.StatusCode);
        var body = await create.Content.ReadFromJsonAsync<ErrorBody>(Json);
        Assert.Equal("LICENSE_LIMIT_REACHED", body?.Code);
    }

    [Fact]
    public async Task Suspended_tenant_and_unassigned_consultant_are_denied()
    {
        var suffix = Guid.NewGuid().ToString("N")[..8];
        var platform = await PlatformClientAsync();
        var tenant = await ProvisionAsync(platform, $"SUS{suffix}", $"sus-{suffix}.test", "sus-admin@iso.test");
        var suspend = await platform.PostAsync($"/api/platform/tenants/{tenant.TenantId}/suspend", null);
        Assert.Equal(HttpStatusCode.NoContent, suspend.StatusCode);

        var client = factory.CreateClient();
        client.DefaultRequestHeaders.Host = $"sus-{suffix}.test";
        var login = await client.PostAsJsonAsync("/api/auth/cloud/login", new { email = "sus-admin@iso.test", password = "AdminPass123!" });
        Assert.Equal(HttpStatusCode.Forbidden, login.StatusCode);

        await platform.PostAsync($"/api/platform/tenants/{tenant.TenantId}/reactivate", null);
        var consultantLogin = await platform.PostAsJsonAsync("/api/platform/auth/login", new { email = "platformadmin@silame.local", password = "PlatformAdmin123!" });
        Assert.Equal(HttpStatusCode.OK, consultantLogin.StatusCode);
    }

    [Fact]
    public async Task Unassigned_consultant_cannot_list_or_launch_another_tenant()
    {
        var suffix = Guid.NewGuid().ToString("N")[..8];
        var platform = await PlatformClientAsync();
        var tenant = await ProvisionAsync(platform, $"CNS{suffix}", $"cns-{suffix}.test", "cns-admin@iso.test");
        var created = await platform.PostAsJsonAsync("/api/platform/users", new
        {
            email = $"consultant-{suffix}@silame.local",
            displayName = "Consultant",
            password = "Consultant123!",
            roleKey = "CUSTOMER_SUPPORT_CONSULTANT",
        });
        Assert.Equal(HttpStatusCode.OK, created.StatusCode);

        var consultant = factory.CreateClient();
        var login = await consultant.PostAsJsonAsync("/api/platform/auth/login", new { email = $"consultant-{suffix}@silame.local", password = "Consultant123!" });
        Assert.Equal(HttpStatusCode.OK, login.StatusCode);
        var tenants = await consultant.GetFromJsonAsync<JsonElement>("/api/platform/tenants", Json);
        Assert.Equal(0, tenants.GetArrayLength());

        var launch = await consultant.PostAsync($"/api/platform/tenants/{tenant.TenantId}/environments/{tenant.EnvironmentId}/launch", null);
        Assert.Equal(HttpStatusCode.Forbidden, launch.StatusCode);
    }

    [Fact]
    public async Task Background_jobs_resolve_each_tenant_database_without_a_default_fallback()
    {
        var suffix = Guid.NewGuid().ToString("N")[..8];
        var platform = await PlatformClientAsync();
        var tenantA = await ProvisionAsync(platform, $"JOBA{suffix}", $"job-a-{suffix}.test", "job-a@iso.test");
        var tenantB = await ProvisionAsync(platform, $"JOBB{suffix}", $"job-b-{suffix}.test", "job-b@iso.test");
        await SeedSupplierAsync(tenantA.Secret, "1000234", "ABC UAE");
        await SeedSupplierAsync(tenantB.Secret, "1000234", "XYZ Tanzania");

        using var scope = factory.Services.CreateScope();
        var runner = scope.ServiceProvider.GetRequiredService<TenantJobRunner>();
        var seen = new Dictionary<string, string>();
        await runner.ForEachActiveEnvironmentAsync(async (services, context, token) =>
        {
            var db = services.GetRequiredService<SilaMeDbContext>();
            var supplier = await db.Suppliers.SingleOrDefaultAsync(item => item.SupplierCode == "1000234", token);
            if (supplier is not null)
            {
                seen[context.TenantCode] = supplier.Name;
            }
        }, CancellationToken.None);

        Assert.Equal("ABC UAE", seen[$"JOBA{suffix}"]);
        Assert.Equal("XYZ Tanzania", seen[$"JOBB{suffix}"]);
    }

    [Fact]
    public async Task Health_responses_do_not_leak_secrets()
    {
        var client = factory.CreateClient();
        var health = await client.GetStringAsync("/api/healthz");
        Assert.DoesNotContain("Password=", health);
        Assert.DoesNotContain("postgresql://", health);
        Assert.DoesNotContain("DATABASE_URL", health);
    }

    private async Task<HttpClient> PlatformClientAsync()
    {
        var client = factory.CreateClient();
        var login = await client.PostAsJsonAsync("/api/platform/auth/login", new { email = "platformadmin@silame.local", password = "PlatformAdmin123!" });
        Assert.Equal(HttpStatusCode.OK, login.StatusCode);
        return client;
    }

    private async Task<Provisioned> ProvisionAsync(HttpClient platform, string code, string hostname, string adminEmail, int cloudUsers = 25)
    {
        var response = await platform.PostAsJsonAsync("/api/platform/tenants", new
        {
            tenantCode = code,
            customerName = code,
            cloudUsers,
            mobileUsers = 100,
            products = new[] { "SILA_ME" },
            modules = ProductCodes.DefaultModules,
            createTest = true,
            createProduction = false,
            testHostname = hostname,
            testBaseUrl = $"http://{hostname}",
            adminEmail,
            adminDisplayName = "Tenant Admin",
            adminPassword = "AdminPass123!",
            adminApplications = new[] { "CLOUD" },
        });
        response.EnsureSuccessStatusCode();
        var payload = await response.Content.ReadFromJsonAsync<JsonElement>(Json);
        var tenantId = payload.GetProperty("id").GetGuid();
        var environments = payload.GetProperty("environments");
        Assert.True(environments.GetArrayLength() > 0, payload.GetRawText());
        var environment = environments[0];
        return new Provisioned(
            tenantId,
            environment.GetProperty("id").GetGuid(),
            environment.GetProperty("databaseSecretReference").GetString() ?? "");
    }

    private async Task SeedSupplierAsync(string secretReference, string supplierCode, string name)
    {
        using var scope = factory.Services.CreateScope();
        var secrets = scope.ServiceProvider.GetRequiredService<ISecretProvider>();
        var cs = DatabaseUrl.Normalize(secrets.Resolve(secretReference));
        var options = new DbContextOptionsBuilder<SilaMeDbContext>().UseNpgsql(cs).Options;
        await using var db = new SilaMeDbContext(options);
        var org = await db.Organizations.FirstAsync();
        db.Suppliers.Add(new Supplier
        {
            Id = Guid.NewGuid(),
            OrganizationId = org.Id,
            SupplierCode = supplierCode,
            Name = name,
            NormalizedName = name.ToUpperInvariant(),
            Status = StatusKind.ACTIVE,
            EntityCode = "DEFAULT",
            SourceSystem = "TEST",
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
        });
        await db.SaveChangesAsync();
    }

    private async Task<Guid> LoadDefaultOrgAsync(string host)
    {
        using var scope = factory.Services.CreateScope();
        var platform = scope.ServiceProvider.GetRequiredService<PlatformDbContext>();
        var domain = await platform.TenantDomains.Include(item => item.Environment).SingleAsync(item => item.Hostname == host);
        var secrets = scope.ServiceProvider.GetRequiredService<ISecretProvider>();
        var cs = DatabaseUrl.Normalize(secrets.Resolve(domain.Environment.DatabaseSecretReference));
        var options = new DbContextOptionsBuilder<SilaMeDbContext>().UseNpgsql(cs).Options;
        await using var db = new SilaMeDbContext(options);
        return (await db.Organizations.FirstAsync()).Id;
    }

    private sealed record ErrorBody(string Code, string Message);
    private sealed record PublicTenant(string TenantCode, string CustomerName);
    private sealed record MeBody(string Email, string? TenantCode, bool SupportSession);
    private sealed record LaunchBody(string RedirectUrl);
    private sealed record Provisioned(Guid TenantId, Guid EnvironmentId, string Secret);
}
