using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using SilaMe.Api.Data;
using SilaMe.Api.Models;
using Xunit;

namespace SilaMe.Api.Tests;

public sealed class FiveCustomerLaunchHandoffTests(ApiFactory factory) : IClassFixture<ApiFactory>
{
    private static readonly JsonSerializerOptions Json = new() { PropertyNameCaseInsensitive = true };

    [Fact]
    public async Task Direct_five_and_legacy_alias_login_share_sila_five_test()
    {
        var five = factory.CreateClient();
        five.DefaultRequestHeaders.Add("X-Sila-Route-Slug", "five");
        var fiveTenant = await five.GetFromJsonAsync<JsonElement>("/api/public/tenant", Json);
        Assert.Equal("five", fiveTenant.GetProperty("tenantCode").GetString());
        Assert.Equal("TEST", fiveTenant.GetProperty("environment").GetString());
        Assert.Equal("sila_five_test", fiveTenant.GetProperty("databaseName").GetString());
        Assert.Equal("Five Hotels and Resorts", fiveTenant.GetProperty("customerName").GetString());

        var fiveLogin = await five.PostAsJsonAsync("/api/auth/cloud/login", new { email = "cloud-test@silame.local", password = "test-password-strong" });
        if (fiveLogin.StatusCode != HttpStatusCode.OK)
        {
            fiveLogin = await five.PostAsJsonAsync("/api/auth/cloud/login", new { email = "bala@chervic.in", password = "PlatformAdmin123!" });
        }
        Assert.Equal(HttpStatusCode.OK, fiveLogin.StatusCode);
        var fiveMe = await five.GetFromJsonAsync<JsonElement>("/api/me", Json);
        Assert.Equal("CUSTOMER_USER", fiveMe.GetProperty("actorType").GetString());
        Assert.False(fiveMe.GetProperty("supportSession").GetBoolean());

        var alias = factory.CreateClient();
        alias.DefaultRequestHeaders.Add("X-Sila-Route-Slug", "five-test");
        var aliasTenant = await alias.GetFromJsonAsync<JsonElement>("/api/public/tenant", Json);
        Assert.Equal("sila_five_test", aliasTenant.GetProperty("databaseName").GetString());
        CopyCookies(fiveLogin, alias);
        Assert.Equal(HttpStatusCode.OK, (await alias.GetAsync("/api/me")).StatusCode);

        var legacy = factory.CreateClient();
        legacy.DefaultRequestHeaders.Add("X-Sila-Route-Slug", "sila-dev");
        CopyCookies(fiveLogin, legacy);
        Assert.Equal(HttpStatusCode.Unauthorized, (await legacy.GetAsync("/api/me")).StatusCode);
    }

    [Fact]
    public async Task Super_admin_launches_five_with_one_time_codes()
    {
        var platform = await PlatformClientAsync();
        var five = await LoadFiveAsync(platform);
        var testEnv = EnvironmentId(five, "TEST");
        var prodEnv = EnvironmentId(five, "PRODUCTION");

        var testLaunch = await platform.PostAsync($"/api/platform/tenants/{five.GetProperty("id").GetGuid()}/environments/{testEnv}/launch", null);
        Assert.Equal(HttpStatusCode.OK, testLaunch.StatusCode);
        var testRedirect = (await testLaunch.Content.ReadFromJsonAsync<JsonElement>(Json)).GetProperty("redirectUrl").GetString();
        Assert.Contains("/five/platform-launch?code=", testRedirect);
        Assert.DoesNotContain("five-test", testRedirect, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("session", testRedirect, StringComparison.OrdinalIgnoreCase);
        var testCode = testRedirect!.Split("code=").Last();

        var testClient = factory.CreateClient();
        testClient.DefaultRequestHeaders.Add("X-Sila-Route-Slug", "five");
        var consumeTest = await testClient.PostAsJsonAsync("/api/auth/launch", new { code = testCode });
        Assert.Equal(HttpStatusCode.OK, consumeTest.StatusCode);
        var testUser = await consumeTest.Content.ReadFromJsonAsync<JsonElement>(Json);
        Assert.True(testUser.GetProperty("user").GetProperty("supportSession").GetBoolean());
        Assert.Equal("PLATFORM_USER", testUser.GetProperty("user").GetProperty("actorType").GetString());
        Assert.Equal("PLATFORM_SUPER_ADMIN", testUser.GetProperty("user").GetProperty("platformRole").GetString());
        Assert.Equal("five", testUser.GetProperty("user").GetProperty("tenantCode").GetString());

        var replay = await testClient.PostAsJsonAsync("/api/auth/launch", new { code = testCode });
        Assert.Equal(HttpStatusCode.Forbidden, replay.StatusCode);
        var replayBody = await replay.Content.ReadFromJsonAsync<JsonElement>(Json);
        Assert.Equal("TENANT_LAUNCH_ALREADY_USED", replayBody.GetProperty("code").GetString());

        var prodLaunch = await platform.PostAsync($"/api/platform/tenants/{five.GetProperty("id").GetGuid()}/environments/{prodEnv}/launch", null);
        Assert.Equal(HttpStatusCode.OK, prodLaunch.StatusCode);
        var prodRedirect = (await prodLaunch.Content.ReadFromJsonAsync<JsonElement>(Json)).GetProperty("redirectUrl").GetString();
        var prodCode = prodRedirect!.Split("code=").Last();

        var prodOnCustomerRoute = factory.CreateClient();
        prodOnCustomerRoute.DefaultRequestHeaders.Add("X-Sila-Route-Slug", "five");
        var consumeProd = await prodOnCustomerRoute.PostAsJsonAsync("/api/auth/launch", new { code = prodCode });
        Assert.Equal(HttpStatusCode.Forbidden, consumeProd.StatusCode);

        var unusedTest = await platform.PostAsync($"/api/platform/tenants/{five.GetProperty("id").GetGuid()}/environments/{testEnv}/launch", null);
        unusedTest.EnsureSuccessStatusCode();
        var unusedCode = (await unusedTest.Content.ReadFromJsonAsync<JsonElement>(Json)).GetProperty("redirectUrl").GetString()!.Split("code=").Last();
        var wrongTenant = factory.CreateClient();
        wrongTenant.DefaultRequestHeaders.Add("X-Sila-Route-Slug", "sila-dev");
        var wrongEnv = await wrongTenant.PostAsJsonAsync("/api/auth/launch", new { code = unusedCode });
        Assert.Equal(HttpStatusCode.Forbidden, wrongEnv.StatusCode);

        var ended = await testClient.PostAsync("/api/auth/support/end", null);
        Assert.Equal(HttpStatusCode.NoContent, ended.StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await testClient.GetAsync("/api/me")).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await platform.GetAsync("/api/platform/session")).StatusCode);

        var audit = await platform.GetFromJsonAsync<JsonElement>("/api/platform/audit", Json);
        var actions = audit.EnumerateArray().Select(item => item.TryGetProperty("action", out var action) ? action.GetString() : item.GetProperty("Action").GetString()).ToHashSet();
        Assert.Contains("PLATFORM_CUSTOMER_LAUNCH_REQUESTED", actions);
        Assert.Contains("PLATFORM_CUSTOMER_LAUNCH_SUCCEEDED", actions);
        Assert.Contains("PLATFORM_CUSTOMER_SESSION_ENDED", actions);
        Assert.DoesNotContain(audit.EnumerateArray(), item => item.GetRawText().Contains(testCode, StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public async Task Expired_and_unknown_launch_codes_are_rejected()
    {
        var platform = await PlatformClientAsync();
        var five = await LoadFiveAsync(platform);
        var testEnv = EnvironmentId(five, "TEST");
        var launch = await platform.PostAsync($"/api/platform/tenants/{five.GetProperty("id").GetGuid()}/environments/{testEnv}/launch", null);
        launch.EnsureSuccessStatusCode();
        var redirect = (await launch.Content.ReadFromJsonAsync<JsonElement>(Json)).GetProperty("redirectUrl").GetString();
        var code = redirect!.Split("code=").Last();

        using (var scope = factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<PlatformDbContext>();
            var hash = Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(System.Text.Encoding.UTF8.GetBytes(code)));
            var row = await db.TenantLaunchAuthorizations.SingleAsync(item => item.CodeHash == hash);
            row.ExpiresAt = DateTime.UtcNow.AddSeconds(-1);
            await db.SaveChangesAsync();
        }

        var client = factory.CreateClient();
        client.DefaultRequestHeaders.Add("X-Sila-Route-Slug", "five");
        var expired = await client.PostAsJsonAsync("/api/auth/launch", new { code });
        Assert.Equal(HttpStatusCode.Forbidden, expired.StatusCode);
        Assert.Equal("TENANT_LAUNCH_EXPIRED", (await expired.Content.ReadFromJsonAsync<JsonElement>(Json)).GetProperty("code").GetString());

        var unknown = await client.PostAsJsonAsync("/api/auth/launch", new { code = Convert.ToHexString(System.Security.Cryptography.RandomNumberGenerator.GetBytes(32)) });
        Assert.Equal(HttpStatusCode.Forbidden, unknown.StatusCode);
        Assert.Equal("TENANT_LAUNCH_INVALID", (await unknown.Content.ReadFromJsonAsync<JsonElement>(Json)).GetProperty("code").GetString());
    }

    [Fact]
    public async Task Support_roles_require_assignment_and_environment_scope()
    {
        var platform = await PlatformClientAsync();
        var five = await LoadFiveAsync(platform);
        var fiveId = five.GetProperty("id").GetGuid();
        var testEnv = EnvironmentId(five, "TEST");
        var prodEnv = EnvironmentId(five, "PRODUCTION");
        var suffix = Guid.NewGuid().ToString("N")[..6];

        var outsider = await platform.PostAsJsonAsync("/api/platform/users", new
        {
            email = $"consultant-out-{suffix}@silame.local",
            displayName = "Outsider",
            password = "Consultant123!",
            roleKey = "CUSTOMER_SUPPORT_CONSULTANT",
        });
        outsider.EnsureSuccessStatusCode();
        var outsiderClient = factory.CreateClient();
        (await outsiderClient.PostAsJsonAsync("/api/platform/auth/login", new { email = $"consultant-out-{suffix}@silame.local", password = "Consultant123!" })).EnsureSuccessStatusCode();
        var denied = await outsiderClient.PostAsync($"/api/platform/tenants/{fiveId}/environments/{testEnv}/launch", null);
        Assert.Equal(HttpStatusCode.Forbidden, denied.StatusCode);

        var consultant = await platform.PostAsJsonAsync("/api/platform/users", new
        {
            email = $"consultant-five-{suffix}@silame.local",
            displayName = "Five Consultant",
            password = "Consultant123!",
            roleKey = "CUSTOMER_SUPPORT_CONSULTANT",
            assignedOrganizationId = fiveId,
            environmentAccess = "TEST",
        });
        consultant.EnsureSuccessStatusCode();
        var consultantClient = factory.CreateClient();
        (await consultantClient.PostAsJsonAsync("/api/platform/auth/login", new { email = $"consultant-five-{suffix}@silame.local", password = "Consultant123!" })).EnsureSuccessStatusCode();
        var allowed = await consultantClient.PostAsync($"/api/platform/tenants/{fiveId}/environments/{testEnv}/launch", null);
        Assert.Equal(HttpStatusCode.OK, allowed.StatusCode);
        var blockedProd = await consultantClient.PostAsync($"/api/platform/tenants/{fiveId}/environments/{prodEnv}/launch", null);
        Assert.Equal(HttpStatusCode.Forbidden, blockedProd.StatusCode);

        var admin = await platform.PostAsJsonAsync("/api/platform/users", new
        {
            email = $"support-admin-{suffix}@silame.local",
            displayName = "Support Admin",
            password = "SupportAdmin123!",
            roleKey = "CUSTOMER_SUPPORT_ADMIN",
            assignedOrganizationId = fiveId,
            environmentAccess = "BOTH",
        });
        admin.EnsureSuccessStatusCode();
        var adminClient = factory.CreateClient();
        (await adminClient.PostAsJsonAsync("/api/platform/auth/login", new { email = $"support-admin-{suffix}@silame.local", password = "SupportAdmin123!" })).EnsureSuccessStatusCode();
        Assert.Equal(HttpStatusCode.OK, (await adminClient.PostAsync($"/api/platform/tenants/{fiveId}/environments/{testEnv}/launch", null)).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await adminClient.PostAsync($"/api/platform/tenants/{fiveId}/environments/{prodEnv}/launch", null)).StatusCode);
    }

    [Fact]
    public async Task Suspended_customer_blocks_direct_login_and_normal_launch()
    {
        var platform = await PlatformClientAsync();
        var suffix = Guid.NewGuid().ToString("N")[..6];
        var created = await platform.PostAsJsonAsync("/api/platform/organizations", new
        {
            organizationName = "Suspend Launch Org",
            tenantId = $"SusL-{suffix}",
            licenseCount = 2,
            totalUsers = 4,
            testUrl = $"https://localhost:3001/susl-test-{suffix}",
            adminEmail = $"susl-{suffix}@silame.local",
            adminPassword = "FiveAdmin123!",
        });
        created.EnsureSuccessStatusCode();
        var org = await created.Content.ReadFromJsonAsync<JsonElement>(Json);
        var orgId = org.GetProperty("id").GetGuid();
        var testEnv = org.GetProperty("environments").EnumerateArray().First(item => item.GetProperty("environmentType").GetString() == "TEST").GetProperty("id").GetGuid();
        var slug = $"susl-test-{suffix}";

        var suspend = await platform.PostAsync($"/api/platform/tenants/{orgId}/suspend", null);
        Assert.Equal(HttpStatusCode.NoContent, suspend.StatusCode);

        var client = factory.CreateClient();
        client.DefaultRequestHeaders.Add("X-Sila-Route-Slug", slug);
        var login = await client.PostAsJsonAsync("/api/auth/cloud/login", new { email = $"susl-{suffix}@silame.local", password = "FiveAdmin123!" });
        Assert.Equal(HttpStatusCode.Forbidden, login.StatusCode);

        var launch = await platform.PostAsync($"/api/platform/tenants/{orgId}/environments/{testEnv}/launch", null);
        Assert.Equal(HttpStatusCode.Forbidden, launch.StatusCode);
        Assert.Equal("TENANT_SUSPENDED", (await launch.Content.ReadFromJsonAsync<JsonElement>(Json)).GetProperty("code").GetString());

        await platform.PostAsync($"/api/platform/tenants/{orgId}/reactivate", null);
        await platform.DeleteAsync($"/api/platform/organizations/{orgId}");
    }

    [Fact]
    public async Task Cross_origin_state_changing_cloud_action_is_rejected()
    {
        var client = factory.CreateClient();
        client.DefaultRequestHeaders.Add("X-Sila-Route-Slug", "five");
        client.DefaultRequestHeaders.Add("Origin", "https://evil.example");
        var response = await client.PostAsJsonAsync("/api/auth/cloud/login", new { email = "cloud-test@silame.local", password = "test-password-strong" });
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    private async Task<HttpClient> PlatformClientAsync()
    {
        var client = factory.CreateClient();
        var login = await client.PostAsJsonAsync("/api/platform/auth/login", new { email = "platformadmin@silame.local", password = "PlatformAdmin123!" });
        Assert.Equal(HttpStatusCode.OK, login.StatusCode);
        return client;
    }

    private static async Task<JsonElement> LoadFiveAsync(HttpClient platform)
    {
        var rows = await platform.GetFromJsonAsync<JsonElement>("/api/platform/organizations", Json);
        return rows.EnumerateArray().First(item => string.Equals(item.GetProperty("tenantCode").GetString(), "five", StringComparison.OrdinalIgnoreCase));
    }

    private static Guid EnvironmentId(JsonElement tenant, string environmentType)
    {
        if (tenant.TryGetProperty("testEnvironmentId", out var testId) && environmentType == "TEST" && testId.ValueKind != JsonValueKind.Null)
        {
            return testId.GetGuid();
        }

        if (tenant.TryGetProperty("productionEnvironmentId", out var prodId) && environmentType == "PRODUCTION" && prodId.ValueKind != JsonValueKind.Null)
        {
            return prodId.GetGuid();
        }

        throw new InvalidOperationException($"Missing {environmentType} environment on FIVE.");
    }

    private static void CopyCookies(HttpResponseMessage source, HttpClient target)
    {
        if (!source.Headers.TryGetValues("Set-Cookie", out var cookies))
        {
            return;
        }

        foreach (var cookie in cookies)
        {
            target.DefaultRequestHeaders.Add("Cookie", cookie.Split(';')[0]);
        }
    }
}
