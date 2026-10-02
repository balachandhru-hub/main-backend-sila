using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using SilaMe.Api.Data;
using SilaMe.Api.Models;
using SilaMe.Api.Tenancy;
using Xunit;

namespace SilaMe.Api.Tests;

public sealed class FiveTenantArchitectureTests(ApiFactory factory) : IClassFixture<ApiFactory>
{
    private static readonly JsonSerializerOptions Json = new() { PropertyNameCaseInsensitive = true };

    [Fact]
    public void Canonical_five_databases_and_routes_are_locked()
    {
        Assert.Equal("sila_platform", TenantOperationalDatabase.PlatformDatabaseName);
        Assert.Equal("sila_five_test", TenantOperationalDatabase.Name("five", TenantEnvironmentType.TEST));
        Assert.Equal("sila_five_prod", TenantOperationalDatabase.Name("five", TenantEnvironmentType.PRODUCTION));
        Assert.Equal("five-test", TenantOperationalDatabase.SecretReference("five", TenantEnvironmentType.TEST));
        Assert.Equal("five-prod", TenantOperationalDatabase.SecretReference("five", TenantEnvironmentType.PRODUCTION));
        Assert.Equal("TEST", TenantOperationalDatabase.EnvironmentCode(TenantEnvironmentType.TEST));
        Assert.Equal("PROD", TenantOperationalDatabase.EnvironmentCode(TenantEnvironmentType.PRODUCTION));
        Assert.Equal("five", TenantOperationalDatabase.FiveCustomerRouteSlug);
        Assert.Equal("five", TenantRouteResolver.CanonicalizeSlug("five-test"));
        Assert.Equal("five", TenantRouteResolver.SlugFromPath("/five/login"));
        Assert.Equal("five-test", TenantRouteResolver.SlugFromPath("/five-test/receiving"));
        Assert.True(FiveCustomerBootstrap.Inventory.Length >= 20);
    }

    [Fact]
    public async Task Five_and_legacy_five_test_alias_resolve_the_same_operational_database()
    {
        foreach (var slug in new[] { "five", "five-test" })
        {
            var client = factory.CreateClient();
            client.DefaultRequestHeaders.Add("X-Sila-Route-Slug", slug);
            var tenant = await client.GetFromJsonAsync<JsonElement>("/api/public/tenant", Json);
            Assert.Equal("five", tenant.GetProperty("tenantCode").GetString());
            Assert.Equal("TEST", tenant.GetProperty("environment").GetString());
            Assert.Equal("TEST", tenant.GetProperty("environmentCode").GetString());
            Assert.Equal("sila_five_test", tenant.GetProperty("databaseName").GetString());
            Assert.Equal("Five Hotels and Resorts", tenant.GetProperty("customerName").GetString());
        }
    }

    [Fact]
    public async Task Query_string_and_body_cannot_switch_the_tenant_database()
    {
        var client = factory.CreateClient();
        client.DefaultRequestHeaders.Add("X-Sila-Route-Slug", "five");
        var hijack = await client.GetFromJsonAsync<JsonElement>("/api/public/tenant?tenantId=SILA-DEV&database=sila_me&databaseName=sila_me", Json);
        Assert.Equal("TEST", hijack.GetProperty("environmentCode").GetString());
        Assert.Equal("sila_five_test", hijack.GetProperty("databaseName").GetString());

        var body = await client.PostAsJsonAsync("/api/auth/mobile/login", new
        {
            email = "mobile-test@silame.local",
            password = "test-password-strong",
            tenantId = "SILA-DEV",
            database = "sila_me",
            databaseName = "sila_me",
        });
        Assert.True(body.StatusCode is HttpStatusCode.OK or HttpStatusCode.Unauthorized);
        var after = await client.GetFromJsonAsync<JsonElement>("/api/public/tenant", Json);
        Assert.Equal("sila_five_test", after.GetProperty("databaseName").GetString());
        Assert.NotEqual("sila_me", after.GetProperty("databaseName").GetString());
    }

    [Fact]
    public async Task Unknown_route_slug_is_rejected()
    {
        var client = factory.CreateClient();
        client.DefaultRequestHeaders.Add("X-Sila-Route-Slug", "not-a-registered-customer");
        var response = await client.GetAsync("/api/public/tenant");
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Missing_customer_route_on_localhost_is_rejected()
    {
        var client = factory.CreateClient();
        client.DefaultRequestHeaders.Host = "localhost";
        var response = await client.GetAsync("/api/public/tenant");
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>(Json);
        Assert.Equal("TENANT_CONTEXT_REQUIRED", body.GetProperty("code").GetString());
    }

    [Fact]
    public async Task Unauthenticated_operational_access_on_five_test_is_rejected()
    {
        var client = factory.CreateClient();
        client.DefaultRequestHeaders.Add("X-Sila-Route-Slug", "five");
        var response = await client.GetAsync("/api/v1/master-data/suppliers?organizationId=" + Guid.NewGuid());
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Five_test_contains_linked_receiving_records_and_prod_does_not()
    {
        using var scope = factory.Services.CreateScope();
        var secrets = scope.ServiceProvider.GetRequiredService<ISecretProvider>();
        await using var testDb = new SilaMeDbContext(new DbContextOptionsBuilder<SilaMeDbContext>()
            .UseNpgsql(DatabaseUrl.Normalize(secrets.Resolve("five-test"))).Options);
        await using var prodDb = new SilaMeDbContext(new DbContextOptionsBuilder<SilaMeDbContext>()
            .UseNpgsql(DatabaseUrl.Normalize(secrets.Resolve("five-prod"))).Options);

        var organization = await testDb.Organizations.SingleAsync(item => item.Code == "FIVE");
        Assert.Equal("Five Hotels and Resorts", organization.Name);
        Assert.False(await testDb.Organizations.AnyAsync(item => item.Code == "TEST-PHASE2" || item.Code == "SILA-DEMO"));

        var supplier = await testDb.Suppliers.Include(item => item.PurchaseOrders).ThenInclude(item => item.Items)
            .FirstAsync(item => item.OrganizationId == organization.Id && item.SupplierCode == "1003430" && item.Name == "Test SBN");
        var po = supplier.PurchaseOrders.First(item => item.PoNumber == "4500003415");
        Assert.Equal(PurchaseOrderStatus.OPEN, po.Status);
        Assert.Equal(supplier.Id, po.SupplierId);
        Assert.Equal(4, po.Items.Count);

        Assert.False(await prodDb.Suppliers.AnyAsync(item => item.SupplierCode == "1003430"));
        Assert.False(await prodDb.PurchaseOrders.AnyAsync(item => item.PoNumber == "4500003415"));
        Assert.Equal("sila_five_test", new Npgsql.NpgsqlConnectionStringBuilder(testDb.Database.GetConnectionString()).Database);
        Assert.Equal("sila_five_prod", new Npgsql.NpgsqlConnectionStringBuilder(prodDb.Database.GetConnectionString()).Database);
    }

    [Fact]
    public async Task Five_test_open_po_api_returns_linked_receiving_records()
    {
        using var scope = factory.Services.CreateScope();
        var secrets = scope.ServiceProvider.GetRequiredService<ISecretProvider>();
        await using var testDb = new SilaMeDbContext(new DbContextOptionsBuilder<SilaMeDbContext>()
            .UseNpgsql(DatabaseUrl.Normalize(secrets.Resolve("five-test"))).Options);
        var organization = await testDb.Organizations.SingleAsync(item => item.Code == "FIVE");
        var supplier = await testDb.Suppliers.FirstAsync(item => item.OrganizationId == organization.Id && item.SupplierCode == "1003430");

        var client = factory.CreateClient();
        client.DefaultRequestHeaders.Add("X-Sila-Route-Slug", "five");
        var login = await client.PostAsJsonAsync("/api/auth/mobile/login", new { email = "mobile-test@silame.local", password = "test-password-strong" });
        Assert.Equal(HttpStatusCode.OK, login.StatusCode);
        using var loginDoc = JsonDocument.Parse(await login.Content.ReadAsStringAsync());
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", loginDoc.RootElement.GetProperty("token").GetString());

        var suppliers = await client.GetFromJsonAsync<JsonElement>(
            $"/api/v1/master-data/suppliers?organizationId={organization.Id}&query=1003430", Json);
        Assert.Contains(suppliers.EnumerateArray(), item => item.GetProperty("supplierCode").GetString() == "1003430"
            && item.GetProperty("name").GetString() == "Test SBN");

        var open = await client.GetFromJsonAsync<JsonElement>(
            $"/api/v1/purchase-orders/search?organizationId={organization.Id}&supplierId={supplier.Id}&openOnly=true", Json);
        var po = open.EnumerateArray().First(item => item.GetProperty("poNumber").GetString() == "4500003415");
        Assert.Equal(supplier.Id.ToString(), po.GetProperty("supplierId").GetGuid().ToString());
        Assert.Equal(4, po.GetProperty("items").GetArrayLength());

        var missing = factory.CreateClient();
        missing.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", loginDoc.RootElement.GetProperty("token").GetString());
        var missingResponse = await missing.GetAsync("/api/me");
        Assert.Equal(HttpStatusCode.Unauthorized, missingResponse.StatusCode);
    }

    [Fact]
    public async Task Production_provision_does_not_copy_demo_receiving_transactions()
    {
        var suffix = Guid.NewGuid().ToString("N")[..8];
        var platform = factory.CreateClient();
        var login = await platform.PostAsJsonAsync("/api/platform/auth/login", new { email = "platformadmin@silame.local", password = "PlatformAdmin123!" });
        Assert.Equal(HttpStatusCode.OK, login.StatusCode);

        var create = await platform.PostAsJsonAsync("/api/platform/tenants", new
        {
            tenantCode = $"FIVP{suffix}",
            customerName = "Five Isolation",
            cloudUsers = 5,
            mobileUsers = 5,
            products = new[] { "SILA_ME" },
            modules = ProductCodes.DefaultModules,
            createTest = true,
            createProduction = true,
            testRouteSlug = $"fivp-test-{suffix}",
            productionRouteSlug = $"fivp-{suffix}",
            testBaseUrl = $"http://localhost/fivp-test-{suffix}",
            productionBaseUrl = $"http://localhost/fivp-{suffix}",
            adminEmail = $"fivp-{suffix}@iso.test",
            adminDisplayName = "Admin",
            adminPassword = "AdminPass123!",
            adminApplications = new[] { "CLOUD" },
        });
        create.EnsureSuccessStatusCode();
        var payload = await create.Content.ReadFromJsonAsync<JsonElement>(Json);
        var prodSecret = payload.GetProperty("environments").EnumerateArray()
            .First(item => item.GetProperty("environmentType").GetString() == "PRODUCTION")
            .GetProperty("databaseSecretReference").GetString();
        var testSecret = payload.GetProperty("environments").EnumerateArray()
            .First(item => item.GetProperty("environmentType").GetString() == "TEST")
            .GetProperty("databaseSecretReference").GetString();
        Assert.EndsWith("-prod", prodSecret);
        Assert.EndsWith("-test", testSecret);

        using var scope = factory.Services.CreateScope();
        var secrets = scope.ServiceProvider.GetRequiredService<ISecretProvider>();
        await using (var prodDb = new SilaMeDbContext(new DbContextOptionsBuilder<SilaMeDbContext>().UseNpgsql(DatabaseUrl.Normalize(secrets.Resolve(prodSecret!))).Options))
        {
            Assert.False(await prodDb.Suppliers.AnyAsync(item => item.SupplierCode == "1003430"));
            Assert.False(await prodDb.PurchaseOrders.AnyAsync(item => item.PoNumber == "4500003415"));
        }

        await using (var testDb = new SilaMeDbContext(new DbContextOptionsBuilder<SilaMeDbContext>().UseNpgsql(DatabaseUrl.Normalize(secrets.Resolve(testSecret!))).Options))
        {
            Assert.Equal($"sila_fivp{suffix}_test", new Npgsql.NpgsqlConnectionStringBuilder(testDb.Database.GetConnectionString()).Database);
        }
    }
}
