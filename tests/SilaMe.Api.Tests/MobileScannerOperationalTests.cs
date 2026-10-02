using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using SilaMe.Api.Data;
using SilaMe.Api.Models;
using SilaMe.Api.Services;
using Xunit;

namespace SilaMe.Api.Tests;

public sealed class MobileScannerOperationalTests(ApiFactory factory) : IClassFixture<ApiFactory>
{
    [Fact]
    public async Task Scanner_upload_is_idempotent_and_http_errors_preserve_recovery_details()
    {
        await using var scope = factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<SilaMeDbContext>();
        var operations = scope.ServiceProvider.GetRequiredService<OperationalService>();
        var now = DateTime.UtcNow;
        var user = new User
        {
            Id = Guid.NewGuid(),
            Email = $"mobile-scanner-{Guid.NewGuid():N}@silame.local",
            NormalizedEmail = string.Empty,
            DisplayName = "Mobile Scanner Test User",
            PasswordHash = "not-used",
            CreatedAt = now,
            UpdatedAt = now,
        };
        user.NormalizedEmail = user.Email.ToUpperInvariant();
        var organization = new Organization
        {
            Id = Guid.NewGuid(),
            Code = $"SCAN-{Guid.NewGuid():N}"[..16].ToUpperInvariant(),
            Name = "Mobile Scanner Test Organization",
            Kind = OrganizationKind.CUSTOMER,
            CreatedAt = now,
            UpdatedAt = now,
        };
        var unit = new OrganizationUnit
        {
            Id = Guid.NewGuid(),
            OrganizationId = organization.Id,
            Code = "SCAN-STORE",
            Name = "Scanner Test Store",
            Kind = OrganizationUnitKind.STORE,
            CreatedAt = now,
            UpdatedAt = now,
        };
        var role = new Role
        {
            Id = Guid.NewGuid(),
            Key = $"SCAN_ROLE_{Guid.NewGuid():N}"[..24].ToUpperInvariant(),
            Name = "Mobile Scanner Test Role",
            CreatedAt = now,
            UpdatedAt = now,
        };
        var supplier = new Supplier
        {
            Id = Guid.NewGuid(), OrganizationId = organization.Id, SupplierCode = "1003430",
            Name = "Test SBN", NormalizedName = "TEST SBN", LegalName = "Test SBN",
            SearchName = "TEST SBN", TaxNumber = "100123456700003", Trn = "100123456700003", Status = StatusKind.ACTIVE,
            CreatedAt = now, UpdatedAt = now,
        };
        supplier.Aliases.Add(new SupplierAlias
        {
            Id = Guid.NewGuid(), OrganizationId = organization.Id, SupplierId = supplier.Id, Alias = "Test-SBN",
            NormalizedAlias = "TEST SBN", IsConfirmed = true, CreatedAt = now,
        });
        var purchaseOrder = new PurchaseOrder
        {
            Id = Guid.NewGuid(),
            OrganizationId = organization.Id,
            OperatingUnitId = unit.Id,
            SupplierId = supplier.Id,
            PoNumber = "4500003415",
            EntityCode = supplier.EntityCode,
            CompanyCode = "1050",
            Currency = "AED",
            Status = PurchaseOrderStatus.OPEN,
            SourceSystem = "TEST",
            CreatedAt = now,
            UpdatedAt = now,
        };
        purchaseOrder.Items.Add(new PurchaseOrderItem
        {
            Id = Guid.NewGuid(),
            PurchaseOrderId = purchaseOrder.Id,
            LineNumber = 10,
            MaterialCode = "TEST-MAT-10",
            Description = "Test material",
            OrderedQuantity = 10,
            ReceivedQuantity = 0,
            OpenQuantity = 10,
            Uom = "EA",
            Status = PurchaseOrderItemStatus.OPEN,
            GoodsReceiptExpected = true,
            CreatedAt = now,
            UpdatedAt = now,
        });
        var otherSupplier = new Supplier
        {
            Id = Guid.NewGuid(),
            OrganizationId = organization.Id,
            SupplierCode = "1009999",
            Name = "Other Supplier",
            NormalizedName = "OTHER SUPPLIER",
            EntityCode = supplier.EntityCode,
            Status = StatusKind.ACTIVE,
            CreatedAt = now,
            UpdatedAt = now,
        };
        var wrongSupplierPo = new PurchaseOrder
        {
            Id = Guid.NewGuid(),
            OrganizationId = organization.Id,
            OperatingUnitId = unit.Id,
            SupplierId = otherSupplier.Id,
            PoNumber = "PO-WRONG-SUPPLIER",
            EntityCode = supplier.EntityCode,
            Currency = "AED",
            Status = PurchaseOrderStatus.OPEN,
            SourceSystem = "TEST",
            CreatedAt = now,
            UpdatedAt = now,
        };
        wrongSupplierPo.Items.Add(new PurchaseOrderItem
        {
            Id = Guid.NewGuid(), PurchaseOrderId = wrongSupplierPo.Id, LineNumber = 10,
            MaterialCode = "OTHER-MAT", Description = "Other material", OrderedQuantity = 1,
            OpenQuantity = 1, Uom = "EA", Status = PurchaseOrderItemStatus.OPEN,
            GoodsReceiptExpected = true, CreatedAt = now, UpdatedAt = now,
        });
        var noGrPo = new PurchaseOrder
        {
            Id = Guid.NewGuid(),
            OrganizationId = organization.Id,
            OperatingUnitId = unit.Id,
            SupplierId = supplier.Id,
            PoNumber = "PO-NO-GR",
            EntityCode = supplier.EntityCode,
            Currency = "AED",
            Status = PurchaseOrderStatus.OPEN,
            SourceSystem = "TEST",
            CreatedAt = now,
            UpdatedAt = now,
        };
        noGrPo.Items.Add(new PurchaseOrderItem
        {
            Id = Guid.NewGuid(), PurchaseOrderId = noGrPo.Id, LineNumber = 10,
            MaterialCode = "SERVICE", Description = "Service", OrderedQuantity = 1,
            OpenQuantity = 1, Uom = "EA", Status = PurchaseOrderItemStatus.OPEN,
            GoodsReceiptExpected = false, CreatedAt = now, UpdatedAt = now,
        });
        var permission = await db.Permissions.SingleAsync(item => item.Key == PermissionKeys.UploadInvoice);
        role.Permissions.Add(new RolePermission { RoleId = role.Id, PermissionId = permission.Id });
        db.AddRange(user, organization, unit, supplier, otherSupplier, purchaseOrder, wrongSupplierPo, noGrPo, role);
        db.UserApplicationAccess.Add(new UserApplicationAccess
        {
            Id = Guid.NewGuid(), UserId = user.Id, Application = ApplicationKind.MOBILE, CreatedAt = now,
        });
        db.UserOrganizationMemberships.Add(new UserOrganizationMembership
        {
            Id = Guid.NewGuid(),
            UserId = user.Id,
            OrganizationId = organization.Id,
            OrganizationUnitId = unit.Id,
            CreatedAt = now,
        });
        db.UserRoleAssignments.Add(new UserRoleAssignment
        {
            Id = Guid.NewGuid(),
            UserId = user.Id,
            RoleId = role.Id,
            OrganizationId = organization.Id,
            OrganizationUnitId = unit.Id,
            CreatedAt = now,
        });
        await db.SaveChangesAsync();

        var session = new Session
        {
            Id = Guid.NewGuid(),
            UserId = user.Id,
            Application = ApplicationKind.MOBILE,
            TokenHash = "mobile-scanner-test",
            CreatedAt = now,
            ExpiresAt = now.AddHours(1),
            LastUsedAt = now,
        };
        var scanSessionId = $"scan-{Guid.NewGuid():N}";
        var first = await operations.UploadInvoiceAsync(
            session,
            organization.Id,
            unit.Id,
            DocumentSourceChannel.MOBILE_SCANNER,
            File("first.pdf", "first invoice"),
            CancellationToken.None,
            pageCount: 3,
            scanSessionId: $" {scanSessionId} ",
            supplierName: "Test SBN",
            supplierId: supplier.Id,
            supplierTrn: "100123456700003",
            supplierInvoiceNumber: "TEST-INV-17092026-001",
            invoiceDate: new DateOnly(2026, 9, 17),
            purchaseOrderNumber: "4500003415",
            invoiceGross: 24350m,
            currency: "aed",
            deferFullExtraction: true,
            idempotencyKey: $"save-{scanSessionId}");
        var missing = await Assert.ThrowsAsync<OperationalException>(() => operations.UploadInvoiceAsync(
            session,
            organization.Id,
            unit.Id,
            DocumentSourceChannel.MOBILE_SCANNER,
            File("missing.pdf", "missing required field"),
            CancellationToken.None,
            pageCount: 1,
            scanSessionId: $"{scanSessionId}-missing",
            supplierName: "Test SBN",
            supplierId: supplier.Id,
            supplierTrn: "100123456700003",
            supplierInvoiceNumber: "TAX-MISSING",
            invoiceGross: 100m,
            currency: "aed",
            deferFullExtraction: true));
        Assert.Contains("purchaseOrderNumber", missing.MissingFields!);
        var supplierRequired = await Assert.ThrowsAsync<OperationalException>(() => operations.UploadInvoiceAsync(
            session,
            organization.Id,
            unit.Id,
            DocumentSourceChannel.MOBILE_SCANNER,
            File("no-supplier.pdf", "missing supplier"),
            CancellationToken.None,
            pageCount: 1,
            scanSessionId: $"{scanSessionId}-nosupplier",
            supplierName: "Test SBN",
            supplierInvoiceNumber: "TAX-NOSUP",
            purchaseOrderNumber: "4500003415",
            invoiceGross: 100m,
            currency: "aed",
            deferFullExtraction: true));
        Assert.Equal("SUPPLIER_REQUIRED", supplierRequired.Code);
        var poSupplierMismatch = await Assert.ThrowsAsync<OperationalException>(() => operations.UploadInvoiceAsync(
            session, organization.Id, unit.Id, DocumentSourceChannel.MOBILE_SCANNER,
            File("wrong-po.pdf", "wrong supplier po"), CancellationToken.None,
            pageCount: 1, scanSessionId: $"{scanSessionId}-wrong-po", supplierName: supplier.Name,
            supplierId: supplier.Id, supplierInvoiceNumber: "TAX-WRONG-PO",
            purchaseOrderNumber: wrongSupplierPo.PoNumber, invoiceGross: 100m, deferFullExtraction: true));
        Assert.Equal("PO_SUPPLIER_MISMATCH", poSupplierMismatch.Code);
        var poNotEligible = await Assert.ThrowsAsync<OperationalException>(() => operations.UploadInvoiceAsync(
            session, organization.Id, unit.Id, DocumentSourceChannel.MOBILE_SCANNER,
            File("no-gr-po.pdf", "no gr po"), CancellationToken.None,
            pageCount: 1, scanSessionId: $"{scanSessionId}-no-gr-po", supplierName: supplier.Name,
            supplierId: supplier.Id, supplierInvoiceNumber: "TAX-NO-GR",
            purchaseOrderNumber: noGrPo.PoNumber, invoiceGross: 100m, deferFullExtraction: true));
        Assert.Equal("PO_NOT_GR_ELIGIBLE", poNotEligible.Code);
        var probableDuplicate = await Assert.ThrowsAsync<OperationalException>(() => operations.UploadInvoiceAsync(
            session,
            organization.Id,
            unit.Id,
            DocumentSourceChannel.MOBILE_SCANNER,
            File("probable.pdf", "different content"),
            CancellationToken.None,
            pageCount: 1,
            scanSessionId: $"{scanSessionId}-probable",
            supplierName: "Test SBN",
            supplierId: supplier.Id,
            supplierTrn: "100123456700003",
            supplierInvoiceNumber: "TEST-INV-17092026-001",
            purchaseOrderNumber: "4500003415",
            invoiceGross: 24350m,
            currency: "aed",
            deferFullExtraction: true));
        Assert.Equal("PROBABLE", probableDuplicate.DuplicateType);
        Assert.Equal(first.InvoiceId, probableDuplicate.ExistingInvoiceId);
        var duplicate = await operations.UploadInvoiceAsync(
            session,
            organization.Id,
            unit.Id,
            DocumentSourceChannel.MOBILE_SCANNER,
            File("second.pdf", "should not be stored"),
            CancellationToken.None,
            pageCount: 1,
            scanSessionId: scanSessionId,
            deferFullExtraction: true);
        var retriedByKey = await operations.UploadInvoiceAsync(
            session,
            organization.Id,
            unit.Id,
            DocumentSourceChannel.MOBILE_SCANNER,
            File("retry.pdf", "first invoice"),
            CancellationToken.None,
            pageCount: 1,
            scanSessionId: $"{scanSessionId}-retry",
            supplierName: "Test SBN",
            supplierTrn: "100123456700003",
            supplierInvoiceNumber: "TEST-INV-17092026-001",
            purchaseOrderNumber: "4500003415",
            invoiceGross: 24350m,
            currency: "aed",
            deferFullExtraction: true,
            idempotencyKey: $"save-{scanSessionId}");

        var document = await db.Documents
            .Include(item => item.Invoice)
            .Include(item => item.Extractions)
            .SingleAsync(item => item.Id == first.Id);

        Assert.Equal(first.Id, duplicate.Id);
        Assert.Equal(first.InvoiceId, duplicate.InvoiceId);
        Assert.Equal(first.Id, retriedByKey.Id);
        Assert.Equal(first.InvoiceId, retriedByKey.InvoiceId);
        Assert.Equal(3, document.PageCount);
        Assert.Equal(scanSessionId, document.ScanSessionId);
        Assert.Equal(DocumentSourceChannel.MOBILE_SCANNER, document.SourceChannel);
        Assert.Equal("first.pdf", document.OriginalFilename);
        Assert.Equal(1, await db.Documents.CountAsync(item => item.OrganizationId == organization.Id));
        Assert.Equal(1, await db.Invoices.CountAsync(item => item.OrganizationId == organization.Id));
        Assert.Contains(document.Extractions, extraction =>
            extraction.Provider == "MOBILE_OCR" &&
            extraction.ExtractionType == ExtractionType.BASIC_INVOICE &&
            extraction.Status == ProcessingStatus.COMPLETED);
        Assert.Equal(first.InvoiceId, await db.IdempotencyRecords
            .Where(item => item.IdempotencyKey == $"save-{scanSessionId}")
            .Select(item => Guid.Parse(item.ResponseReference!))
            .SingleAsync());
        Assert.NotNull(document.ContentHash);
        var extractedRows = await db.InvoiceExtData.Where(item => item.DocumentId == document.Id).ToListAsync();
        Assert.True(extractedRows.Any(row => row.SupplierInvoiceNumber == "TEST-INV-17092026-001" && row.PurchaseOrderNumber == "4500003415"),
            string.Join(" | ", extractedRows.Select(row => $"invoice={row.SupplierInvoiceNumber ?? "<null>"} po={row.PurchaseOrderNumber ?? "<null>"} supplier={row.SupplierName ?? "<null>"}")));
        Assert.Equal("Test SBN", document.Invoice!.SupplierNameRaw);
        Assert.Equal(supplier.Id, document.Invoice.SupplierId);
        Assert.Equal("1003430", supplier.SupplierCode);
        Assert.Equal(purchaseOrder.Id, document.Invoice.PurchaseOrderId);
        Assert.Equal(new DateOnly(2026, 9, 17), document.Invoice.InvoiceDate);
        Assert.Equal(24350m, document.Invoice.GrossAmount);
        Assert.True(extractedRows.Any(row => row.SupplierName == "Test SBN"),
            string.Join(" | ", extractedRows.Select(row => $"invoice={row.SupplierInvoiceNumber ?? "<null>"} po={row.PurchaseOrderNumber ?? "<null>"} supplier={row.SupplierName ?? "<null>"}")));
        Assert.Contains(await db.AuditEvents.Where(item => item.EntityId == first.InvoiceId).ToListAsync(),
            audit => audit.EventType == "INVOICE_REVIEW_SAVED");
        Assert.Contains(await db.AuditEvents.Where(item => item.EntityId == first.Id).ToListAsync(),
            audit => audit.EventType == "DOCUMENT_TRANSFER_SKIPPED");

        var sessions = scope.ServiceProvider.GetRequiredService<ISessionService>();
        (_, var rawToken) = await sessions.CreateAsync(user, ApplicationKind.MOBILE, CancellationToken.None);
        using var client = factory.CreateClient();
        client.DefaultRequestHeaders.Add("X-Sila-Route-Slug", "sila-dev");
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", rawToken);

        using var incompleteResponse = await client.PostAsync(
            "/api/v1/documents/invoices",
            ScannerInvoiceForm(organization.Id, unit.Id, "http-missing", includePurchaseOrder: false, "TAX-HTTP-MISSING"));
        Assert.Equal(HttpStatusCode.BadRequest, incompleteResponse.StatusCode);
        var incompleteError = await incompleteResponse.Content.ReadFromJsonAsync<ApiErrorPayload>();
        Assert.NotNull(incompleteError);
        Assert.Equal("INVOICE_REVIEW_FIELDS_REQUIRED", incompleteError!.Code);
        Assert.Contains("purchaseOrderNumber", incompleteError.MissingFields!);

        using var duplicateResponse = await client.PostAsync(
            "/api/v1/documents/invoices",
            ScannerInvoiceForm(organization.Id, unit.Id, "http-duplicate", includePurchaseOrder: true, "TEST-INV-17092026-001", supplier.Id));
        Assert.Equal(HttpStatusCode.Conflict, duplicateResponse.StatusCode);
        var duplicateError = await duplicateResponse.Content.ReadFromJsonAsync<ApiErrorPayload>();
        Assert.NotNull(duplicateError);
        Assert.Equal("DUPLICATE_INVOICE", duplicateError!.Code);
        Assert.Equal(first.InvoiceId, duplicateError.ExistingInvoiceId);
        Assert.Equal("PROBABLE", duplicateError.DuplicateType);
        Assert.Equal(1, await db.Invoices.CountAsync(item => item.OrganizationId == organization.Id));
    }

    private static MultipartFormDataContent ScannerInvoiceForm(
        Guid organizationId,
        Guid operatingUnitId,
        string scanSessionId,
        bool includePurchaseOrder,
        string invoiceNumber,
        Guid? supplierId = null)
    {
        var form = new MultipartFormDataContent();
        form.Add(new StringContent(organizationId.ToString()), "organizationId");
        form.Add(new StringContent(operatingUnitId.ToString()), "operatingUnitId");
        form.Add(new StringContent("MOBILE_SCANNER"), "sourceChannel");
        form.Add(new StringContent("1"), "pageCount");
        form.Add(new StringContent(scanSessionId), "scanSessionId");
        form.Add(new StringContent("Test SBN"), "supplierName");
        if (supplierId is not null) form.Add(new StringContent(supplierId.Value.ToString()), "supplierId");
        form.Add(new StringContent("100123456700003"), "supplierTrn");
        form.Add(new StringContent(invoiceNumber), "supplierInvoiceNumber");
        form.Add(new StringContent("1047.50"), "invoiceGross");
        form.Add(new StringContent("AED"), "currency");
        form.Add(new StringContent("true"), "deferFullExtraction");
        if (includePurchaseOrder)
            form.Add(new StringContent("4500003415"), "purchaseOrderNumber");

        var file = new ByteArrayContent(Encoding.UTF8.GetBytes("%PDF-http-invoice"));
        file.Headers.ContentType = new MediaTypeHeaderValue("application/pdf");
        form.Add(file, "file", "http-invoice.pdf");
        return form;
    }

    private sealed record ApiErrorPayload(
        string Code,
        string Message,
        IReadOnlyList<string>? MissingFields,
        Guid? ExistingInvoiceId,
        string? DuplicateType);

    private static IFormFile File(string filename, string contents)
    {
        var stream = new MemoryStream(System.Text.Encoding.UTF8.GetBytes(contents));
        return new FormFile(stream, 0, stream.Length, "file", filename)
        {
            Headers = new HeaderDictionary(),
            ContentType = "application/pdf",
        };
    }
}