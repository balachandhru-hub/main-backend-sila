using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using SilaMe.Api.Data;
using SilaMe.Api.Models;
using SilaMe.Api.Services;
using Xunit;

namespace SilaMe.Api.Tests;

public sealed class OrganizationLogoTests(ApiFactory factory) : IClassFixture<ApiFactory>, IAsyncLifetime
{
    private const string AdminEmail = "logo-admin@silame.local";
    private const string ViewerEmail = "logo-viewer@silame.local";
    private const string Password = "test-password-strong";
    private const string OrganizationCode = "LOGO-ORG";
    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNameCaseInsensitive = true };

    public async Task InitializeAsync()
    {
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<SilaMeDbContext>();
        var passwordService = scope.ServiceProvider.GetRequiredService<IPasswordService>();

        await db.Users.Where(user => user.Email == AdminEmail || user.Email == ViewerEmail).ExecuteDeleteAsync();
        await db.Organizations.Where(organization => organization.Code == OrganizationCode).ExecuteDeleteAsync();

        var organization = new Organization
        {
            Id = Guid.NewGuid(),
            Code = OrganizationCode,
            Name = "Logo Test Organization",
            Kind = OrganizationKind.CUSTOMER,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
        };
        var admin = CreateUser(AdminEmail, "Logo Admin", passwordService);
        var viewer = CreateUser(ViewerEmail, "Logo Viewer", passwordService);
        var superAdmin = await db.Roles.SingleAsync(role => role.Key == "SUPER_ADMIN");
        var viewerRole = await db.Roles.SingleAsync(role => role.Key == "VIEWER");

        db.Organizations.Add(organization);
        db.Users.AddRange(admin, viewer);
        db.UserApplicationAccess.AddRange(
            new UserApplicationAccess { Id = Guid.NewGuid(), UserId = admin.Id, Application = ApplicationKind.CLOUD, Status = StatusKind.ACTIVE, CreatedAt = DateTime.UtcNow },
            new UserApplicationAccess { Id = Guid.NewGuid(), UserId = viewer.Id, Application = ApplicationKind.CLOUD, Status = StatusKind.ACTIVE, CreatedAt = DateTime.UtcNow });
        db.UserOrganizationMemberships.AddRange(
            new UserOrganizationMembership { Id = Guid.NewGuid(), UserId = admin.Id, OrganizationId = organization.Id, CreatedAt = DateTime.UtcNow },
            new UserOrganizationMembership { Id = Guid.NewGuid(), UserId = viewer.Id, OrganizationId = organization.Id, CreatedAt = DateTime.UtcNow });
        db.UserRoleAssignments.AddRange(
            new UserRoleAssignment { Id = Guid.NewGuid(), UserId = admin.Id, RoleId = superAdmin.Id, OrganizationId = organization.Id, CreatedAt = DateTime.UtcNow },
            new UserRoleAssignment { Id = Guid.NewGuid(), UserId = viewer.Id, RoleId = viewerRole.Id, OrganizationId = organization.Id, CreatedAt = DateTime.UtcNow });
        await db.SaveChangesAsync();
    }

    public Task DisposeAsync() => Task.CompletedTask;

    [Fact]
    public async Task SuperAdmin_CanUploadAndRemoveCustomerLogo()
    {
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<SilaMeDbContext>();
        var organizationId = await db.Organizations.Where(item => item.Code == OrganizationCode).Select(item => item.Id).SingleAsync();

        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions { HandleCookies = true });
        client.DefaultRequestHeaders.Add("X-Sila-Route-Slug", "sila-dev");
        Assert.True((await client.PostAsJsonAsync("/api/auth/cloud/login", new { email = AdminEmail, password = Password })).IsSuccessStatusCode);

        using var content = new MultipartFormDataContent();
        var file = new ByteArrayContent(Encoding.UTF8.GetBytes("fake-png-bytes"));
        file.Headers.ContentType = new MediaTypeHeaderValue("image/png");
        content.Add(file, "file", "customer.png");

        var upload = await client.PutAsync($"/api/v1/access/organizations/{organizationId}/logo", content);
        Assert.Equal(HttpStatusCode.OK, upload.StatusCode);
        var uploaded = await upload.Content.ReadFromJsonAsync<OrganizationDto>(JsonOptions);
        Assert.Equal($"/api/v1/access/organizations/{organizationId}/logo", uploaded!.LogoUrl);

        var download = await client.GetAsync($"/api/v1/access/organizations/{organizationId}/logo");
        Assert.Equal(HttpStatusCode.OK, download.StatusCode);
        Assert.Equal("image/png", download.Content.Headers.ContentType?.MediaType);

        var remove = await client.DeleteAsync($"/api/v1/access/organizations/{organizationId}/logo");
        Assert.Equal(HttpStatusCode.OK, remove.StatusCode);
        var cleared = await remove.Content.ReadFromJsonAsync<OrganizationDto>(JsonOptions);
        Assert.Null(cleared?.LogoUrl);

        Assert.Equal(HttpStatusCode.NotFound, (await client.GetAsync($"/api/v1/access/organizations/{organizationId}/logo")).StatusCode);
    }

    [Fact]
    public async Task NonSuperAdmin_CannotUploadCustomerLogo()
    {
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<SilaMeDbContext>();
        var organizationId = await db.Organizations.Where(item => item.Code == OrganizationCode).Select(item => item.Id).SingleAsync();

        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions { HandleCookies = true });
        client.DefaultRequestHeaders.Add("X-Sila-Route-Slug", "sila-dev");
        Assert.True((await client.PostAsJsonAsync("/api/auth/cloud/login", new { email = ViewerEmail, password = Password })).IsSuccessStatusCode);

        using var content = new MultipartFormDataContent();
        var file = new ByteArrayContent(Encoding.UTF8.GetBytes("x"));
        file.Headers.ContentType = new MediaTypeHeaderValue("image/png");
        content.Add(file, "file", "customer.png");
        var upload = await client.PutAsync($"/api/v1/access/organizations/{organizationId}/logo", content);
        Assert.Equal(HttpStatusCode.Forbidden, upload.StatusCode);
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

    private sealed record OrganizationDto(Guid Id, string Code, string Name, string? LogoUrl);
}
