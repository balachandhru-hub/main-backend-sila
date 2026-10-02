using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using SilaMe.Api.Data;
using Xunit;

namespace SilaMe.Api.Tests;

public sealed class FiveMobileDevelopmentTests(ApiFactory factory) : IClassFixture<ApiFactory>
{
    private static readonly JsonSerializerOptions Json = new() { PropertyNameCaseInsensitive = true };

    [Fact]
    public async Task Mobile_login_routes_to_five_and_binds_the_session()
    {
        var client = factory.CreateClient();
        client.DefaultRequestHeaders.Add("X-Sila-Route-Slug", "five");
        var login = await client.PostAsJsonAsync("/api/auth/mobile/login", new { email = "mobile-test@silame.local", password = "test-password-strong" });
        Assert.Equal(HttpStatusCode.OK, login.StatusCode);
        using var loginDoc = JsonDocument.Parse(await login.Content.ReadAsStringAsync());
        var token = loginDoc.RootElement.GetProperty("token").GetString();
        Assert.False(string.IsNullOrWhiteSpace(token));
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var me = await client.GetFromJsonAsync<JsonElement>("/api/me", Json);
        Assert.Equal("five", me.GetProperty("tenantCode").GetString());
        Assert.Equal("Five Hotels and Resorts", me.GetProperty("customerName").GetString());
        Assert.Equal("TEST", me.GetProperty("environment").GetString());
        Assert.Equal("MOBILE", me.GetProperty("application").GetString());
        var products = me.GetProperty("productEntitlements").EnumerateArray().Select(item => item.GetString()).ToHashSet();
        Assert.Contains("SILA_ME", products);

        var tenant = await client.GetFromJsonAsync<JsonElement>("/api/public/tenant", Json);
        Assert.Equal("sila_five_test", tenant.GetProperty("databaseName").GetString());
    }

    [Fact]
    public async Task Five_mobile_token_cannot_switch_to_sila_dev_or_missing_slug()
    {
        var loginClient = factory.CreateClient();
        loginClient.DefaultRequestHeaders.Add("X-Sila-Route-Slug", "five");
        var login = await loginClient.PostAsJsonAsync("/api/auth/mobile/login", new { email = "mobile-test@silame.local", password = "test-password-strong" });
        login.EnsureSuccessStatusCode();
        var token = (await login.Content.ReadFromJsonAsync<JsonElement>(Json)).GetProperty("token").GetString();

        var alias = factory.CreateClient();
        alias.DefaultRequestHeaders.Add("X-Sila-Route-Slug", "five-test");
        alias.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        Assert.Equal(HttpStatusCode.OK, (await alias.GetAsync("/api/me")).StatusCode);

        var legacy = factory.CreateClient();
        legacy.DefaultRequestHeaders.Add("X-Sila-Route-Slug", "sila-dev");
        legacy.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        Assert.Equal(HttpStatusCode.Unauthorized, (await legacy.GetAsync("/api/me")).StatusCode);

        var missing = factory.CreateClient();
        missing.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        var missingResponse = await missing.GetAsync("/api/me");
        Assert.Equal(HttpStatusCode.Unauthorized, missingResponse.StatusCode);
        var missingBody = await missingResponse.Content.ReadFromJsonAsync<JsonElement>(Json);
        Assert.Equal("TENANT_CONTEXT_REQUIRED", missingBody.GetProperty("code").GetString());

        var accepted = factory.CreateClient();
        accepted.DefaultRequestHeaders.Add("X-Sila-Route-Slug", "five");
        accepted.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        Assert.Equal(HttpStatusCode.OK, (await accepted.GetAsync("/api/me")).StatusCode);

        var unroutedLogin = factory.CreateClient();
        var unrouted = await unroutedLogin.PostAsJsonAsync("/api/auth/mobile/login", new { email = "mobile-test@silame.local", password = "test-password-strong" });
        Assert.Equal(HttpStatusCode.Unauthorized, unrouted.StatusCode);
        Assert.Equal("TENANT_CONTEXT_REQUIRED", (await unrouted.Content.ReadFromJsonAsync<JsonElement>(Json)).GetProperty("code").GetString());
    }

    [Fact]
    public async Task Five_mobile_can_read_supplier_1003430_and_open_po_4500003415()
    {
        using var scope = factory.Services.CreateScope();
        var secrets = scope.ServiceProvider.GetRequiredService<SilaMe.Api.Tenancy.ISecretProvider>();
        await using var testDb = new SilaMeDbContext(new DbContextOptionsBuilder<SilaMeDbContext>()
            .UseNpgsql(SilaMe.Api.Data.DatabaseUrl.Normalize(secrets.Resolve("five-test"))).Options);
        var organization = await testDb.Organizations.SingleAsync(item => item.Code == "FIVE");
        var supplier = await testDb.Suppliers.FirstAsync(item => item.OrganizationId == organization.Id && item.SupplierCode == "1003430");
        Assert.Equal("Test SBN", supplier.Name);
        Assert.Equal("sila_five_test", new Npgsql.NpgsqlConnectionStringBuilder(testDb.Database.GetConnectionString()).Database);

        var client = factory.CreateClient();
        client.DefaultRequestHeaders.Add("X-Sila-Route-Slug", "five");
        var login = await client.PostAsJsonAsync("/api/auth/mobile/login", new { email = "mobile-test@silame.local", password = "test-password-strong" });
        Assert.Equal(HttpStatusCode.OK, login.StatusCode);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer",
            (await login.Content.ReadFromJsonAsync<JsonElement>(Json)).GetProperty("token").GetString());

        var suppliers = await client.GetFromJsonAsync<JsonElement>(
            $"/api/v1/master-data/suppliers?organizationId={organization.Id}&query=1003430", Json);
        Assert.Contains(suppliers.EnumerateArray(), item => item.GetProperty("supplierCode").GetString() == "1003430"
            && item.GetProperty("name").GetString() == "Test SBN");

        var open = await client.GetFromJsonAsync<JsonElement>(
            $"/api/v1/purchase-orders/search?organizationId={organization.Id}&supplierId={supplier.Id}&openOnly=true", Json);
        Assert.Contains(open.EnumerateArray(), item => item.GetProperty("poNumber").GetString() == "4500003415");
    }
}
