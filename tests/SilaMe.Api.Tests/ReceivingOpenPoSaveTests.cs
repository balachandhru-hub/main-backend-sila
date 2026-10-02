using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using SilaMe.Api.Data;
using SilaMe.Api.Models;
using SilaMe.Api.Services;
using Xunit;

namespace SilaMe.Api.Tests;

public sealed class ReceivingOpenPoSaveTests(ApiFactory factory) : IClassFixture<ApiFactory>
{
    [Fact]
    public async Task Save_resolves_open_po_when_supplier_entity_is_default_and_po_is_1050()
    {
        await using var scope = factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<SilaMeDbContext>();
        var operations = scope.ServiceProvider.GetRequiredService<OperationalService>();
        var now = DateTime.UtcNow;
        var user = new User
        {
            Id = Guid.NewGuid(),
            Email = $"recv-{Guid.NewGuid():N}@silame.local",
            NormalizedEmail = string.Empty,
            DisplayName = "Receiving Test",
            PasswordHash = "not-used",
            CreatedAt = now,
            UpdatedAt = now,
        };
        user.NormalizedEmail = user.Email.ToUpperInvariant();
        var organization = new Organization
        {
            Id = Guid.NewGuid(),
            Code = $"RCV-{Guid.NewGuid():N}"[..16].ToUpperInvariant(),
            Name = "Receiving Test Org",
            Kind = OrganizationKind.CUSTOMER,
            CreatedAt = now,
            UpdatedAt = now,
        };
        var unit = new OrganizationUnit
        {
            Id = Guid.NewGuid(), OrganizationId = organization.Id, Code = "RCV", Name = "Receiving Store",
            Kind = OrganizationUnitKind.STORE, CreatedAt = now, UpdatedAt = now,
        };
        var role = new Role
        {
            Id = Guid.NewGuid(), Key = $"RCV_{Guid.NewGuid():N}"[..24].ToUpperInvariant(),
            Name = "Receiving Role", CreatedAt = now, UpdatedAt = now,
        };
        var supplier = new Supplier
        {
            Id = Guid.NewGuid(), OrganizationId = organization.Id, SupplierCode = "1003430",
            Name = "Test SBN", NormalizedName = "TEST SBN", EntityCode = "DEFAULT",
            Status = StatusKind.ACTIVE, CreatedAt = now, UpdatedAt = now,
        };
        var purchaseOrder = new PurchaseOrder
        {
            Id = Guid.NewGuid(), OrganizationId = organization.Id, OperatingUnitId = unit.Id,
            SupplierId = supplier.Id, PoNumber = "4500003415", EntityCode = "1050", CompanyCode = "1050",
            Currency = "AED", Status = PurchaseOrderStatus.OPEN, SourceSystem = "TEST",
            CreatedAt = now, UpdatedAt = now,
        };
        purchaseOrder.Items.Add(new PurchaseOrderItem
        {
            Id = Guid.NewGuid(), PurchaseOrderId = purchaseOrder.Id, LineNumber = 10, ItemNumber = "10",
            MaterialCode = "MAT-10", Description = "Item 10", OrderedQuantity = 5, ReceivedQuantity = 0,
            OpenQuantity = 5, Uom = "EA", Status = PurchaseOrderItemStatus.OPEN, GoodsReceiptExpected = true,
            CreatedAt = now, UpdatedAt = now,
        });
        purchaseOrder.Items.Add(new PurchaseOrderItem
        {
            Id = Guid.NewGuid(), PurchaseOrderId = purchaseOrder.Id, LineNumber = 20, ItemNumber = "20",
            MaterialCode = "MAT-20", Description = "Item 20", OrderedQuantity = 5, ReceivedQuantity = 0,
            OpenQuantity = 5, Uom = "EA", Status = PurchaseOrderItemStatus.OPEN, GoodsReceiptExpected = true,
            CreatedAt = now, UpdatedAt = now,
        });
        var permission = await db.Permissions.SingleAsync(item => item.Key == PermissionKeys.UploadInvoice);
        role.Permissions.Add(new RolePermission { RoleId = role.Id, PermissionId = permission.Id });
        db.AddRange(user, organization, unit, supplier, purchaseOrder, role);
        db.UserApplicationAccess.Add(new UserApplicationAccess
        {
            Id = Guid.NewGuid(), UserId = user.Id, Application = ApplicationKind.MOBILE, CreatedAt = now,
        });
        db.UserOrganizationMemberships.Add(new UserOrganizationMembership
        {
            Id = Guid.NewGuid(), UserId = user.Id, OrganizationId = organization.Id, OrganizationUnitId = unit.Id, CreatedAt = now,
        });
        db.UserRoleAssignments.Add(new UserRoleAssignment
        {
            Id = Guid.NewGuid(), UserId = user.Id, RoleId = role.Id, OrganizationId = organization.Id,
            OrganizationUnitId = unit.Id, CreatedAt = now,
        });
        await db.SaveChangesAsync();

        var session = new Session
        {
            Id = Guid.NewGuid(), UserId = user.Id, Application = ApplicationKind.MOBILE,
            TokenHash = "recv-test", CreatedAt = now, ExpiresAt = now.AddHours(1), LastUsedAt = now,
        };
        var linesJson = """[{"lineNumber":10,"description":"Item 10","quantity":5},{"lineNumber":20,"description":"Item 20","quantity":5}]""";
        var saved = await operations.UploadInvoiceAsync(
            session, organization.Id, unit.Id, DocumentSourceChannel.MOBILE_SCANNER,
            File("test-sbn.pdf", "test sbn invoice"), CancellationToken.None,
            pageCount: 1, scanSessionId: $"scan-{Guid.NewGuid():N}",
            supplierName: "ID: 1003430", supplierId: supplier.Id,
            supplierInvoiceNumber: $"RCV-{Guid.NewGuid():N}"[..16],
            invoiceDate: new DateOnly(2026, 9, 18), purchaseOrderNumber: "4500003415",
            invoiceGross: 10m, currency: "AED", deferFullExtraction: true, invoiceLinesJson: linesJson);

        Assert.NotEqual(Guid.Empty, saved.InvoiceId);
        var invoice = await db.Invoices.Include(item => item.Lines).SingleAsync(item => item.Id == saved.InvoiceId);
        Assert.Equal(purchaseOrder.Id, invoice.PurchaseOrderId);
        Assert.Equal("Test SBN", invoice.SupplierNameRaw);
        Assert.DoesNotContain("ID:", invoice.SupplierNameRaw);
        Assert.Equal(2, invoice.Lines.Count);
        Assert.All(invoice.Lines, line => Assert.Equal(5m, line.Quantity));
        Assert.All(invoice.Lines, line => Assert.NotNull(line.PurchaseOrderItemId));
    }

    [Fact]
    public void Unscoped_supplier_entity_matches_company_code_po()
    {
        Assert.True(PurchaseOrderReceiving.IsUnscopedEntity("DEFAULT"));
        Assert.True(PurchaseOrderReceiving.IsUnscopedEntity("ALL"));
        Assert.True(PurchaseOrderReceiving.MatchesRequestedEntity("DEFAULT", "1050"));
        Assert.True(PurchaseOrderReceiving.MatchesRequestedEntity(null, "1050"));
        Assert.False(PurchaseOrderReceiving.MatchesRequestedEntity("1040", "1050"));
    }

    [Fact]
    public async Task Open_po_search_http_returns_po_for_internal_supplier_uuid_without_default_entity_filter()
    {
        await using var scope = factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<SilaMeDbContext>();
        var passwords = scope.ServiceProvider.GetRequiredService<IPasswordService>();
        var now = DateTime.UtcNow;
        const string password = "test-password-strong";
        var email = $"recv-http-{Guid.NewGuid():N}@silame.local";
        var user = new User
        {
            Id = Guid.NewGuid(),
            Email = email,
            NormalizedEmail = email.ToUpperInvariant(),
            DisplayName = "Receiving HTTP",
            PasswordHash = string.Empty,
            CreatedAt = now,
            UpdatedAt = now,
        };
        user.PasswordHash = passwords.HashPassword(user, password);
        var organization = new Organization
        {
            Id = Guid.NewGuid(),
            Code = $"RCH-{Guid.NewGuid():N}"[..16].ToUpperInvariant(),
            Name = "Receiving HTTP Org",
            Kind = OrganizationKind.CUSTOMER,
            CreatedAt = now,
            UpdatedAt = now,
        };
        var unit = new OrganizationUnit
        {
            Id = Guid.NewGuid(), OrganizationId = organization.Id, Code = "RCH", Name = "Receiving HTTP Store",
            Kind = OrganizationUnitKind.STORE, CreatedAt = now, UpdatedAt = now,
        };
        var role = await db.Roles.SingleAsync(item => item.Key == "RECEIVING_OPERATOR");
        var supplier = new Supplier
        {
            Id = Guid.NewGuid(), OrganizationId = organization.Id, SupplierCode = "1003430",
            Name = "Test SBN", NormalizedName = "TEST SBN", EntityCode = "DEFAULT",
            Status = StatusKind.ACTIVE, CreatedAt = now, UpdatedAt = now,
        };
        var purchaseOrder = new PurchaseOrder
        {
            Id = Guid.NewGuid(), OrganizationId = organization.Id, OperatingUnitId = unit.Id,
            SupplierId = supplier.Id, PoNumber = "4500003415", EntityCode = "1050", CompanyCode = "1050",
            Currency = "AED", Status = PurchaseOrderStatus.OPEN, SourceSystem = "TEST", ErpSupplierId = "1003430",
            CreatedAt = now, UpdatedAt = now,
        };
        purchaseOrder.Items.Add(new PurchaseOrderItem
        {
            Id = Guid.NewGuid(), PurchaseOrderId = purchaseOrder.Id, LineNumber = 10, ItemNumber = "10",
            MaterialCode = "MAT-10", Description = "Item 10", OrderedQuantity = 5, ReceivedQuantity = 0,
            OpenQuantity = 5, Uom = "EA", Status = PurchaseOrderItemStatus.OPEN, GoodsReceiptExpected = true,
            CreatedAt = now, UpdatedAt = now,
        });
        db.AddRange(user, organization, unit, supplier, purchaseOrder);
        db.UserApplicationAccess.Add(new UserApplicationAccess
        {
            Id = Guid.NewGuid(), UserId = user.Id, Application = ApplicationKind.MOBILE, CreatedAt = now,
        });
        db.UserOrganizationMemberships.Add(new UserOrganizationMembership
        {
            Id = Guid.NewGuid(), UserId = user.Id, OrganizationId = organization.Id, OrganizationUnitId = unit.Id, CreatedAt = now,
        });
        db.UserRoleAssignments.Add(new UserRoleAssignment
        {
            Id = Guid.NewGuid(), UserId = user.Id, RoleId = role.Id, OrganizationId = organization.Id,
            OrganizationUnitId = unit.Id, CreatedAt = now,
        });
        await db.SaveChangesAsync();

        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions { HandleCookies = true });
        client.DefaultRequestHeaders.Add("X-Sila-Route-Slug", "sila-dev");
        var login = await client.PostAsJsonAsync("/api/auth/mobile/login", new { email, password });
        Assert.Equal(HttpStatusCode.OK, login.StatusCode);
        using var loginDoc = JsonDocument.Parse(await login.Content.ReadAsStringAsync());
        var token = loginDoc.RootElement.GetProperty("token").GetString();
        Assert.False(string.IsNullOrWhiteSpace(token));
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var url = $"/api/v1/purchase-orders/search?organizationId={organization.Id}&supplierId={supplier.Id}&openOnly=true";
        var response = await client.GetAsync(url);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var payload = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal(JsonValueKind.Array, payload.ValueKind);
        Assert.Contains(payload.EnumerateArray(), item => item.GetProperty("poNumber").GetString() == "4500003415");
        Assert.All(payload.EnumerateArray(), item => Assert.Equal(supplier.Id, item.GetProperty("supplierId").GetGuid()));

        var defaultEntity = await client.GetAsync($"{url}&entityCode=DEFAULT");
        Assert.Equal(HttpStatusCode.OK, defaultEntity.StatusCode);
        var defaultPayload = await defaultEntity.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Contains(defaultPayload.EnumerateArray(), item => item.GetProperty("poNumber").GetString() == "4500003415");
    }

    private static Microsoft.AspNetCore.Http.IFormFile File(string filename, string contents)
    {
        var stream = new MemoryStream(System.Text.Encoding.UTF8.GetBytes(contents));
        return new Microsoft.AspNetCore.Http.FormFile(stream, 0, stream.Length, "file", filename)
        {
            Headers = new Microsoft.AspNetCore.Http.HeaderDictionary(),
            ContentType = "application/pdf",
        };
    }
}
