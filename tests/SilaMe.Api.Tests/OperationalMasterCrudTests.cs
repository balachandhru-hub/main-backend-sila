using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using SilaMe.Api.Data;
using Xunit;

namespace SilaMe.Api.Tests;

public sealed class OperationalMasterCrudTests(ApiFactory factory) : IClassFixture<ApiFactory>
{
    private static readonly JsonSerializerOptions Json = new() { PropertyNameCaseInsensitive = true };

    [Fact]
    public async Task Company_property_plant_storage_crud_and_hierarchy_stay_inside_five()
    {
        using var scope = factory.Services.CreateScope();
        var secrets = scope.ServiceProvider.GetRequiredService<SilaMe.Api.Tenancy.ISecretProvider>();
        await using var db = new SilaMeDbContext(new DbContextOptionsBuilder<SilaMeDbContext>()
            .UseNpgsql(SilaMe.Api.Data.DatabaseUrl.Normalize(secrets.Resolve("five-test"))).Options);
        var organization = await db.Organizations.SingleAsync(item => item.Code == "FIVE");
        Assert.Equal("sila_five_test", new Npgsql.NpgsqlConnectionStringBuilder(db.Database.GetConnectionString()).Database);

        var client = factory.CreateClient();
        client.DefaultRequestHeaders.Add("X-Sila-Route-Slug", "five");
        var login = await client.PostAsJsonAsync("/api/auth/cloud/login", new { email = "cloud-test@silame.local", password = "test-password-strong" });
        if (login.StatusCode != HttpStatusCode.OK)
            login = await client.PostAsJsonAsync("/api/auth/cloud/login", new { email = "bala@chervic.in", password = "PlatformAdmin123!" });
        Assert.Equal(HttpStatusCode.OK, login.StatusCode);

        var suffix = DateTime.UtcNow.Ticks.ToString()[^8..];
        var companyCode = $"RST{suffix}";
        var propertyCode = $"PR{suffix}";

        var company = await client.PutAsJsonAsync($"/api/v1/master-data/company-codes?organizationId={organization.Id}", new
        {
            companyCode, companyName = "Restore CRUD Co", country = "AE", currency = "AED", status = "ACTIVE",
        });
        Assert.Equal(HttpStatusCode.OK, company.StatusCode);
        var companyId = (await company.Content.ReadFromJsonAsync<JsonElement>(Json)).GetProperty("id").GetGuid();

        var property = await client.PutAsJsonAsync($"/api/v1/master-data/properties?organizationId={organization.Id}", new
        {
            propertyCode, propertyName = "Restore CRUD Property", companyCode, country = "AE", status = "ACTIVE",
        });
        Assert.Equal(HttpStatusCode.OK, property.StatusCode);
        var propertyId = (await property.Content.ReadFromJsonAsync<JsonElement>(Json)).GetProperty("id").GetGuid();

        Assert.Equal(HttpStatusCode.Gone, (await client.GetAsync($"/api/v1/master-data/plants?organizationId={organization.Id}")).StatusCode);
        Assert.Equal(HttpStatusCode.Gone, (await client.PutAsJsonAsync($"/api/v1/master-data/plants?organizationId={organization.Id}", new { plantCode = "GONE", plantName = "Gone", status = "ACTIVE" })).StatusCode);
        Assert.Equal(HttpStatusCode.Gone, (await client.GetAsync($"/api/v1/master-data/storage-locations?organizationId={organization.Id}")).StatusCode);

        Assert.Equal(HttpStatusCode.NoContent, (await client.PostAsync($"/api/v1/master-data/company-codes/{companyId}/suspend?organizationId={organization.Id}", null)).StatusCode);
        Assert.Equal(HttpStatusCode.NoContent, (await client.PostAsync($"/api/v1/master-data/company-codes/{companyId}/activate?organizationId={organization.Id}", null)).StatusCode);

        await using var otherDb = new SilaMeDbContext(new DbContextOptionsBuilder<SilaMeDbContext>()
            .UseNpgsql(SilaMe.Api.Data.DatabaseUrl.Normalize(secrets.Resolve("dev-operational"))).Options);
        Assert.False(await otherDb.CompanyCodes.AnyAsync(item => item.CompanyCode == companyCode));

        await db.Properties.Where(item => item.Id == propertyId).ExecuteDeleteAsync();
        await db.CompanyCodes.Where(item => item.Id == companyId).ExecuteDeleteAsync();
    }
}
