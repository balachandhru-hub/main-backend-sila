using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using SilaMe.Api.Data;
using SilaMe.Api.Models;
using Xunit;

namespace SilaMe.Api.Tests;

public sealed class CloudControlPlaneTests(ApiFactory factory) : IClassFixture<ApiFactory>
{
    private static readonly JsonSerializerOptions Json = new() { PropertyNameCaseInsensitive = true };

    [Fact]
    public async Task Super_admin_lists_organizations_and_creates_five_with_path_slugs()
    {
        var platform = await PlatformClientAsync();
        var suffix = Guid.NewGuid().ToString("N")[..6];
        var tenantId = $"Five-Test-{suffix}";
        var prodSlug = $"five-{suffix}";
        var testSlug = $"five-test-{suffix}";
        var create = await platform.PostAsJsonAsync("/api/platform/organizations", new
        {
            organizationName = "Five Hotels and Resorts",
            tenantId,
            address = "Dubai, Palm, JVC",
            country = "AE",
            licenseCount = 10,
            totalUsers = 100,
            productionUrl = $"https://localhost:3001/{prodSlug}",
            testUrl = $"https://localhost:3001/{testSlug}",
            adminEmail = $"five-admin-{suffix}@silame.local",
            adminDisplayName = "FIVE Admin",
            adminPassword = "FiveAdmin123!",
        });
        Assert.Equal(HttpStatusCode.OK, create.StatusCode);
        var created = await create.Content.ReadFromJsonAsync<JsonElement>(Json);
        Assert.Equal(tenantId, created.GetProperty("tenantCode").GetString());

        var list = await platform.GetFromJsonAsync<JsonElement>("/api/platform/organizations", Json);
        Assert.Contains(list.EnumerateArray(), item => item.GetProperty("tenantCode").GetString() == tenantId);
        var row = list.EnumerateArray().First(item => item.GetProperty("tenantCode").GetString() == tenantId);
        Assert.Equal("Five Hotels and Resorts", row.GetProperty("organizationName").GetString());
        Assert.Equal("Dubai, Palm, JVC", row.GetProperty("address").GetString());
        Assert.Equal(10, row.GetProperty("licenseCount").GetInt32());
        Assert.Equal(100, row.GetProperty("totalUsers").GetInt32());
        Assert.Equal($"https://localhost:3001/{prodSlug}", row.GetProperty("productionUrl").GetString());
        Assert.Equal($"https://localhost:3001/{testSlug}", row.GetProperty("testUrl").GetString());

        var orgId = created.GetProperty("id").GetGuid();
        var edit = await platform.PutAsJsonAsync($"/api/platform/organizations/{orgId}", new { address = "Dubai, Palm Jumeirah, JVC" });
        Assert.Equal(HttpStatusCode.OK, edit.StatusCode);

        var prodClient = factory.CreateClient();
        prodClient.DefaultRequestHeaders.Add("X-Sila-Route-Slug", prodSlug);
        var prod = await prodClient.GetFromJsonAsync<JsonElement>("/api/public/tenant", Json);
        Assert.Equal("Five Hotels and Resorts", prod.GetProperty("customerName").GetString());
        Assert.Equal("PRODUCTION", prod.GetProperty("environment").GetString());

        var testClient = factory.CreateClient();
        testClient.DefaultRequestHeaders.Add("X-Sila-Route-Slug", testSlug);
        var test = await testClient.GetFromJsonAsync<JsonElement>("/api/public/tenant", Json);
        Assert.Equal("TEST", test.GetProperty("environment").GetString());
        Assert.Equal(tenantId, test.GetProperty("tenantCode").GetString());

        var login = await prodClient.PostAsJsonAsync("/api/auth/cloud/login", new { email = $"five-admin-{suffix}@silame.local", password = "FiveAdmin123!" });
        Assert.Equal(HttpStatusCode.OK, login.StatusCode);

        var prodEnvId = created.GetProperty("environments").EnumerateArray().First(item => item.GetProperty("environmentType").GetString() == "PRODUCTION").GetProperty("id").GetGuid();
        var testEnvId = created.GetProperty("environments").EnumerateArray().First(item => item.GetProperty("environmentType").GetString() == "TEST").GetProperty("id").GetGuid();
        var launchProd = await platform.PostAsync($"/api/platform/tenants/{orgId}/environments/{prodEnvId}/launch", null);
        Assert.Equal(HttpStatusCode.OK, launchProd.StatusCode);
        var launchBody = await launchProd.Content.ReadFromJsonAsync<JsonElement>(Json);
        var redirect = launchBody.GetProperty("redirectUrl").GetString();
        Assert.Contains($"/{prodSlug}/platform-launch?code=", redirect);

        var code = redirect!.Split("code=").Last();
        var consume = await prodClient.PostAsJsonAsync("/api/auth/launch", new { code });
        Assert.Equal(HttpStatusCode.OK, consume.StatusCode);

        var suspend = await platform.PostAsync($"/api/platform/tenants/{orgId}/suspend", null);
        Assert.Equal(HttpStatusCode.NoContent, suspend.StatusCode);
        var blocked = await testClient.GetAsync("/api/public/tenant");
        Assert.Equal(HttpStatusCode.Forbidden, blocked.StatusCode);

        var stillListed = await platform.GetFromJsonAsync<JsonElement>("/api/platform/organizations", Json);
        Assert.Contains(stillListed.EnumerateArray(), item => item.GetProperty("id").GetGuid() == orgId);

        var reactivate = await platform.PostAsync($"/api/platform/tenants/{orgId}/reactivate", null);
        Assert.Equal(HttpStatusCode.NoContent, reactivate.StatusCode);
        var restored = await testClient.GetAsync("/api/public/tenant");
        Assert.Equal(HttpStatusCode.OK, restored.StatusCode);

        var archive = await platform.DeleteAsync($"/api/platform/organizations/{orgId}");
        Assert.Equal(HttpStatusCode.NoContent, archive.StatusCode);
        var afterArchive = await platform.GetFromJsonAsync<JsonElement>("/api/platform/organizations", Json);
        Assert.DoesNotContain(afterArchive.EnumerateArray(), item => item.GetProperty("id").GetGuid() == orgId);
    }

    [Fact]
    public async Task Support_consultant_sees_only_assigned_organization()
    {
        var platform = await PlatformClientAsync();
        var suffix = Guid.NewGuid().ToString("N")[..6];
        var created = await platform.PostAsJsonAsync("/api/platform/organizations", new
        {
            organizationName = "Assigned Org",
            tenantId = $"Asg-{suffix}",
            licenseCount = 2,
            totalUsers = 5,
            testUrl = $"https://localhost:3001/asg-test-{suffix}",
            adminEmail = $"asg-{suffix}@silame.local",
            adminPassword = "FiveAdmin123!",
        });
        created.EnsureSuccessStatusCode();
        var org = await created.Content.ReadFromJsonAsync<JsonElement>(Json);
        var orgId = org.GetProperty("id").GetGuid();
        var user = await platform.PostAsJsonAsync("/api/platform/users", new
        {
            firstName = "Aman",
            lastName = "Support",
            email = $"aman-{suffix}@silame.local",
            displayName = "Aman",
            password = "Consultant123!",
            roleKey = "CUSTOMER_SUPPORT_CONSULTANT",
            assignedOrganizationId = orgId,
            environmentAccess = "TEST",
        });
        Assert.Equal(HttpStatusCode.OK, user.StatusCode);

        var consultant = factory.CreateClient();
        var login = await consultant.PostAsJsonAsync("/api/platform/auth/login", new { email = $"aman-{suffix}@silame.local", password = "Consultant123!" });
        Assert.Equal(HttpStatusCode.OK, login.StatusCode);
        var rows = await consultant.GetFromJsonAsync<JsonElement>("/api/platform/organizations", Json);
        Assert.Equal(1, rows.GetArrayLength());
        Assert.Equal(orgId, rows[0].GetProperty("id").GetGuid());

        var prodEnv = org.GetProperty("environments").EnumerateArray().FirstOrDefault(item => item.GetProperty("environmentType").GetString() == "PRODUCTION");
        if (prodEnv.ValueKind == JsonValueKind.Object)
        {
            var denied = await consultant.PostAsync($"/api/platform/tenants/{orgId}/environments/{prodEnv.GetProperty("id").GetGuid()}/launch", null);
            Assert.Equal(HttpStatusCode.Forbidden, denied.StatusCode);
        }
    }

    [Fact]
    public async Task Bala_has_platform_super_admin_access()
    {
        using var scope = factory.Services.CreateScope();
        var platform = scope.ServiceProvider.GetRequiredService<PlatformDbContext>();
        var user = await platform.PlatformUsers
            .Include(item => item.Roles).ThenInclude(item => item.Role)
            .SingleOrDefaultAsync(item => item.NormalizedEmail == "BALA@CHERVIC.IN");
        Assert.NotNull(user);
        Assert.Equal(PlatformUserStatus.ACTIVE, user!.Status);
        Assert.Contains(user.Roles, item => item.Role.Key == "PLATFORM_SUPER_ADMIN");

        var client = factory.CreateClient();
        var denied = await client.GetAsync("/api/platform/organizations");
        Assert.Equal(HttpStatusCode.Unauthorized, denied.StatusCode);
    }

    private async Task<HttpClient> PlatformClientAsync()
    {
        var client = factory.CreateClient();
        var login = await client.PostAsJsonAsync("/api/platform/auth/login", new { email = "platformadmin@silame.local", password = "PlatformAdmin123!" });
        Assert.Equal(HttpStatusCode.OK, login.StatusCode);
        return client;
    }
}
