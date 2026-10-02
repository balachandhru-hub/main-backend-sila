using System.Net;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using SilaMe.Api.Data;
using SilaMe.Api.DTOs;
using SilaMe.Api.Models;
using SilaMe.Api.Services;
using Xunit;

namespace SilaMe.Api.Tests;

[Collection("api-factory")]
public sealed class PostGrnConfigurationTests(ApiFactory factory) : IClassFixture<ApiFactory>
{
    [Fact]
    public void Data_protection_application_name_is_stable_when_api_starts_from_dll()
    {
        var project = Path.GetDirectoryName(Directory.GetFiles("/workspace", "SilaMe.Api.csproj", SearchOption.AllDirectories).Single())!;
        var expected = project.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar) + Path.DirectorySeparatorChar;
        Assert.Equal(expected, ApiDataProtection.ApplicationName(project));
        Assert.Equal(expected, ApiDataProtection.ApplicationName(Path.Combine(project, "bin", "Debug", "net8.0")));
    }

    [Fact]
    public void ResolvePostGrnConfiguration_prefers_entity_over_all()
    {
        var organizationId = Guid.NewGuid();
        var all = Config(organizationId, "ALL", "all-fallback");
        var entity = Config(organizationId, "1050", "entity-1050");
        var resolved = IntegrationService.ResolvePostGrnConfiguration([all, entity], "1050");
        Assert.Equal(entity.Id, resolved?.Id);
    }

    [Fact]
    public async Task Missing_post_grn_config_does_not_update_stock()
    {
        factory.IntegrationHandler.Reset();
        var posted = await PostSampleAsync(configure: false);
        Assert.Equal(GoodsReceiptStatus.READY_TO_POST, posted.Grn.Status);
        Assert.Equal("POST_GRN_CONFIGURATION_NOT_FOUND", posted.Grn.FailureCode);
        Assert.Equal(0, posted.ReceivedQuantity);
        Assert.False(posted.HasStock);
    }

    [Fact]
    public async Task Post_grn_uses_this_tenant_configuration_only()
    {
        factory.IntegrationHandler.Reset();
        factory.IntegrationHandler.Enqueue(HttpStatusCode.OK, """{"MaterialDocument":"5000001111","DocumentYear":"2026"}""");
        var posted = await PostSampleAsync(configure: true, otherTenantUrl: "https://erp.tenant-b.test/grn");
        Assert.Equal(GoodsReceiptStatus.POSTED, posted.Grn.Status);
        Assert.Equal("5000001111", posted.Grn.ErpMaterialDocument);
        Assert.Contains(factory.IntegrationHandler.Requests, uri => uri.Host == "erp.tenant-a.test");
        Assert.DoesNotContain(factory.IntegrationHandler.Requests, uri => uri.Host == "erp.tenant-b.test");
    }

    [Fact]
    public async Task Erp_rejection_does_not_update_stock()
    {
        factory.IntegrationHandler.Reset();
        factory.IntegrationHandler.Enqueue(HttpStatusCode.BadRequest, """{"error":"rejected"}""");
        var posted = await PostSampleAsync(configure: true);
        Assert.Equal(GoodsReceiptStatus.FAILED, posted.Grn.Status);
        Assert.Equal(0, posted.ReceivedQuantity);
        Assert.False(posted.HasStock);
    }

    [Fact]
    public async Task Erp_timeout_is_unknown_and_does_not_update_stock()
    {
        factory.IntegrationHandler.Reset();
        factory.IntegrationHandler.NextException = new TaskCanceledException();
        var posted = await PostSampleAsync(configure: true);
        Assert.Equal(GoodsReceiptStatus.UNKNOWN, posted.Grn.Status);
        Assert.Equal("ERP_POST_UNKNOWN", posted.Grn.FailureCode);
        Assert.Equal(0, posted.ReceivedQuantity);
    }

    [Fact]
    public async Task Prepare_grn_does_not_require_operating_unit()
    {
        await using var scope = factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<SilaMeDbContext>();
        var operations = scope.ServiceProvider.GetRequiredService<OperationalService>();
        var now = DateTime.UtcNow;
        var user = new User
        {
            Id = Guid.NewGuid(), Email = $"grn-ou-{Guid.NewGuid():N}@silame.local", NormalizedEmail = string.Empty,
            DisplayName = "GRN Company Code", PasswordHash = "not-used", CreatedAt = now, UpdatedAt = now,
        };
        user.NormalizedEmail = user.Email.ToUpperInvariant();
        var organization = new Organization
        {
            Id = Guid.NewGuid(), Code = $"GRN-{Guid.NewGuid():N}"[..16].ToUpperInvariant(), Name = "GRN Org",
            Kind = OrganizationKind.CUSTOMER, CreatedAt = now, UpdatedAt = now,
        };
        var supplier = new Supplier
        {
            Id = Guid.NewGuid(), OrganizationId = organization.Id, SupplierCode = "1003430", Name = "Test SBN",
            NormalizedName = "TEST SBN", Status = StatusKind.ACTIVE, CreatedAt = now, UpdatedAt = now,
        };
        var po = new PurchaseOrder
        {
            Id = Guid.NewGuid(), OrganizationId = organization.Id, SupplierId = supplier.Id,
            PoNumber = "4500003415", EntityCode = "1050", CompanyCode = "1050", Currency = "AED",
            Status = PurchaseOrderStatus.OPEN, SourceSystem = "TEST", CreatedAt = now, UpdatedAt = now,
        };
        var poItem = new PurchaseOrderItem
        {
            Id = Guid.NewGuid(), PurchaseOrderId = po.Id, LineNumber = 10, ItemNumber = "10",
            MaterialCode = "MAT-1", Description = "GI Red /Socket 2x1", OrderedQuantity = 100,
            ReceivedQuantity = 0, OpenQuantity = 100, Uom = "EA", UnitPrice = 300, Status = PurchaseOrderItemStatus.OPEN,
            GoodsReceiptExpected = true, CreatedAt = now, UpdatedAt = now,
        };
        po.Items.Add(poItem);
        var document = new Document
        {
            Id = Guid.NewGuid(), OrganizationId = organization.Id, UploadedByUserId = user.Id,
            DocumentType = DocumentType.INVOICE, OriginalFilename = "inv.pdf", ContentType = "application/pdf",
            StorageProvider = "TEST", StorageReference = "inv.pdf", Status = DocumentStatus.PROCESSED,
            SourceChannel = DocumentSourceChannel.MOBILE_SCANNER, CreatedAt = now, UpdatedAt = now,
        };
        var invoice = new Invoice
        {
            Id = Guid.NewGuid(), OrganizationId = organization.Id, DocumentId = document.Id,
            SupplierId = supplier.Id, InvoiceNumber = "TEST-INV-NO-OU", InvoiceDate = new DateOnly(2026, 9, 17),
            SupplierNameRaw = "Supplier ID: 1003430", PurchaseOrderId = po.Id, Currency = "AED",
            InvoiceType = InvoiceType.MATERIAL, Status = InvoiceStatus.READY_FOR_GRN,
            CreatedByUserId = user.Id, CreatedAt = now, UpdatedAt = now,
        };
        invoice.Lines.Add(new InvoiceLine
        {
            Id = Guid.NewGuid(), InvoiceId = invoice.Id, LineNumber = 10, DescriptionRaw = poItem.Description,
            Quantity = 5, Uom = "EA", PurchaseOrderItemId = poItem.Id, MatchStatus = InvoiceLineMatchStatus.MATCHED,
            CreatedAt = now, UpdatedAt = now,
        });
        var permission = await db.Permissions.SingleAsync(item => item.Key == PermissionKeys.PostGrn);
        var role = new Role
        {
            Id = Guid.NewGuid(), Key = $"GRN_ROLE_{Guid.NewGuid():N}"[..24].ToUpperInvariant(), Name = "GRN Role",
            CreatedAt = now, UpdatedAt = now,
        };
        role.Permissions.Add(new RolePermission { RoleId = role.Id, PermissionId = permission.Id });
        db.AddRange(user, organization, supplier, po, document, invoice, role);
        db.UserApplicationAccess.Add(new UserApplicationAccess { Id = Guid.NewGuid(), UserId = user.Id, Application = ApplicationKind.CLOUD, CreatedAt = now });
        db.UserRoleAssignments.Add(new UserRoleAssignment { Id = Guid.NewGuid(), UserId = user.Id, RoleId = role.Id, OrganizationId = organization.Id, CreatedAt = now });
        db.UserOrganizationMemberships.Add(new UserOrganizationMembership { Id = Guid.NewGuid(), UserId = user.Id, OrganizationId = organization.Id, CreatedAt = now });
        await db.SaveChangesAsync();
        var session = new Session
        {
            Id = Guid.NewGuid(), UserId = user.Id, Application = ApplicationKind.CLOUD, TokenHash = "grn-no-ou",
            CreatedAt = now, ExpiresAt = now.AddHours(1), LastUsedAt = now,
        };

        var grn = await operations.PrepareGrnAsync(session, new PrepareGrnRequest { InvoiceId = invoice.Id, PurchaseOrderId = po.Id }, CancellationToken.None);

        Assert.Equal("SRC0001", grn.GrnNumber);
        Assert.Equal("SRC0001", grn.ErpMaterialDocument);
        Assert.NotEqual(Guid.Empty, grn.Id);
        Assert.Null(grn.OperatingUnitId);
        Assert.Equal("1050", grn.PurchaseOrder.CompanyCode);
        Assert.DoesNotContain("operating unit", grn.FailureMessage ?? string.Empty, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Finalize_grn_uses_ordered_quantity_when_open_qty_is_stale()
    {
        await using var scope = factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<SilaMeDbContext>();
        var operations = scope.ServiceProvider.GetRequiredService<OperationalService>();
        var now = DateTime.UtcNow;
        var user = new User
        {
            Id = Guid.NewGuid(), Email = $"grn-qty-{Guid.NewGuid():N}@silame.local", NormalizedEmail = string.Empty,
            DisplayName = "GRN Qty", PasswordHash = "not-used", CreatedAt = now, UpdatedAt = now,
        };
        user.NormalizedEmail = user.Email.ToUpperInvariant();
        var organization = new Organization
        {
            Id = Guid.NewGuid(), Code = $"GRN-{Guid.NewGuid():N}"[..16].ToUpperInvariant(), Name = "GRN Org",
            Kind = OrganizationKind.CUSTOMER, CreatedAt = now, UpdatedAt = now,
        };
        var supplier = new Supplier
        {
            Id = Guid.NewGuid(), OrganizationId = organization.Id, SupplierCode = "1003430", Name = "Test SBN",
            NormalizedName = "TEST SBN", Status = StatusKind.ACTIVE, CreatedAt = now, UpdatedAt = now,
        };
        var po = new PurchaseOrder
        {
            Id = Guid.NewGuid(), OrganizationId = organization.Id, SupplierId = supplier.Id,
            PoNumber = "4500003415", EntityCode = "1050", CompanyCode = "1050", Currency = "AED",
            Status = PurchaseOrderStatus.OPEN, SourceSystem = "TEST", CreatedAt = now, UpdatedAt = now,
        };
        var poItem = new PurchaseOrderItem
        {
            Id = Guid.NewGuid(), PurchaseOrderId = po.Id, LineNumber = 10, ItemNumber = "10",
            MaterialCode = "1006929", Description = "1000142  Goose Island JVC  2026-01-20",
            OrderedQuantity = 1000, ReceivedQuantity = 4570, OpenQuantity = 1, Uom = "EA",
            Status = PurchaseOrderItemStatus.OPEN, GoodsReceiptExpected = true, CreatedAt = now, UpdatedAt = now,
        };
        po.Items.Add(poItem);
        var document = new Document
        {
            Id = Guid.NewGuid(), OrganizationId = organization.Id, UploadedByUserId = user.Id,
            DocumentType = DocumentType.INVOICE, OriginalFilename = "inv.pdf", ContentType = "application/pdf",
            StorageProvider = "TEST", StorageReference = "inv.pdf", Status = DocumentStatus.PROCESSED,
            SourceChannel = DocumentSourceChannel.MOBILE_SCANNER, CreatedAt = now, UpdatedAt = now,
        };
        var invoice = new Invoice
        {
            Id = Guid.NewGuid(), OrganizationId = organization.Id, DocumentId = document.Id,
            SupplierId = supplier.Id, InvoiceNumber = "INV1003430", InvoiceDate = new DateOnly(2026, 9, 17),
            PurchaseOrderId = po.Id, Currency = "AED", InvoiceType = InvoiceType.MATERIAL,
            Status = InvoiceStatus.READY_FOR_GRN, CreatedByUserId = user.Id, CreatedAt = now, UpdatedAt = now,
        };
        invoice.Lines.Add(new InvoiceLine
        {
            Id = Guid.NewGuid(), InvoiceId = invoice.Id, LineNumber = 10, DescriptionRaw = poItem.Description,
            Quantity = 24, Uom = "EA", PurchaseOrderItemId = poItem.Id, MatchStatus = InvoiceLineMatchStatus.MATCHED,
            CreatedAt = now, UpdatedAt = now,
        });
        var permission = await db.Permissions.SingleAsync(item => item.Key == PermissionKeys.PostGrn);
        var role = new Role
        {
            Id = Guid.NewGuid(), Key = $"GRN_ROLE_{Guid.NewGuid():N}"[..24].ToUpperInvariant(), Name = "GRN Role",
            CreatedAt = now, UpdatedAt = now,
        };
        role.Permissions.Add(new RolePermission { RoleId = role.Id, PermissionId = permission.Id });
        db.AddRange(user, organization, supplier, po, document, invoice, role);
        db.UserApplicationAccess.Add(new UserApplicationAccess { Id = Guid.NewGuid(), UserId = user.Id, Application = ApplicationKind.CLOUD, CreatedAt = now });
        db.UserRoleAssignments.Add(new UserRoleAssignment { Id = Guid.NewGuid(), UserId = user.Id, RoleId = role.Id, OrganizationId = organization.Id, CreatedAt = now });
        db.UserOrganizationMemberships.Add(new UserOrganizationMembership { Id = Guid.NewGuid(), UserId = user.Id, OrganizationId = organization.Id, CreatedAt = now });
        await db.SaveChangesAsync();
        var session = new Session
        {
            Id = Guid.NewGuid(), UserId = user.Id, Application = ApplicationKind.CLOUD, TokenHash = "grn-qty",
            CreatedAt = now, ExpiresAt = now.AddHours(1), LastUsedAt = now,
        };

        var prepared = await operations.PrepareGrnAsync(session, new PrepareGrnRequest { InvoiceId = invoice.Id, PurchaseOrderId = po.Id }, CancellationToken.None);
        var posted = await operations.PostExistingGrnAsync(session, prepared.Id, new PostGoodsReceiptRequest
        {
            Lines =
            [
                new GrnLineInput
                {
                    PurchaseOrderItemId = poItem.Id,
                    ReceivedQuantity = 24,
                    AcceptedQuantity = 24,
                },
            ],
        }, CancellationToken.None);

        Assert.Equal(1000, prepared.Lines.Single().OpenQuantityBefore);
        Assert.Equal(24, prepared.Lines.Single().AcceptedQuantity);
        Assert.Equal(24, posted.Lines.Single().AcceptedQuantity);
        Assert.DoesNotContain("remaining quantity", posted.FailureMessage ?? string.Empty, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Ariba_grn_soap_header_uses_five_test_variant_and_partition()
    {
        factory.IntegrationHandler.Reset();
        factory.IntegrationHandler.Enqueue(HttpStatusCode.OK, """<?xml version="1.0"?><soap:Envelope xmlns:soap="http://schemas.xmlsoap.org/soap/envelope/"><soap:Body><StatusString>OK</StatusString></soap:Envelope>""", "text/xml");
        await using var scope = factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<SilaMeDbContext>();
        var operations = scope.ServiceProvider.GetRequiredService<OperationalService>();
        var now = DateTime.UtcNow;
        var user = new User
        {
            Id = Guid.NewGuid(), Email = $"grn-ariba-{Guid.NewGuid():N}@silame.local", NormalizedEmail = string.Empty,
            DisplayName = "Ariba GRN", PasswordHash = "not-used", CreatedAt = now, UpdatedAt = now,
        };
        user.NormalizedEmail = user.Email.ToUpperInvariant();
        var organization = new Organization
        {
            Id = Guid.NewGuid(), Code = $"GRN-{Guid.NewGuid():N}"[..16].ToUpperInvariant(), Name = "Ariba GRN Org",
            Kind = OrganizationKind.CUSTOMER, CreatedAt = now, UpdatedAt = now,
        };
        var supplier = new Supplier
        {
            Id = Guid.NewGuid(), OrganizationId = organization.Id, SupplierCode = "1000234", Name = "ABC FOOD",
            NormalizedName = "ABC FOOD", Status = StatusKind.ACTIVE, CreatedAt = now, UpdatedAt = now,
        };
        var po = new PurchaseOrder
        {
            Id = Guid.NewGuid(), OrganizationId = organization.Id, SupplierId = supplier.Id,
            PoNumber = "4500002849", EntityCode = "1050", CompanyCode = "1050", Currency = "AED",
            Status = PurchaseOrderStatus.OPEN, SourceSystem = "EXCEL_UPLOAD", SourceLastChangedAtRaw = "EP23721",
            CreatedAt = now, UpdatedAt = now,
        };
        var poItem = new PurchaseOrderItem
        {
            Id = Guid.NewGuid(), PurchaseOrderId = po.Id, LineNumber = 10, ItemNumber = "10",
            MaterialCode = "MAT-1", Description = "Item", OrderedQuantity = 1, ReceivedQuantity = 0, OpenQuantity = 1,
            Uom = "EA", Status = PurchaseOrderItemStatus.OPEN, GoodsReceiptExpected = true, CreatedAt = now, UpdatedAt = now,
        };
        po.Items.Add(poItem);
        var document = new Document
        {
            Id = Guid.NewGuid(), OrganizationId = organization.Id, UploadedByUserId = user.Id,
            DocumentType = DocumentType.INVOICE, OriginalFilename = "inv.pdf", ContentType = "application/pdf",
            StorageProvider = "TEST", StorageReference = "inv.pdf", Status = DocumentStatus.PROCESSED,
            SourceChannel = DocumentSourceChannel.MOBILE_SCANNER, CreatedAt = now, UpdatedAt = now,
        };
        var invoice = new Invoice
        {
            Id = Guid.NewGuid(), OrganizationId = organization.Id, DocumentId = document.Id,
            SupplierId = supplier.Id, InvoiceNumber = "TESTINVOICE123", InvoiceDate = new DateOnly(2026, 9, 17),
            PurchaseOrderId = po.Id, Currency = "AED", InvoiceType = InvoiceType.MATERIAL,
            Status = InvoiceStatus.READY_FOR_GRN, CreatedByUserId = user.Id, CreatedAt = now, UpdatedAt = now,
        };
        invoice.Lines.Add(new InvoiceLine
        {
            Id = Guid.NewGuid(), InvoiceId = invoice.Id, LineNumber = 10, DescriptionRaw = "Item", Quantity = 1, Uom = "EA",
            PurchaseOrderItemId = poItem.Id, MatchStatus = InvoiceLineMatchStatus.MATCHED, CreatedAt = now, UpdatedAt = now,
        });
        var permission = await db.Permissions.SingleAsync(item => item.Key == PermissionKeys.PostGrn);
        var role = new Role
        {
            Id = Guid.NewGuid(), Key = $"GRN_ROLE_{Guid.NewGuid():N}"[..24].ToUpperInvariant(), Name = "GRN Role",
            CreatedAt = now, UpdatedAt = now,
        };
        role.Permissions.Add(new RolePermission { RoleId = role.Id, PermissionId = permission.Id });
        var config = new ApiIntegrationConfiguration
        {
            Id = Guid.NewGuid(), OrganizationId = organization.Id, EntityCode = "ALL", Name = "FIVETEST-GRNPOSTING-ARIBA",
            ProcessType = IntegrationProcessType.POST_GRN, Protocol = IntegrationProtocol.SOAP, SystemKind = IntegrationSystemKind.SAP_ARIBA,
            BaseUrl = "https://s1.mn1.ariba.com/Buyer/soap/fiveholdings-C1-T/ExternalReceiptImport",
            AuthenticationType = IntegrationAuthenticationType.NONE, Status = IntegrationConfigurationStatus.VALIDATED,
            RetryCount = 0, TimeoutSeconds = 5, CreatedAt = now, UpdatedAt = now,
        };
        var route = new IntegrationRoute
        {
            Id = Guid.NewGuid(), OrganizationId = organization.Id, ProcessType = IntegrationProcessType.POST_GRN,
            SystemKind = IntegrationSystemKind.SAP_ARIBA, ApiIntegrationConfigurationId = config.Id,
            AppliesToAllCompanyCodes = true, IsActive = true, CreatedAt = now, UpdatedAt = now,
        };
        db.AddRange(user, organization, supplier, po, document, invoice, role, config, route);
        db.UserApplicationAccess.Add(new UserApplicationAccess { Id = Guid.NewGuid(), UserId = user.Id, Application = ApplicationKind.CLOUD, CreatedAt = now });
        db.UserRoleAssignments.Add(new UserRoleAssignment { Id = Guid.NewGuid(), UserId = user.Id, RoleId = role.Id, OrganizationId = organization.Id, CreatedAt = now });
        db.UserOrganizationMemberships.Add(new UserOrganizationMembership { Id = Guid.NewGuid(), UserId = user.Id, OrganizationId = organization.Id, CreatedAt = now });
        await db.SaveChangesAsync();
        var session = new Session
        {
            Id = Guid.NewGuid(), UserId = user.Id, Application = ApplicationKind.CLOUD, TokenHash = "grn-ariba",
            CreatedAt = now, ExpiresAt = now.AddHours(1), LastUsedAt = now,
        };

        var prepared = await operations.PostGrnAsync(session, new PostGrnRequest
        {
            InvoiceId = invoice.Id, PurchaseOrderId = po.Id,
            Lines = [new GrnLineInput { PurchaseOrderItemId = poItem.Id, ReceivedQuantity = 1, AcceptedQuantity = 1 }],
        }, $"grn-ariba-{invoice.Id:N}", CancellationToken.None);
        var posted = await operations.PostExistingGrnAsync(session, prepared.Id, null, CancellationToken.None);

        var xml = factory.IntegrationHandler.Bodies.LastOrDefault() ?? string.Empty;
        Assert.Contains($"<urn:variant>{AribaPostGrnAdapter.DefaultSoapVariant}</urn:variant>", xml);
        Assert.Contains("<!--Optional:-->", xml);
        Assert.Contains($"<urn:partition>{AribaPostGrnAdapter.DefaultSoapPartition}</urn:partition>", xml);
        Assert.DoesNotContain("ARIBA_REALM_MISSING", posted.FailureCode ?? string.Empty);
    }

    [Fact]
    public async Task S4hana_grn_post_fetches_csrf_and_reuses_session_cookies()
    {
        factory.IntegrationHandler.Reset();
        factory.IntegrationHandler.Enqueue(HttpStatusCode.OK, "<ok/>", "application/xml", new Dictionary<string, string>
        {
            ["X-CSRF-Token"] = "s4-csrf-token",
            ["Set-Cookie"] = "SAP_SESSIONID=session-cookie; Path=/",
        });
        factory.IntegrationHandler.Enqueue(HttpStatusCode.Created, """{"d":{"MaterialDocument":"5000009999","MaterialDocumentYear":"2026"}}""");
        var posted = await PostS4HanaSampleAsync();

        Assert.Equal(GoodsReceiptStatus.POSTED, posted.Status);
        Assert.Equal("5000009999", posted.ErpMaterialDocument);
        Assert.Equal("2026", posted.ErpDocumentYear);
        var requests = factory.IntegrationHandler.Requests.ToArray();
        var methods = factory.IntegrationHandler.Methods.ToArray();
        var headers = factory.IntegrationHandler.RequestHeaders.ToArray();
        Assert.Equal(2, requests.Length);
        Assert.Equal("GET", methods[0]);
        Assert.Equal("https://my419951-api.s4hana.cloud.sap/sap/opu/odata/sap/API_MATERIAL_DOCUMENT_SRV/", requests[0].ToString());
        Assert.Equal("Fetch", headers[0]["X-CSRF-Token"]);
        Assert.Equal("POST", methods[1]);
        Assert.Equal("https://my419951-api.s4hana.cloud.sap/sap/opu/odata/sap/API_MATERIAL_DOCUMENT_SRV/A_MaterialDocumentHeader", requests[1].ToString());
        Assert.Equal("s4-csrf-token", headers[1]["X-CSRF-Token"]);
        Assert.Contains("SAP_SESSIONID=session-cookie", headers[1]["Cookie"]);
        var body = factory.IntegrationHandler.Bodies.Last();
        Assert.Contains("\"GoodsMovementCode\":\"01\"", body.Replace(" ", string.Empty));
        Assert.Contains("\"GoodsMovementType\":\"101\"", body.Replace(" ", string.Empty));
        Assert.Contains("to_MaterialDocumentItem", body);
        Assert.Contains("\"PurchaseOrderItem\":\"00010\"", body.Replace(" ", string.Empty));
        Assert.DoesNotContain("StorageLocation", body);
    }

    [Fact]
    public async Task S4hana_grn_csrf_rejection_captures_posted_payload()
    {
        factory.IntegrationHandler.Reset();
        factory.IntegrationHandler.Enqueue(HttpStatusCode.OK, "<ok/>", "application/xml", new Dictionary<string, string>
        {
            ["X-CSRF-Token"] = "s4-csrf-token",
            ["Set-Cookie"] = "SAP_SESSIONID=session-cookie; Path=/",
        });
        factory.IntegrationHandler.Enqueue(HttpStatusCode.Forbidden, """{"error":{"message":{"value":"CSRF token validation failed"}}}""");
        var posted = await PostS4HanaSampleAsync();

        Assert.Equal(GoodsReceiptStatus.FAILED, posted.Status);
        Assert.Equal("ERP_POST_FAILED", posted.FailureCode);
        Assert.Equal("ERP REJECTED: CSRF token validation failed", posted.FailureMessage);
        Assert.Contains("to_MaterialDocumentItem", posted.ErpResponseJson);
        Assert.Contains("requestBody", posted.ErpResponseJson);
        Assert.Contains("csrfFetchUrl", posted.ErpResponseJson);
        Assert.DoesNotContain("StorageLocation", posted.ErpResponseJson);
    }

    [Fact]
    public void S4hana_adapter_does_not_claim_rest_post_grn()
    {
        var rest = Config(Guid.NewGuid(), "ALL", "tenant-a", "https://erp.tenant-a.test/grn");
        Assert.False(S4HanaPostGrnAdapter.AppliesTo(rest));
        var odata = new ApiIntegrationConfiguration
        {
            Id = Guid.NewGuid(), OrganizationId = Guid.NewGuid(), EntityCode = "ALL", Name = "FIVE S4",
            ProcessType = IntegrationProcessType.POST_GRN, Protocol = IntegrationProtocol.ODATA_V2,
            SystemKind = IntegrationSystemKind.SAP_S4HANA,
            BaseUrl = "https://my419951-api.s4hana.cloud.sap",
            ServicePath = "/sap/opu/odata/sap/API_MATERIAL_DOCUMENT_SRV",
            EntitySet = "A_MaterialDocumentHeader",
            AuthenticationType = IntegrationAuthenticationType.NONE, Status = IntegrationConfigurationStatus.ACTIVE,
            CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow,
        };
        Assert.True(S4HanaPostGrnAdapter.AppliesTo(odata));
    }

    private async Task<GoodsReceipt> PostS4HanaSampleAsync()
    {
        await using var scope = factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<SilaMeDbContext>();
        var operations = scope.ServiceProvider.GetRequiredService<OperationalService>();
        var now = DateTime.UtcNow;
        var user = new User
        {
            Id = Guid.NewGuid(), Email = $"grn-s4-{Guid.NewGuid():N}@silame.local", NormalizedEmail = string.Empty,
            DisplayName = "S4 GRN", PasswordHash = "not-used", CreatedAt = now, UpdatedAt = now,
        };
        user.NormalizedEmail = user.Email.ToUpperInvariant();
        var organization = new Organization
        {
            Id = Guid.NewGuid(), Code = $"GRN-{Guid.NewGuid():N}"[..16].ToUpperInvariant(), Name = "S4 GRN Org",
            Kind = OrganizationKind.CUSTOMER, CreatedAt = now, UpdatedAt = now,
        };
        var supplier = new Supplier
        {
            Id = Guid.NewGuid(), OrganizationId = organization.Id, SupplierCode = "1000234", Name = "ABC FOOD",
            NormalizedName = "ABC FOOD", Status = StatusKind.ACTIVE, CreatedAt = now, UpdatedAt = now,
        };
        var po = new PurchaseOrder
        {
            Id = Guid.NewGuid(), OrganizationId = organization.Id, SupplierId = supplier.Id,
            PoNumber = "4500002849", EntityCode = "1050", CompanyCode = "1050", Currency = "AED",
            Status = PurchaseOrderStatus.OPEN, SourceSystem = "EXCEL_UPLOAD", CreatedAt = now, UpdatedAt = now,
        };
        var poItem = new PurchaseOrderItem
        {
            Id = Guid.NewGuid(), PurchaseOrderId = po.Id, LineNumber = 10, ItemNumber = "10",
            MaterialCode = "MAT-1", Description = "Item", OrderedQuantity = 1, ReceivedQuantity = 0, OpenQuantity = 1,
            Uom = "EA", Plant = "1010", StorageLocation = "101A",
            Status = PurchaseOrderItemStatus.OPEN, GoodsReceiptExpected = true, CreatedAt = now, UpdatedAt = now,
        };
        po.Items.Add(poItem);
        var document = new Document
        {
            Id = Guid.NewGuid(), OrganizationId = organization.Id, UploadedByUserId = user.Id,
            DocumentType = DocumentType.INVOICE, OriginalFilename = "inv.pdf", ContentType = "application/pdf",
            StorageProvider = "TEST", StorageReference = "inv.pdf", Status = DocumentStatus.PROCESSED,
            SourceChannel = DocumentSourceChannel.MOBILE_SCANNER, CreatedAt = now, UpdatedAt = now,
        };
        var invoice = new Invoice
        {
            Id = Guid.NewGuid(), OrganizationId = organization.Id, DocumentId = document.Id,
            SupplierId = supplier.Id, InvoiceNumber = "TESTINVOICE123", InvoiceDate = new DateOnly(2026, 9, 17),
            PurchaseOrderId = po.Id, Currency = "AED", InvoiceType = InvoiceType.MATERIAL,
            Status = InvoiceStatus.READY_FOR_GRN, CreatedByUserId = user.Id, CreatedAt = now, UpdatedAt = now,
        };
        invoice.Lines.Add(new InvoiceLine
        {
            Id = Guid.NewGuid(), InvoiceId = invoice.Id, LineNumber = 10, DescriptionRaw = "Item", Quantity = 1, Uom = "EA",
            PurchaseOrderItemId = poItem.Id, MatchStatus = InvoiceLineMatchStatus.MATCHED, CreatedAt = now, UpdatedAt = now,
        });
        var permission = await db.Permissions.SingleAsync(item => item.Key == PermissionKeys.PostGrn);
        var role = new Role
        {
            Id = Guid.NewGuid(), Key = $"GRN_ROLE_{Guid.NewGuid():N}"[..24].ToUpperInvariant(), Name = "GRN Role",
            CreatedAt = now, UpdatedAt = now,
        };
        role.Permissions.Add(new RolePermission { RoleId = role.Id, PermissionId = permission.Id });
        var config = new ApiIntegrationConfiguration
        {
            Id = Guid.NewGuid(), OrganizationId = organization.Id, EntityCode = "ALL", Name = "FIVE S4 - POST GRN",
            ProcessType = IntegrationProcessType.POST_GRN, Protocol = IntegrationProtocol.ODATA_V2, SystemKind = IntegrationSystemKind.SAP_S4HANA,
            BaseUrl = "https://my419951-api.s4hana.cloud.sap",
            ServicePath = "/sap/opu/odata/sap/API_MATERIAL_DOCUMENT_SRV",
            EntitySet = "A_MaterialDocumentHeader", HttpMethod = "POST",
            AuthenticationType = IntegrationAuthenticationType.NONE, Status = IntegrationConfigurationStatus.ACTIVE,
            RetryCount = 0, TimeoutSeconds = 5, CreatedAt = now, UpdatedAt = now,
            DesignerJson = """{"csrfRequired":true,"csrfFetchMethod":"GET","csrfHeaderName":"X-CSRF-Token","csrfHeaderValue":"Fetch","csrfResponseHeader":"X-CSRF-Token","csrfFetchPath":"/sap/opu/odata/sap/API_MATERIAL_DOCUMENT_SRV/"}""",
        };
        var route = new IntegrationRoute
        {
            Id = Guid.NewGuid(), OrganizationId = organization.Id, ProcessType = IntegrationProcessType.POST_GRN,
            SystemKind = IntegrationSystemKind.SAP_S4HANA, ApiIntegrationConfigurationId = config.Id,
            AppliesToAllCompanyCodes = true, IsActive = true, CreatedAt = now, UpdatedAt = now,
        };
        db.AddRange(user, organization, supplier, po, document, invoice, role, config, route);
        db.UserApplicationAccess.Add(new UserApplicationAccess { Id = Guid.NewGuid(), UserId = user.Id, Application = ApplicationKind.CLOUD, CreatedAt = now });
        db.UserRoleAssignments.Add(new UserRoleAssignment { Id = Guid.NewGuid(), UserId = user.Id, RoleId = role.Id, OrganizationId = organization.Id, CreatedAt = now });
        db.UserOrganizationMemberships.Add(new UserOrganizationMembership { Id = Guid.NewGuid(), UserId = user.Id, OrganizationId = organization.Id, CreatedAt = now });
        await db.SaveChangesAsync();
        var session = new Session
        {
            Id = Guid.NewGuid(), UserId = user.Id, Application = ApplicationKind.CLOUD, TokenHash = "grn-s4",
            CreatedAt = now, ExpiresAt = now.AddHours(1), LastUsedAt = now,
        };
        var prepared = await operations.PostGrnAsync(session, new PostGrnRequest
        {
            InvoiceId = invoice.Id, PurchaseOrderId = po.Id,
            Lines = [new GrnLineInput { PurchaseOrderItemId = poItem.Id, ReceivedQuantity = 1, AcceptedQuantity = 1 }],
        }, $"grn-s4-{invoice.Id:N}", CancellationToken.None);
        return await operations.PostExistingGrnAsync(session, prepared.Id, null, CancellationToken.None);
    }

    private async Task<PostedSample> PostSampleAsync(bool configure, string? otherTenantUrl = null)
    {
        await using var scope = factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<SilaMeDbContext>();
        var operations = scope.ServiceProvider.GetRequiredService<OperationalService>();
        var now = DateTime.UtcNow;
        var user = new User
        {
            Id = Guid.NewGuid(), Email = $"grn-cfg-{Guid.NewGuid():N}@silame.local", NormalizedEmail = string.Empty,
            DisplayName = "GRN Config", PasswordHash = "not-used", CreatedAt = now, UpdatedAt = now,
        };
        user.NormalizedEmail = user.Email.ToUpperInvariant();
        var organization = new Organization
        {
            Id = Guid.NewGuid(), Code = $"GRN-{Guid.NewGuid():N}"[..16].ToUpperInvariant(), Name = "GRN Org",
            Kind = OrganizationKind.CUSTOMER, CreatedAt = now, UpdatedAt = now,
        };
        var other = new Organization
        {
            Id = Guid.NewGuid(), Code = $"GRN-{Guid.NewGuid():N}"[..16].ToUpperInvariant(), Name = "Other Tenant",
            Kind = OrganizationKind.CUSTOMER, CreatedAt = now, UpdatedAt = now,
        };
        var unit = new OrganizationUnit
        {
            Id = Guid.NewGuid(), OrganizationId = organization.Id, Code = "STORE", Name = "Store",
            Kind = OrganizationUnitKind.STORE, CreatedAt = now, UpdatedAt = now,
        };
        var supplier = new Supplier
        {
            Id = Guid.NewGuid(), OrganizationId = organization.Id, SupplierCode = "1003430", Name = "Test SBN",
            NormalizedName = "TEST SBN", Status = StatusKind.ACTIVE, CreatedAt = now, UpdatedAt = now,
        };
        var material = new Material
        {
            Id = Guid.NewGuid(), OrganizationId = organization.Id, MaterialCode = "MAT-1", Description = "GI Red /Socket 2x1",
            NormalizedDescription = "GI RED SOCKET 2X1", BaseUom = "EA", Status = StatusKind.ACTIVE, CreatedAt = now, UpdatedAt = now,
        };
        var po = new PurchaseOrder
        {
            Id = Guid.NewGuid(), OrganizationId = organization.Id, OperatingUnitId = unit.Id, SupplierId = supplier.Id,
            PoNumber = "4500003415", EntityCode = "1050", CompanyCode = "1050", Currency = "AED",
            Status = PurchaseOrderStatus.OPEN, SourceSystem = "TEST", CreatedAt = now, UpdatedAt = now,
        };
        var poItem = new PurchaseOrderItem
        {
            Id = Guid.NewGuid(), PurchaseOrderId = po.Id, LineNumber = 10, ItemNumber = "10", MaterialId = material.Id,
            MaterialCode = material.MaterialCode, Description = material.Description, OrderedQuantity = 100,
            ReceivedQuantity = 0, OpenQuantity = 100, Uom = "EA", UnitPrice = 300, Status = PurchaseOrderItemStatus.OPEN,
            GoodsReceiptExpected = true, CreatedAt = now, UpdatedAt = now,
        };
        po.Items.Add(poItem);
        var document = new Document
        {
            Id = Guid.NewGuid(), OrganizationId = organization.Id, OperatingUnitId = unit.Id, UploadedByUserId = user.Id,
            DocumentType = DocumentType.INVOICE, OriginalFilename = "inv.pdf", ContentType = "application/pdf",
            StorageProvider = "TEST", StorageReference = "inv.pdf", Status = DocumentStatus.PROCESSED,
            SourceChannel = DocumentSourceChannel.MOBILE_SCANNER, CreatedAt = now, UpdatedAt = now,
        };
        var invoice = new Invoice
        {
            Id = Guid.NewGuid(), OrganizationId = organization.Id, OperatingUnitId = unit.Id, DocumentId = document.Id,
            SupplierId = supplier.Id, InvoiceNumber = "TEST-INV-17092026-001", InvoiceDate = new DateOnly(2026, 9, 17),
            SupplierNameRaw = "Supplier ID: 1003430", PurchaseOrderId = po.Id, Currency = "AED",
            InvoiceType = InvoiceType.MATERIAL, Status = InvoiceStatus.READY_FOR_GRN,
            CreatedByUserId = user.Id, CreatedAt = now, UpdatedAt = now,
        };
        invoice.Lines.Add(new InvoiceLine
        {
            Id = Guid.NewGuid(), InvoiceId = invoice.Id, LineNumber = 10, DescriptionRaw = material.Description,
            Quantity = 5, Uom = "EA", PurchaseOrderItemId = poItem.Id, MatchStatus = InvoiceLineMatchStatus.MATCHED,
            CreatedAt = now, UpdatedAt = now,
        });
        var permission = await db.Permissions.SingleAsync(item => item.Key == PermissionKeys.PostGrn);
        var role = new Role
        {
            Id = Guid.NewGuid(), Key = $"GRN_ROLE_{Guid.NewGuid():N}"[..24].ToUpperInvariant(), Name = "GRN Role",
            CreatedAt = now, UpdatedAt = now,
        };
        role.Permissions.Add(new RolePermission { RoleId = role.Id, PermissionId = permission.Id });
        db.AddRange(user, organization, other, unit, supplier, material, po, document, invoice, role);
        db.UserApplicationAccess.Add(new UserApplicationAccess { Id = Guid.NewGuid(), UserId = user.Id, Application = ApplicationKind.CLOUD, CreatedAt = now });
        db.UserRoleAssignments.Add(new UserRoleAssignment { Id = Guid.NewGuid(), UserId = user.Id, RoleId = role.Id, OrganizationId = organization.Id, OrganizationUnitId = unit.Id, CreatedAt = now });
        db.UserOrganizationMemberships.Add(new UserOrganizationMembership { Id = Guid.NewGuid(), UserId = user.Id, OrganizationId = organization.Id, OrganizationUnitId = unit.Id, CreatedAt = now });
        if (configure)
        {
            db.ApiIntegrationConfigurations.Add(Config(organization.Id, "ALL", "tenant-a", "https://erp.tenant-a.test/grn"));
            if (!string.IsNullOrWhiteSpace(otherTenantUrl))
                db.ApiIntegrationConfigurations.Add(Config(other.Id, "ALL", "tenant-b", otherTenantUrl));
        }
        await db.SaveChangesAsync();
        var session = new Session
        {
            Id = Guid.NewGuid(), UserId = user.Id, Application = ApplicationKind.CLOUD, TokenHash = "grn-cfg",
            CreatedAt = now, ExpiresAt = now.AddHours(1), LastUsedAt = now,
        };
        var grn = await operations.PostGrnAsync(session, new PostGrnRequest
        {
            InvoiceId = invoice.Id, PurchaseOrderId = po.Id, OperatingUnitId = unit.Id,
            Lines = [new GrnLineInput { PurchaseOrderItemId = poItem.Id, ReceivedQuantity = 5, AcceptedQuantity = 5, DamagedQuantity = 0, RejectedQuantity = 0 }],
        }, $"grn-cfg-{invoice.Id:N}", CancellationToken.None);
        grn = await operations.PostExistingGrnAsync(session, grn.Id, null, CancellationToken.None);
        await db.Entry(poItem).ReloadAsync();
        var hasStock = await db.StockBalances.AnyAsync(item => item.OperatingUnitId == unit.Id);
        return new PostedSample(grn, poItem, unit.Id, poItem.ReceivedQuantity) { HasStock = hasStock };
    }

    private static ApiIntegrationConfiguration Config(Guid organizationId, string entityCode, string name, string? baseUrl = null) =>
        new()
        {
            Id = Guid.NewGuid(), OrganizationId = organizationId, EntityCode = entityCode, Name = name,
            ProcessType = IntegrationProcessType.POST_GRN, Protocol = IntegrationProtocol.REST,
            BaseUrl = baseUrl ?? "https://erp.example.test", ResourcePath = "post",
            AuthenticationType = IntegrationAuthenticationType.NONE, Status = IntegrationConfigurationStatus.ACTIVE,
            RetryCount = 0, TimeoutSeconds = 5,
            CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow,
        };

    private sealed record PostedSample(GoodsReceipt Grn, PurchaseOrderItem PoItem, Guid UnitId, decimal ReceivedQuantity)
    {
        public bool HasStock { get; init; }
    }
}
