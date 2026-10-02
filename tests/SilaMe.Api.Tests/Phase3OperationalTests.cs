using System.Net;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using SilaMe.Api.Data;
using SilaMe.Api.DTOs;
using SilaMe.Api.Models;
using SilaMe.Api.Services;
using Xunit;

namespace SilaMe.Api.Tests;

public sealed class Phase3OperationalTests(ApiFactory factory) : IClassFixture<ApiFactory>
{
    [Fact]
    public async Task Grn_uses_physical_quantity_and_is_idempotent()
    {
        await using var scope = factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<SilaMeDbContext>();
        var operations = scope.ServiceProvider.GetRequiredService<OperationalService>();
        var now = DateTime.UtcNow;
        var user = new User
        {
            Id = Guid.NewGuid(),
            Email = $"phase3-{Guid.NewGuid():N}@silame.local",
            NormalizedEmail = $"PHASE3-{Guid.NewGuid():N}@SILAME.LOCAL",
            DisplayName = "Phase 3 Test Operator",
            PasswordHash = "not-used",
            CreatedAt = now,
            UpdatedAt = now,
        };
        user.NormalizedEmail = user.Email.ToUpperInvariant();
        var organization = new Organization
        {
            Id = Guid.NewGuid(),
            Code = $"PHASE3-{Guid.NewGuid():N}"[..16].ToUpperInvariant(),
            Name = "Phase 3 Receiving Test",
            Kind = OrganizationKind.CUSTOMER,
            CreatedAt = now,
            UpdatedAt = now,
        };
        var unit = new OrganizationUnit
        {
            Id = Guid.NewGuid(), OrganizationId = organization.Id, Code = "PHASE3-STORE", Name = "Phase 3 Store",
            Kind = OrganizationUnitKind.STORE, CreatedAt = now, UpdatedAt = now,
        };
        var supplier = new Supplier
        {
            Id = Guid.NewGuid(), OrganizationId = organization.Id, SupplierCode = "PHASE3-SUPPLIER",
            Name = "Phase 3 Supplier", NormalizedName = "PHASE3 SUPPLIER", Status = StatusKind.ACTIVE,
            CreatedAt = now, UpdatedAt = now,
        };
        var material = new Material
        {
            Id = Guid.NewGuid(), OrganizationId = organization.Id, MaterialCode = "PHASE3-CHICKEN",
            Description = "Chicken Breast", NormalizedDescription = "CHICKEN BREAST", BaseUom = "KG",
            Status = StatusKind.ACTIVE, CreatedAt = now, UpdatedAt = now,
        };
        var po = new PurchaseOrder
        {
            Id = Guid.NewGuid(), OrganizationId = organization.Id, OperatingUnitId = unit.Id, SupplierId = supplier.Id,
            PoNumber = $"PHASE3-PO-{Guid.NewGuid():N}"[..20].ToUpperInvariant(), PoDate = DateOnly.FromDateTime(now),
            Currency = "AED", Status = PurchaseOrderStatus.OPEN, SourceSystem = "TEST", CreatedAt = now, UpdatedAt = now,
        };
        var poItem = new PurchaseOrderItem
        {
            Id = Guid.NewGuid(), PurchaseOrderId = po.Id, LineNumber = 10, MaterialId = material.Id,
            MaterialCode = material.MaterialCode, Description = material.Description, OrderedQuantity = 100,
            ReceivedQuantity = 0, OpenQuantity = 100, Uom = "KG", UnitPrice = 22.5m,
            Status = PurchaseOrderItemStatus.OPEN, CreatedAt = now, UpdatedAt = now,
        };
        po.Items.Add(poItem);
        var document = new Document
        {
            Id = Guid.NewGuid(), OrganizationId = organization.Id, OperatingUnitId = unit.Id, UploadedByUserId = user.Id,
            DocumentType = DocumentType.INVOICE, OriginalFilename = "phase3-test.pdf", ContentType = "application/pdf",
            StorageProvider = "TEST", StorageReference = "phase3-test.pdf", Status = DocumentStatus.PROCESSED,
            SourceChannel = DocumentSourceChannel.CLOUD_UPLOAD, CreatedAt = now, UpdatedAt = now,
        };
        var invoice = new Invoice
        {
            Id = Guid.NewGuid(), OrganizationId = organization.Id, OperatingUnitId = unit.Id, DocumentId = document.Id,
            SupplierId = supplier.Id, InvoiceNumber = $"PHASE3-INV-{Guid.NewGuid():N}"[..24].ToUpperInvariant(),
            InvoiceDate = DateOnly.FromDateTime(now), SupplierNameRaw = supplier.Name, PurchaseOrderId = po.Id,
            Currency = "AED", InvoiceType = InvoiceType.MATERIAL, Status = InvoiceStatus.READY_FOR_GRN,
            CreatedByUserId = user.Id, CreatedAt = now, UpdatedAt = now,
        };
        invoice.Lines.Add(new InvoiceLine
        {
            Id = Guid.NewGuid(), InvoiceId = invoice.Id, LineNumber = 10, MaterialId = material.Id,
            MaterialCodeRaw = material.MaterialCode, DescriptionRaw = material.Description, Quantity = 50, Uom = "KG",
            UnitPrice = 22.5m, MatchStatus = InvoiceLineMatchStatus.MATCHED, PurchaseOrderItemId = poItem.Id,
            CreatedAt = now, UpdatedAt = now,
        });
        var permission = await db.Permissions.SingleAsync(item => item.Key == PermissionKeys.PostGrn);
        var role = new Role
        {
            Id = Guid.NewGuid(), Key = $"PHASE3_ROLE_{Guid.NewGuid():N}"[..24].ToUpperInvariant(), Name = "Phase 3 Test Role",
            CreatedAt = now, UpdatedAt = now,
        };
        role.Permissions.Add(new RolePermission { RoleId = role.Id, PermissionId = permission.Id });
        db.AddRange(user, organization, unit, supplier, material, po, document, invoice, role);
        db.UserApplicationAccess.Add(new UserApplicationAccess
        {
            Id = Guid.NewGuid(), UserId = user.Id, Application = ApplicationKind.CLOUD, CreatedAt = now,
        });
        db.UserRoleAssignments.Add(new UserRoleAssignment
        {
            Id = Guid.NewGuid(), UserId = user.Id, RoleId = role.Id, OrganizationId = organization.Id,
            OrganizationUnitId = unit.Id, CreatedAt = now,
        });
        db.UserOrganizationMemberships.Add(new UserOrganizationMembership
        {
            Id = Guid.NewGuid(), UserId = user.Id, OrganizationId = organization.Id, OrganizationUnitId = unit.Id, CreatedAt = now,
        });
        await db.SaveChangesAsync();

        db.ApiIntegrationConfigurations.Add(new ApiIntegrationConfiguration
        {
            Id = Guid.NewGuid(), OrganizationId = organization.Id, EntityCode = "ALL", Name = "POST_GRN",
            ProcessType = IntegrationProcessType.POST_GRN, Protocol = IntegrationProtocol.REST,
            BaseUrl = "https://erp.phase3.test", ResourcePath = "goods-receipts",
            AuthenticationType = IntegrationAuthenticationType.NONE, Status = IntegrationConfigurationStatus.ACTIVE,
            CreatedAt = now, UpdatedAt = now,
        });
        await db.SaveChangesAsync();
        factory.IntegrationHandler.Reset();
        factory.IntegrationHandler.Enqueue(HttpStatusCode.OK, """{"MaterialDocument":"5000112233","DocumentYear":"2026"}""");

        var session = new Session
        {
            Id = Guid.NewGuid(), UserId = user.Id, Application = ApplicationKind.CLOUD, TokenHash = "phase3-test",
            CreatedAt = now, ExpiresAt = now.AddHours(1), LastUsedAt = now,
        };
        var request = new PostGrnRequest
        {
            InvoiceId = invoice.Id, PurchaseOrderId = po.Id, OperatingUnitId = unit.Id,
            Lines =
            [
                new GrnLineInput
                {
                    PurchaseOrderItemId = poItem.Id, ReceivedQuantity = 48, AcceptedQuantity = 46,
                    DamagedQuantity = 2, RejectedQuantity = 0,
                },
            ],
        };

        var posted = await operations.PostGrnAsync(session, request, "phase3-idempotency-key", CancellationToken.None);
        var retried = await operations.PostGrnAsync(session, request, "phase3-idempotency-key", CancellationToken.None);
        var savedItem = await db.PurchaseOrderItems.SingleAsync(item => item.Id == poItem.Id);
        var stock = await db.StockBalances.SingleAsync(item => item.MaterialCode == material.MaterialCode && item.OperatingUnitId == unit.Id);

        Assert.Equal(posted.Id, retried.Id);
        Assert.Equal(46, savedItem.ReceivedQuantity);
        Assert.Equal(54, savedItem.OpenQuantity);
        Assert.Equal(46, stock.Quantity);
        Assert.Equal(1, await db.InventoryTransactions.CountAsync(item => item.ReferenceId == posted.Id));
        Assert.Equal(1, await db.GoodsReceipts.CountAsync(item => item.InvoiceId == invoice.Id));
    }

    [Fact]
    public async Task Grn_keeps_invoice_physical_and_accepted_quantities_distinct()
    {
        await using var scope = factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<SilaMeDbContext>();
        var operations = scope.ServiceProvider.GetRequiredService<OperationalService>();
        var now = DateTime.UtcNow;
        var user = new User
        {
            Id = Guid.NewGuid(), Email = $"phase3-qty-{Guid.NewGuid():N}@silame.local", NormalizedEmail = string.Empty, DisplayName = "Qty Operator",
            PasswordHash = "not-used", CreatedAt = now, UpdatedAt = now,
        };
        user.NormalizedEmail = user.Email.ToUpperInvariant();
        var organization = new Organization
        {
            Id = Guid.NewGuid(), Code = $"QTY-{Guid.NewGuid():N}"[..16].ToUpperInvariant(), Name = "Qty Org",
            Kind = OrganizationKind.CUSTOMER, CreatedAt = now, UpdatedAt = now,
        };
        var unit = new OrganizationUnit
        {
            Id = Guid.NewGuid(), OrganizationId = organization.Id, Code = "QTY-STORE", Name = "Qty Store",
            Kind = OrganizationUnitKind.STORE, CreatedAt = now, UpdatedAt = now,
        };
        var supplier = new Supplier
        {
            Id = Guid.NewGuid(), OrganizationId = organization.Id, SupplierCode = "1000234",
            Name = "ABC FOOD TRADING LLC", NormalizedName = "ABC FOOD TRADING LLC", Status = StatusKind.ACTIVE,
            CreatedAt = now, UpdatedAt = now,
        };
        var material = new Material
        {
            Id = Guid.NewGuid(), OrganizationId = organization.Id, MaterialCode = "QTY-MAT",
            Description = "Qty Material", NormalizedDescription = "QTY MATERIAL", BaseUom = "KG",
            Status = StatusKind.ACTIVE, CreatedAt = now, UpdatedAt = now,
        };
        var po = new PurchaseOrder
        {
            Id = Guid.NewGuid(), OrganizationId = organization.Id, OperatingUnitId = unit.Id, SupplierId = supplier.Id,
            PoNumber = "4500012345", PoDate = DateOnly.FromDateTime(now), Currency = "AED",
            Status = PurchaseOrderStatus.OPEN, SourceSystem = "TEST", CreatedAt = now, UpdatedAt = now,
        };
        var poItem = new PurchaseOrderItem
        {
            Id = Guid.NewGuid(), PurchaseOrderId = po.Id, LineNumber = 10, MaterialId = material.Id,
            MaterialCode = material.MaterialCode, Description = material.Description, OrderedQuantity = 10,
            ReceivedQuantity = 2, OpenQuantity = 8, Uom = "KG", GoodsReceiptExpected = true,
            Status = PurchaseOrderItemStatus.PARTIALLY_RECEIVED, CreatedAt = now, UpdatedAt = now,
        };
        po.Items.Add(poItem);
        var closed = new PurchaseOrder
        {
            Id = Guid.NewGuid(), OrganizationId = organization.Id, OperatingUnitId = unit.Id, SupplierId = supplier.Id,
            PoNumber = "4500009999", PoDate = DateOnly.FromDateTime(now), Currency = "AED",
            Status = PurchaseOrderStatus.CLOSED, SourceSystem = "TEST", CreatedAt = now, UpdatedAt = now,
        };
        closed.Items.Add(new PurchaseOrderItem
        {
            Id = Guid.NewGuid(), PurchaseOrderId = closed.Id, LineNumber = 10, MaterialCode = "QTY-MAT",
            Description = "Closed", OrderedQuantity = 5, ReceivedQuantity = 5, OpenQuantity = 0, Uom = "KG",
            Status = PurchaseOrderItemStatus.CLOSED, CreatedAt = now, UpdatedAt = now,
        });
        var openSecond = new PurchaseOrder
        {
            Id = Guid.NewGuid(), OrganizationId = organization.Id, OperatingUnitId = unit.Id, SupplierId = supplier.Id,
            PoNumber = "4500012400", PoDate = DateOnly.FromDateTime(now), Currency = "AED",
            Status = PurchaseOrderStatus.OPEN, SourceSystem = "TEST", CreatedAt = now, UpdatedAt = now,
        };
        openSecond.Items.Add(new PurchaseOrderItem
        {
            Id = Guid.NewGuid(), PurchaseOrderId = openSecond.Id, LineNumber = 10, MaterialCode = "QTY-MAT",
            Description = "Open two", OrderedQuantity = 4, ReceivedQuantity = 0, OpenQuantity = 4, Uom = "KG",
            GoodsReceiptExpected = true, Status = PurchaseOrderItemStatus.OPEN, CreatedAt = now, UpdatedAt = now,
        });
        var document = new Document
        {
            Id = Guid.NewGuid(), OrganizationId = organization.Id, OperatingUnitId = unit.Id, UploadedByUserId = user.Id,
            DocumentType = DocumentType.INVOICE, OriginalFilename = "qty.pdf", ContentType = "application/pdf",
            StorageProvider = "TEST", StorageReference = "qty.pdf", Status = DocumentStatus.PROCESSED,
            SourceChannel = DocumentSourceChannel.CLOUD_UPLOAD, CreatedAt = now, UpdatedAt = now,
        };
        var invoice = new Invoice
        {
            Id = Guid.NewGuid(), OrganizationId = organization.Id, OperatingUnitId = unit.Id, DocumentId = document.Id,
            SupplierId = supplier.Id, InvoiceNumber = "QTY-INV-1", InvoiceDate = DateOnly.FromDateTime(now),
            SupplierNameRaw = "ABC Food Trading L.L.C.", PurchaseOrderId = po.Id, Currency = "AED",
            InvoiceType = InvoiceType.MATERIAL, Status = InvoiceStatus.READY_FOR_GRN,
            CreatedByUserId = user.Id, CreatedAt = now, UpdatedAt = now,
        };
        invoice.Lines.Add(new InvoiceLine
        {
            Id = Guid.NewGuid(), InvoiceId = invoice.Id, LineNumber = 10, MaterialCodeRaw = material.MaterialCode,
            DescriptionRaw = material.Description, Quantity = 7, Uom = "KG",
            MatchStatus = InvoiceLineMatchStatus.UNMATCHED, CreatedAt = now, UpdatedAt = now,
        });
        var permission = await db.Permissions.SingleAsync(item => item.Key == PermissionKeys.PostGrn);
        var role = new Role
        {
            Id = Guid.NewGuid(), Key = $"QTY_ROLE_{Guid.NewGuid():N}"[..24].ToUpperInvariant(), Name = "Qty Role",
            CreatedAt = now, UpdatedAt = now,
        };
        role.Permissions.Add(new RolePermission { RoleId = role.Id, PermissionId = permission.Id });
        db.AddRange(user, organization, unit, supplier, material, po, closed, openSecond, document, invoice, role);
        db.UserApplicationAccess.Add(new UserApplicationAccess { Id = Guid.NewGuid(), UserId = user.Id, Application = ApplicationKind.CLOUD, CreatedAt = now });
        db.UserRoleAssignments.Add(new UserRoleAssignment { Id = Guid.NewGuid(), UserId = user.Id, RoleId = role.Id, OrganizationId = organization.Id, OrganizationUnitId = unit.Id, CreatedAt = now });
        db.UserOrganizationMemberships.Add(new UserOrganizationMembership { Id = Guid.NewGuid(), UserId = user.Id, OrganizationId = organization.Id, OrganizationUnitId = unit.Id, CreatedAt = now });
        db.ApiIntegrationConfigurations.Add(new ApiIntegrationConfiguration
        {
            Id = Guid.NewGuid(), OrganizationId = organization.Id, EntityCode = "ALL", Name = "POST_GRN",
            ProcessType = IntegrationProcessType.POST_GRN, Protocol = IntegrationProtocol.REST,
            BaseUrl = "https://erp.qty.test", ResourcePath = "goods-receipts",
            AuthenticationType = IntegrationAuthenticationType.NONE, Status = IntegrationConfigurationStatus.ACTIVE,
            CreatedAt = now, UpdatedAt = now,
        });
        await db.SaveChangesAsync();
        factory.IntegrationHandler.Reset();
        factory.IntegrationHandler.Enqueue(HttpStatusCode.OK, """{"MaterialDocument":"5000223344","DocumentYear":"2026"}""");

        Assert.True(PurchaseOrderReceiving.IsOpenForReceiving(po));
        Assert.True(PurchaseOrderReceiving.IsOpenForReceiving(openSecond));
        Assert.Equal("PO CLOSED", PurchaseOrderReceiving.IneligibilityReason(closed));

        var over = await operations.ValidateGrnAsync(new Session
        {
            Id = Guid.NewGuid(), UserId = user.Id, Application = ApplicationKind.CLOUD, TokenHash = "qty",
            CreatedAt = now, ExpiresAt = now.AddHours(1), LastUsedAt = now,
        }, new PostGrnRequest
        {
            InvoiceId = invoice.Id, PurchaseOrderId = po.Id, OperatingUnitId = unit.Id,
            Lines = [new GrnLineInput { PurchaseOrderItemId = poItem.Id, ReceivedQuantity = 9, AcceptedQuantity = 9, DamagedQuantity = 0, RejectedQuantity = 0 }],
        }, CancellationToken.None);
        Assert.Contains(over.Errors, error => error.Contains("GRN_QUANTITY_EXCEEDS_PO_OPEN_QUANTITY"));

        var session = new Session
        {
            Id = Guid.NewGuid(), UserId = user.Id, Application = ApplicationKind.CLOUD, TokenHash = "qty-post",
            CreatedAt = now, ExpiresAt = now.AddHours(1), LastUsedAt = now,
        };
        var posted = await operations.PostGrnAsync(session, new PostGrnRequest
        {
            InvoiceId = invoice.Id, PurchaseOrderId = po.Id, OperatingUnitId = unit.Id,
            Lines =
            [
                new GrnLineInput
                {
                    PurchaseOrderItemId = poItem.Id, ReceivedQuantity = 6, AcceptedQuantity = 5,
                    DamagedQuantity = 1, RejectedQuantity = 0,
                },
            ],
        }, "qty-idempotency", CancellationToken.None);
        var savedItem = await db.PurchaseOrderItems.SingleAsync(item => item.Id == poItem.Id);
        var line = await db.GoodsReceiptLines.SingleAsync(item => item.GoodsReceiptId == posted.Id);
        Assert.Equal(6, line.ReceivedQuantity);
        Assert.Equal(5, line.AcceptedQuantity);
        Assert.Equal(1, line.DamagedQuantity);
        Assert.Equal(7, invoice.Lines.Single().Quantity);
        Assert.Equal(7, savedItem.ReceivedQuantity);
        Assert.Equal(3, savedItem.OpenQuantity);
        Assert.Equal(5, (await db.StockBalances.SingleAsync(item => item.MaterialCode == material.MaterialCode && item.OperatingUnitId == unit.Id)).Quantity);
    }
}