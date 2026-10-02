using System.Collections.Concurrent;
using System.IO.Compression;
using System.Net;
using System.Net.Http.Headers;
using System.Text;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using SilaMe.Api.Data;
using SilaMe.Api.DTOs;
using SilaMe.Api.Models;
using SilaMe.Api.Services;
using Xunit;

namespace SilaMe.Api.Tests;

public sealed class RecordingIntegrationHandler : HttpMessageHandler
{
    private readonly ConcurrentQueue<HttpResponseMessage> responses = new();
    public ConcurrentQueue<Uri> Requests { get; } = new();
    public ConcurrentQueue<string> Methods { get; } = new();
    public ConcurrentQueue<string> Bodies { get; } = new();
    public ConcurrentQueue<Dictionary<string, string>> RequestHeaders { get; } = new();

    public Exception? NextException { get; set; }

    public void Reset()
    {
        NextException = null;
        while (responses.TryDequeue(out var response)) response.Dispose();
        while (Requests.TryDequeue(out _)) { }
        while (Methods.TryDequeue(out _)) { }
        while (Bodies.TryDequeue(out _)) { }
        while (RequestHeaders.TryDequeue(out _)) { }
    }

    public void Enqueue(HttpStatusCode status, string body, string contentType = "application/json", IReadOnlyDictionary<string, string>? headers = null)
    {
        var message = new HttpResponseMessage(status)
        {
            Content = new StringContent(body, Encoding.UTF8),
        };
        message.Content.Headers.ContentType = MediaTypeHeaderValue.Parse(contentType);
        if (headers is not null)
        {
            foreach (var header in headers)
                message.Headers.TryAddWithoutValidation(header.Key, header.Value);
        }
        responses.Enqueue(message);
    }

    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        Requests.Enqueue(request.RequestUri!);
        Methods.Enqueue(request.Method.Method);
        Bodies.Enqueue(request.Content is null ? string.Empty : await request.Content.ReadAsStringAsync(cancellationToken));
        var headers = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        foreach (var header in request.Headers)
            headers[header.Key] = string.Join(", ", header.Value);
        RequestHeaders.Enqueue(headers);
        if (NextException is not null)
        {
            var exception = NextException;
            NextException = null;
            throw exception;
        }
        return responses.TryDequeue(out var response)
            ? response
            : new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent("""{"value":[]}""", Encoding.UTF8, "application/json") };
    }
}

public sealed class IntegrationFrameworkTests(ApiFactory factory) : IClassFixture<ApiFactory>
{
    [Fact]
    public async Task Credentials_are_protected_and_test_schema_mapping_and_import_are_tenant_scoped()
    {
        factory.IntegrationHandler.Reset();
        await using var scope = factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<SilaMeDbContext>();
        var service = scope.ServiceProvider.GetRequiredService<IntegrationService>();
        var credentials = scope.ServiceProvider.GetRequiredService<ProtectedIntegrationCredentialStore>();
        var organization = new Organization
        {
            Id = Guid.NewGuid(), Code = $"INT-{Guid.NewGuid():N}"[..16].ToUpperInvariant(),
            Name = "Integration Test Organization", Kind = OrganizationKind.CUSTOMER,
            CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow,
        };
        db.Organizations.Add(organization);
        await db.SaveChangesAsync();
        db.Suppliers.Add(new Supplier
        {
            Id = Guid.NewGuid(), OrganizationId = organization.Id, EntityCode = "ALL", SupplierCode = "SUP-1",
            Name = "Test Supplier", NormalizedName = "TEST SUPPLIER", Currency = "AED", Status = StatusKind.ACTIVE,
            SourceSystem = "TEST", CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow,
        });
        await db.SaveChangesAsync();

        var input = new IntegrationConfigurationInput
        {
            Name = "SAP PO Test", EntityCode = "ALL", BaseUrl = "https://sap.test.example",
            ResourcePath = "odata/purchase-orders", AuthenticationType = IntegrationAuthenticationType.BEARER_TOKEN,
            BearerToken = "super-secret-token", WatermarkField = "LastChangedAt", RetryCount = 1,
        };
        var saved = await service.SaveAsync(organization.Id, null, input, CancellationToken.None);
        var stored = await db.ApiIntegrationConfigurations.SingleAsync(item => item.Id == saved.Id);
        Assert.Equal("Configured", saved.CredentialStatus);
        Assert.DoesNotContain("super-secret-token", stored.ProtectedBearerToken);
        Assert.Equal("super-secret-token", credentials.Unprotect(stored.ProtectedBearerToken));

        factory.IntegrationHandler.Enqueue(HttpStatusCode.OK, """{"value":[]}""");
        var tested = await service.TestAsync(organization.Id, saved.Id, CancellationToken.None);
        Assert.True(tested.Success);
        Assert.Contains("$top=1", factory.IntegrationHandler.Requests.Single().Query);
        Assert.Equal(IntegrationConfigurationStatus.TESTED, (await db.ApiIntegrationConfigurations.SingleAsync(item => item.Id == saved.Id)).Status);

        factory.IntegrationHandler.Enqueue(HttpStatusCode.OK, """
          <edmx:Edmx xmlns:edmx="http://docs.oasis-open.org/odata/ns/edmx" Version="4.0">
            <edmx:DataServices><Schema xmlns="http://docs.oasis-open.org/odata/ns/edm" Namespace="SAP">
              <EntityType Name="A_PurchaseOrder"><Key><PropertyRef PropertyPath="PurchaseOrder"/></Key>
              <Property Name="PurchaseOrder" Type="Edm.String" Nullable="false"/>
              <Property Name="Supplier" Type="Edm.String" Nullable="false"/></EntityType>
            </Schema></edmx:DataServices>
          </edmx:Edmx>
          """, "application/xml");
        var schema = await service.DiscoverSchemaAsync(organization.Id, saved.Id, CancellationToken.None);
        Assert.Contains(schema.Entities, entity => entity.Name == "A_PurchaseOrder" && entity.Properties.Count == 2);

        await Assert.ThrowsAsync<IntegrationException>(() => service.SaveMappingsAsync(
            organization.Id, saved.Id, [new FieldMappingInput { SourceField = "PurchaseOrder", TargetField = "NotAllowed" }],
            CancellationToken.None));
        await service.SaveMappingsAsync(organization.Id, saved.Id,
        [
            new FieldMappingInput { SourceField = "PurchaseOrder", TargetField = "PurchaseOrder.PoNumber", Transformation = "TRIM" },
            new FieldMappingInput { SourceField = "Supplier", TargetField = "PurchaseOrder.SupplierCode", Transformation = "UPPER" },
            new FieldMappingInput { SourceField = "SupplierName", TargetField = "PurchaseOrder.SupplierName" },
             new FieldMappingInput { SourceField = "Currency", TargetField = "PurchaseOrder.Currency", Transformation = "UPPER" },
             new FieldMappingInput { SourceField = "CompanyCode", TargetField = "PurchaseOrder.CompanyCode" },
             new FieldMappingInput { SourceField = "PurchaseOrderType", TargetField = "PurchaseOrder.PurchaseOrderType" },
             new FieldMappingInput { SourceField = "TotalNetAmount", TargetField = "PurchaseOrder.TotalNetAmount" },
             new FieldMappingInput { SourceField = "TotalTaxAmount", TargetField = "PurchaseOrder.TotalTaxAmount" },
             new FieldMappingInput { SourceField = "TotalAmount", TargetField = "PurchaseOrder.TotalAmount" },
             new FieldMappingInput { SourceField = "PoDate", TargetField = "PurchaseOrder.PoDate" },
             new FieldMappingInput { SourceField = "DeliveryDate", TargetField = "PurchaseOrder.DeliveryDate" },
            new FieldMappingInput { SourceField = "LastChangedAt", TargetField = "PurchaseOrder.SourceLastChangedAt" },
        ], CancellationToken.None);

         factory.IntegrationHandler.Enqueue(HttpStatusCode.OK, """{"value":[{"PurchaseOrder":"4500002001","Supplier":"SUP-1","SupplierName":"Test Supplier","Currency":"AED","CompanyCode":"1000","PurchaseOrderType":"NB","TotalNetAmount":"100.00","TotalTaxAmount":"5.00","TotalAmount":"105.00","PoDate":"2026-09-16","DeliveryDate":"2026-09-20","LastChangedAt":"2026-09-16T10:00:00Z"}]}""");
        var execution = await service.RunAsync(organization.Id, saved.Id, IntegrationExecutionTrigger.MANUAL, false, CancellationToken.None);
        Assert.NotNull(execution);
        Assert.Equal(IntegrationExecutionStatus.SUCCESS, execution!.Status);
        Assert.Equal(1, execution.RecordsCreated);
        Assert.Equal(1, await db.PurchaseOrders.CountAsync(item => item.OrganizationId == organization.Id && item.SourceConfigurationId == saved.Id));
        Assert.Equal(new DateTime(2026, 9, 16, 10, 0, 0, DateTimeKind.Utc), (await db.ApiIntegrationConfigurations.SingleAsync(item => item.Id == saved.Id)).LastWatermark);

        var concurrentConfig = await db.ApiIntegrationConfigurations.SingleAsync(item => item.Id == saved.Id);
        concurrentConfig.IsRunning = true;
        await db.SaveChangesAsync();
        Assert.Null(await service.RunAsync(organization.Id, saved.Id, IntegrationExecutionTrigger.SCHEDULED, false, CancellationToken.None));
    }

    [Fact]
    public async Task Spreadsheet_preview_reports_valid_missing_duplicate_and_malformed_rows_for_purchase_orders_and_suppliers()
    {
        await using var scope = factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<SilaMeDbContext>();
        var service = scope.ServiceProvider.GetRequiredService<IntegrationService>();

        foreach (var kind in Enum.GetValues<IntegrationImportKind>())
        {
            var setup = await CreateImportSetupAsync(db, service, kind);
            var columns = Columns(kind);
            var rows = kind == IntegrationImportKind.PURCHASE_ORDERS
                ? new[]
                {
                     new string?[] { "PO-100", "SUP-100", "Supplier 100", "AED", "1000", "NB", "100", "5", "105", "2026-09-16", "2026-09-20" },
                     new string?[] { "PO-101", null, "Supplier 101", "AED", "1000", "NB", "100", "5", "105", "2026-09-16", "2026-09-20" },
                     new string?[] { "PO-100", "SUP-100", "Duplicate Supplier", "AED", "1000", "NB", "100", "5", "105", "2026-09-17", "2026-09-20" },
                }
                : new[]
                {
                     new string?[] { "SUP-100", "Supplier 100", "100", "supplier100@test.local", "AED" },
                     new string?[] { "SUP-101", null, "101", "supplier101@test.local", "AED" },
                     new string?[] { "SUP-100", "Duplicate Supplier", "102", "duplicate@test.local", "AED" },
                };

            var preview = await service.PreviewImportAsync(
                setup.OrganizationId,
                setup.ConfigurationId,
                kind,
                new MemoryStream(CreateXlsx(columns, rows)),
                "import.xlsx",
                CancellationToken.None);

            Assert.Equal(3, preview.TotalRows);
            Assert.Equal(1, preview.ValidRows);
            Assert.Equal(2, preview.InvalidRows);
            Assert.True(preview.Rows[0].IsValid);
            Assert.Contains(preview.Rows[1].Errors, error => error.Contains("required", StringComparison.OrdinalIgnoreCase));
            Assert.Contains(preview.Rows[2].Errors, error => error.Contains("Duplicate key", StringComparison.OrdinalIgnoreCase));

            var malformed = await Assert.ThrowsAsync<IntegrationException>(() => service.PreviewImportAsync(
                setup.OrganizationId,
                setup.ConfigurationId,
                kind,
                new MemoryStream(Encoding.UTF8.GetBytes("not an xlsx file")),
                "malformed.xlsx",
                CancellationToken.None));
            Assert.Equal("IMPORT_FORMAT_INVALID", malformed.Code);
        }
    }

    [Fact]
    public async Task Invalid_spreadsheet_commits_do_not_change_existing_purchase_orders_or_suppliers()
    {
        await using var scope = factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<SilaMeDbContext>();
        var service = scope.ServiceProvider.GetRequiredService<IntegrationService>();

        foreach (var kind in Enum.GetValues<IntegrationImportKind>())
        {
            var setup = await CreateImportSetupAsync(db, service, kind);
            var existing = kind == IntegrationImportKind.PURCHASE_ORDERS
                 ? new Dictionary<string, string?> { ["PO_NUMBER"] = "PO-200", ["SUPPLIER_CODE"] = "SUP-200", ["SUPPLIER_NAME"] = "Original Supplier", ["CURRENCY"] = "AED", ["COMPANY_CODE"] = "1000", ["PURCHASE_ORDER_TYPE"] = "NB", ["TOTAL_NET_AMOUNT"] = "100", ["TOTAL_TAX_AMOUNT"] = "5", ["TOTAL_AMOUNT"] = "105", ["PO_DATE"] = "2026-09-16", ["DELIVERY_DATE"] = "2026-09-20" }
                 : new Dictionary<string, string?> { ["SUPPLIER_CODE"] = "SUP-200", ["NAME"] = "Original Supplier", ["TAX_NUMBER"] = "200", ["CURRENCY"] = "AED" };

            await service.CommitImportAsync(
                setup.OrganizationId,
                setup.ConfigurationId,
                new IntegrationImportCommitInput(kind, [existing]),
                CancellationToken.None);
            var executionCountBefore = await db.ApiIntegrationExecutions.CountAsync(item => item.ConfigurationId == setup.ConfigurationId);

            var update = kind == IntegrationImportKind.PURCHASE_ORDERS
                 ? new Dictionary<string, string?> { ["PO_NUMBER"] = "PO-200", ["SUPPLIER_CODE"] = "SUP-200", ["SUPPLIER_NAME"] = "Changed Supplier", ["CURRENCY"] = "USD", ["COMPANY_CODE"] = "1000", ["PURCHASE_ORDER_TYPE"] = "NB", ["TOTAL_NET_AMOUNT"] = "100", ["TOTAL_TAX_AMOUNT"] = "5", ["TOTAL_AMOUNT"] = "105", ["PO_DATE"] = "2026-09-16", ["DELIVERY_DATE"] = "2026-09-20", ["SOURCE_LAST_CHANGED_AT"] = "2026-09-18T10:00:00Z" }
                 : new Dictionary<string, string?> { ["SUPPLIER_CODE"] = "SUP-200", ["NAME"] = "Changed Supplier", ["TAX_NUMBER"] = "999", ["CURRENCY"] = "USD" };
            var invalid = kind == IntegrationImportKind.PURCHASE_ORDERS
                ? new Dictionary<string, string?> { ["PO_NUMBER"] = "PO-201", ["SUPPLIER_NAME"] = "Missing Code Supplier" }
                : new Dictionary<string, string?> { ["SUPPLIER_CODE"] = "SUP-201", ["TAX_NUMBER"] = "Missing Name Supplier" };

            var exception = await Assert.ThrowsAsync<IntegrationException>(() => service.CommitImportAsync(
                setup.OrganizationId,
                setup.ConfigurationId,
                new IntegrationImportCommitInput(kind, [update, invalid]),
                CancellationToken.None));
            Assert.Equal("IMPORT_VALIDATION_FAILED", exception.Code);

            Assert.Equal(executionCountBefore, await db.ApiIntegrationExecutions.CountAsync(item => item.ConfigurationId == setup.ConfigurationId));
            if (kind == IntegrationImportKind.PURCHASE_ORDERS)
            {
                var purchaseOrder = await db.PurchaseOrders.SingleAsync(item => item.OrganizationId == setup.OrganizationId && item.PoNumber == "PO-200");
                Assert.Equal("AED", purchaseOrder.Currency);
                Assert.Null(purchaseOrder.SourceLastChangedAt);
                Assert.Equal(1, await db.PurchaseOrders.CountAsync(item => item.OrganizationId == setup.OrganizationId));
            }
            else
            {
                var supplier = await db.Suppliers.SingleAsync(item => item.OrganizationId == setup.OrganizationId && item.SupplierCode == "SUP-200");
                Assert.Equal("Original Supplier", supplier.Name);
                Assert.Equal("200", supplier.TaxNumber);
                Assert.Equal(1, await db.Suppliers.CountAsync(item => item.OrganizationId == setup.OrganizationId));
            }
        }
    }

    [Fact]
    public async Task Successful_excel_import_upserts_all_rows_and_records_excel_trigger_for_purchase_orders_and_suppliers()
    {
        await using var scope = factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<SilaMeDbContext>();
        var service = scope.ServiceProvider.GetRequiredService<IntegrationService>();

        foreach (var kind in Enum.GetValues<IntegrationImportKind>())
        {
            var setup = await CreateImportSetupAsync(db, service, kind);
            var existing = kind == IntegrationImportKind.PURCHASE_ORDERS
                 ? new Dictionary<string, string?> { ["PO_NUMBER"] = "PO-300", ["SUPPLIER_CODE"] = "SUP-300", ["SUPPLIER_NAME"] = "Original PO Supplier", ["CURRENCY"] = "AED", ["COMPANY_CODE"] = "1000", ["PURCHASE_ORDER_TYPE"] = "NB", ["TOTAL_NET_AMOUNT"] = "100", ["TOTAL_TAX_AMOUNT"] = "5", ["TOTAL_AMOUNT"] = "105", ["PO_DATE"] = "2026-09-16", ["DELIVERY_DATE"] = "2026-09-20" }
                 : new Dictionary<string, string?> { ["SUPPLIER_CODE"] = "SUP-300", ["NAME"] = "Original Supplier", ["TAX_NUMBER"] = "300", ["CURRENCY"] = "AED" };
            await service.CommitImportAsync(
                setup.OrganizationId,
                setup.ConfigurationId,
                new IntegrationImportCommitInput(kind, [existing]),
                CancellationToken.None);

            var columns = Columns(kind);
            var rows = kind == IntegrationImportKind.PURCHASE_ORDERS
                ? new[]
                {
                     new string?[] { "PO-300", "SUP-300", "Updated PO Supplier", "USD", "1000", "NB", "100", "5", "105", "2026-09-19", "2026-09-21" },
                     new string?[] { "PO-301", "SUP-301", "New PO Supplier", "EUR", "1000", "NB", "200", "10", "210", "2026-09-20", "2026-09-22" },
                }
                : new[]
                {
                     new string?[] { "SUP-300", "Updated Supplier", "301", "updated300@test.local", "USD" },
                     new string?[] { "SUP-301", "New Supplier", "301", "new301@test.local", "EUR" },
                };

            var preview = await service.PreviewImportAsync(
                setup.OrganizationId,
                setup.ConfigurationId,
                kind,
                new MemoryStream(CreateXlsx(columns, rows)),
                "successful-import.xlsx",
                CancellationToken.None);
            var result = await service.CommitImportAsync(
                setup.OrganizationId,
                setup.ConfigurationId,
                new IntegrationImportCommitInput(
                    kind,
                    preview.Rows.Select(row => new Dictionary<string, string?>(row.Values, StringComparer.OrdinalIgnoreCase)).ToList()),
                CancellationToken.None);

            Assert.Equal(2, result.RecordsCommitted);
            Assert.Equal(IntegrationExecutionTrigger.EXCEL_IMPORT, result.Execution.Trigger);
            Assert.Equal(IntegrationExecutionStatus.SUCCESS, result.Execution.Status);
            Assert.Equal(1, result.Execution.RecordsCreated);
            Assert.Equal(1, result.Execution.RecordsUpdated);
            if (kind == IntegrationImportKind.PURCHASE_ORDERS)
            {
                Assert.Equal(2, await db.PurchaseOrders.CountAsync(item => item.OrganizationId == setup.OrganizationId));
                Assert.Equal(new DateOnly(2026, 9, 19), (await db.PurchaseOrders.SingleAsync(item => item.OrganizationId == setup.OrganizationId && item.PoNumber == "PO-300")).PoDate);
                Assert.Contains("PO-301", await db.PurchaseOrders.Where(item => item.OrganizationId == setup.OrganizationId).Select(item => item.PoNumber).ToListAsync());
            }
            else
            {
                Assert.Equal(2, await db.Suppliers.CountAsync(item => item.OrganizationId == setup.OrganizationId));
                Assert.Equal("Updated Supplier", (await db.Suppliers.SingleAsync(item => item.OrganizationId == setup.OrganizationId && item.SupplierCode == "SUP-300")).Name);
                Assert.Contains("SUP-301", await db.Suppliers.Where(item => item.OrganizationId == setup.OrganizationId).Select(item => item.SupplierCode).ToListAsync());
            }
        }
    }

    [Fact]
    public async Task Supplier_and_po_excel_templates_contain_required_sheets()
    {
        await using var scope = factory.Services.CreateAsyncScope();
        var service = scope.ServiceProvider.GetRequiredService<IntegrationService>();
        var supplier = await service.TemplateAsync(IntegrationImportKind.SUPPLIERS, CancellationToken.None);
        var purchaseOrders = await service.TemplateAsync(IntegrationImportKind.PURCHASE_ORDERS, CancellationToken.None);
        Assert.Contains("SUPPLIERS", SheetNames(supplier));
        Assert.Contains("INSTRUCTIONS", SheetNames(supplier));
        Assert.Contains("PO_HEADERS", SheetNames(purchaseOrders));
        Assert.Contains("PO_ITEMS", SheetNames(purchaseOrders));
        Assert.Contains("INSTRUCTIONS", SheetNames(purchaseOrders));
    }

    [Fact]
    public async Task Required_supplier_and_po_excel_upserts_items_without_duplicates()
    {
        await using var scope = factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<SilaMeDbContext>();
        var service = scope.ServiceProvider.GetRequiredService<IntegrationService>();
        var setup = await CreateImportSetupAsync(db, service, IntegrationImportKind.SUPPLIERS);

        var supplierPreview = await service.PreviewImportAsync(
            setup.OrganizationId, setup.ConfigurationId, IntegrationImportKind.SUPPLIERS,
            new MemoryStream(CreateXlsx(["SupplierId *", "SupplierName *"], ["1000234", "ABC FOOD TRADING LLC"])),
            "suppliers.xlsx", CancellationToken.None);
        Assert.Equal(1, supplierPreview.ValidRows);
        Assert.Equal("NEW", supplierPreview.Rows[0].Action);
        await service.CommitImportAsync(setup.OrganizationId, setup.ConfigurationId,
            new IntegrationImportCommitInput(IntegrationImportKind.SUPPLIERS, [new Dictionary<string, string?>(supplierPreview.Rows[0].Values)]),
            CancellationToken.None);
        Assert.Equal(1, await db.Suppliers.CountAsync(item => item.OrganizationId == setup.OrganizationId && item.SupplierCode == "1000234"));

        var secondSupplier = await service.CommitImportAsync(setup.OrganizationId, setup.ConfigurationId,
            new IntegrationImportCommitInput(IntegrationImportKind.SUPPLIERS, [new Dictionary<string, string?>(supplierPreview.Rows[0].Values)]),
            CancellationToken.None);
        Assert.Equal(0, secondSupplier.Execution.RecordsCreated);
        Assert.Equal(1, secondSupplier.Execution.RecordsUpdated);
        Assert.Equal(1, await db.Suppliers.CountAsync(item => item.OrganizationId == setup.OrganizationId && item.SupplierCode == "1000234"));

        var poSetup = await CreateImportSetupAsync(db, service, IntegrationImportKind.PURCHASE_ORDERS);
        db.Suppliers.Add(new Supplier
        {
            Id = Guid.NewGuid(), OrganizationId = poSetup.OrganizationId, EntityCode = "ALL", SupplierCode = "1000234",
            Name = "ABC FOOD TRADING LLC", NormalizedName = "ABC FOOD TRADING LLC", Currency = "AED", Status = StatusKind.ACTIVE,
            SourceSystem = "TEST", CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow,
        });
        await db.SaveChangesAsync();

        var workbook = CreatePoWorkbook(
            [["4500012345", "MATERIAL", "1000234", "FIVE_DXB", "AED", "997.50", "OPEN"]],
            [
                ["4500012345", "10", "SALMON001", "Salmon Fillet", "10", "KG", "AED", "500.00", "TAX5", "TRUE"],
                ["4500012345", "20", "", "Fresh Tomato", "20", "KG", "AED", "497.50", "TAX5", "TRUE"],
            ]);
        var preview = await service.PreviewImportAsync(poSetup.OrganizationId, poSetup.ConfigurationId, IntegrationImportKind.PURCHASE_ORDERS, new MemoryStream(workbook), "po.xlsx", CancellationToken.None);
        Assert.Equal(2, preview.ValidRows);
        Assert.DoesNotContain(preview.Rows, row => !row.IsValid);
        var committed = await service.CommitImportAsync(poSetup.OrganizationId, poSetup.ConfigurationId,
            new IntegrationImportCommitInput(IntegrationImportKind.PURCHASE_ORDERS, preview.Rows.Select(row => new Dictionary<string, string?>(row.Values)).ToList()),
            CancellationToken.None);
        Assert.Equal(1, committed.Execution.RecordsCreated);
        Assert.Equal(1, await db.PurchaseOrders.CountAsync(item => item.OrganizationId == poSetup.OrganizationId && item.PoNumber == "4500012345"));
        Assert.Equal(2, await db.PurchaseOrderItems.CountAsync(item => item.PurchaseOrder.OrganizationId == poSetup.OrganizationId && item.PurchaseOrder.PoNumber == "4500012345"));

        var duplicate = await service.CommitImportAsync(poSetup.OrganizationId, poSetup.ConfigurationId,
            new IntegrationImportCommitInput(IntegrationImportKind.PURCHASE_ORDERS, preview.Rows.Select(row => new Dictionary<string, string?>(row.Values)).ToList()),
            CancellationToken.None);
        Assert.Equal(0, duplicate.Execution.RecordsCreated);
        Assert.Equal(1, duplicate.Execution.RecordsUpdated);
        Assert.Equal(1, await db.PurchaseOrders.CountAsync(item => item.OrganizationId == poSetup.OrganizationId && item.PoNumber == "4500012345"));
        Assert.Equal(2, await db.PurchaseOrderItems.CountAsync(item => item.PurchaseOrder.OrganizationId == poSetup.OrganizationId && item.PurchaseOrder.PoNumber == "4500012345"));

        var updatedRows = preview.Rows.Select(row =>
        {
            var values = new Dictionary<string, string?>(row.Values);
            if (values["LINE_NUMBER"] == "10") values["ITEM_AMOUNT"] = "510";
            return values;
        }).ToList();
        var updatePreview = await service.PreviewImportAsync(poSetup.OrganizationId, poSetup.ConfigurationId, IntegrationImportKind.PURCHASE_ORDERS,
            new MemoryStream(CreatePoWorkbook(
                [["4500012345", "MATERIAL", "1000234", "FIVE_DXB", "AED", "997.50", "OPEN"]],
                [
                    ["4500012345", "10", "SALMON001", "Salmon Fillet", "10", "KG", "AED", "510", "TAX5", "TRUE"],
                    ["4500012345", "20", "", "Fresh Tomato", "20", "KG", "AED", "497.50", "TAX5", "TRUE"],
                ])), "po-update.xlsx", CancellationToken.None);
        Assert.Contains(updatePreview.Rows, row => row.Action == "UPDATE");
        await service.CommitImportAsync(poSetup.OrganizationId, poSetup.ConfigurationId, new IntegrationImportCommitInput(IntegrationImportKind.PURCHASE_ORDERS, updatedRows), CancellationToken.None);
        var item10 = await db.PurchaseOrderItems.SingleAsync(item => item.PurchaseOrder.OrganizationId == poSetup.OrganizationId && item.PurchaseOrder.PoNumber == "4500012345" && item.LineNumber == 10);
        Assert.Equal(510m, item10.ItemAmount);
        Assert.Equal(2, await db.PurchaseOrderItems.CountAsync(item => item.PurchaseOrder.OrganizationId == poSetup.OrganizationId && item.PurchaseOrder.PoNumber == "4500012345"));
        _ = updatePreview;
    }

    [Fact]
    public async Task Excel_po_header_source_last_changed_at_unique_name_is_persisted_for_ariba()
    {
        await using var scope = factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<SilaMeDbContext>();
        var service = scope.ServiceProvider.GetRequiredService<IntegrationService>();
        var poSetup = await CreateImportSetupAsync(db, service, IntegrationImportKind.PURCHASE_ORDERS);
        db.Suppliers.Add(new Supplier
        {
            Id = Guid.NewGuid(), OrganizationId = poSetup.OrganizationId, EntityCode = "ALL", SupplierCode = "1000234",
            Name = "ABC FOOD TRADING LLC", NormalizedName = "ABC FOOD TRADING LLC", Currency = "AED", Status = StatusKind.ACTIVE,
            SourceSystem = "TEST", CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow,
        });
        await db.SaveChangesAsync();

        var workbook = CreatePoWorkbook(
            [["4500002849", "MATERIAL", "1000234", "FIVE_DXB", "AED", "2000", "OPEN", "EP23721"]],
            [["4500002849", "10", "MAT001", "Test item", "1", "EA", "AED", "2000", "TAX5", "TRUE", ""]]);
        var preview = await service.PreviewImportAsync(poSetup.OrganizationId, poSetup.ConfigurationId, IntegrationImportKind.PURCHASE_ORDERS, new MemoryStream(workbook), "po-ariba.xlsx", CancellationToken.None);
        Assert.Equal(1, preview.ValidRows);
        Assert.Equal("EP23721", preview.Rows[0].Values["SOURCE_LAST_CHANGED_AT"]);
        await service.CommitImportAsync(poSetup.OrganizationId, poSetup.ConfigurationId,
            new IntegrationImportCommitInput(IntegrationImportKind.PURCHASE_ORDERS, preview.Rows.Select(row => new Dictionary<string, string?>(row.Values)).ToList()),
            CancellationToken.None);
        var po = await db.PurchaseOrders.SingleAsync(item => item.OrganizationId == poSetup.OrganizationId && item.PoNumber == "4500002849");
        Assert.Equal("EP23721", po.SourceLastChangedAtRaw);
        Assert.Null(po.SourceLastChangedAt);
    }

    [Fact]
    public async Task Po_item_without_material_or_description_and_unknown_supplier_are_rejected()
    {
        await using var scope = factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<SilaMeDbContext>();
        var service = scope.ServiceProvider.GetRequiredService<IntegrationService>();
        var setup = await CreateImportSetupAsync(db, service, IntegrationImportKind.PURCHASE_ORDERS);

        var missingIdentity = await service.PreviewImportAsync(setup.OrganizationId, setup.ConfigurationId, IntegrationImportKind.PURCHASE_ORDERS,
            new MemoryStream(CreatePoWorkbook(
                [["4500099999", "MATERIAL", "SUP-100", "FIVE_DXB", "AED", "10", "OPEN"]],
                [["4500099999", "10", "", "", "1", "KG", "AED", "10", "TAX5", "TRUE"]])),
            "missing-identity.xlsx", CancellationToken.None);
        Assert.Contains(missingIdentity.Rows, row => row.ErrorCodes!.Contains("PO_ITEM_IDENTIFICATION_REQUIRED"));
        await Assert.ThrowsAsync<IntegrationException>(() => service.CommitImportAsync(setup.OrganizationId, setup.ConfigurationId,
            new IntegrationImportCommitInput(IntegrationImportKind.PURCHASE_ORDERS, missingIdentity.Rows.Select(row => new Dictionary<string, string?>(row.Values)).ToList()),
            CancellationToken.None));
        Assert.Equal(0, await db.PurchaseOrders.CountAsync(item => item.OrganizationId == setup.OrganizationId && item.PoNumber == "4500099999"));

        var unknown = await service.PreviewImportAsync(setup.OrganizationId, setup.ConfigurationId, IntegrationImportKind.PURCHASE_ORDERS,
            new MemoryStream(CreatePoWorkbook(
                [["4500088888", "MATERIAL", "9999999", "FIVE_DXB", "AED", "10", "OPEN"]],
                [["4500088888", "10", "MAT1", "Desc", "1", "KG", "AED", "10", "TAX5", "TRUE"]])),
            "unknown-supplier.xlsx", CancellationToken.None);
        Assert.Contains(unknown.Rows, row => row.ErrorCodes!.Contains("SUPPLIER_NOT_FOUND"));
        await Assert.ThrowsAsync<IntegrationException>(() => service.CommitImportAsync(setup.OrganizationId, setup.ConfigurationId,
            new IntegrationImportCommitInput(IntegrationImportKind.PURCHASE_ORDERS, unknown.Rows.Select(row => new Dictionary<string, string?>(row.Values)).ToList()),
            CancellationToken.None));
        Assert.Equal(0, await db.PurchaseOrders.CountAsync(item => item.OrganizationId == setup.OrganizationId && item.PoNumber == "4500088888"));
    }

    private static async Task<(Guid OrganizationId, Guid ConfigurationId)> CreateImportSetupAsync(
        SilaMeDbContext db,
        IntegrationService service,
        IntegrationImportKind kind)
    {
        var organization = new Organization
        {
            Id = Guid.NewGuid(),
            Code = $"XLS-{Guid.NewGuid():N}"[..16].ToUpperInvariant(),
            Name = "Spreadsheet Import Test Organization",
            Kind = OrganizationKind.CUSTOMER,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
        };
        db.Organizations.Add(organization);
        await db.SaveChangesAsync();

         if (kind == IntegrationImportKind.PURCHASE_ORDERS)
         {
            db.Suppliers.AddRange(new[] { "SUP-1", "SUP-100", "SUP-200", "SUP-300", "SUP-301" }.Select(code => new Supplier
             {
                 Id = Guid.NewGuid(), OrganizationId = organization.Id, EntityCode = "ALL", SupplierCode = code,
                 Name = code, NormalizedName = code, Currency = "AED", Status = StatusKind.ACTIVE,
                 SourceSystem = "TEST", CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow,
             }));
             await db.SaveChangesAsync();
         }

        var configuration = await service.SaveAsync(
            organization.Id,
            null,
            new IntegrationConfigurationInput
            {
                Name = $"{kind} spreadsheet import",
                EntityCode = "ALL",
                ProcessType = kind == IntegrationImportKind.SUPPLIERS ? IntegrationProcessType.GET_SUPPLIER : IntegrationProcessType.GET_PO,
                BaseUrl = "https://spreadsheet.test.example",
                ResourcePath = "integration",
                AuthenticationType = IntegrationAuthenticationType.NONE,
            },
            CancellationToken.None);
        return (organization.Id, configuration.Id);
    }

    private static string[] Columns(IntegrationImportKind kind) =>
        kind == IntegrationImportKind.PURCHASE_ORDERS
             ? ["PO_NUMBER", "SUPPLIER_CODE", "SUPPLIER_NAME", "CURRENCY", "COMPANY_CODE", "PURCHASE_ORDER_TYPE", "TOTAL_NET_AMOUNT", "TOTAL_TAX_AMOUNT", "TOTAL_AMOUNT", "PO_DATE", "DELIVERY_DATE"]
             : ["SUPPLIER_CODE", "NAME", "TAX_NUMBER", "EMAIL", "CURRENCY"];

    private static byte[] CreatePoWorkbook(string?[][] headers, string?[][] items)
    {
        using var output = new MemoryStream();
        using (var archive = new ZipArchive(output, ZipArchiveMode.Create, true))
        {
            AddZipEntry(archive, "[Content_Types].xml", """<?xml version="1.0" encoding="UTF-8" standalone="yes"?><Types xmlns="http://schemas.openxmlformats.org/package/2006/content-types"><Default Extension="rels" ContentType="application/vnd.openxmlformats-package.relationships+xml"/><Default Extension="xml" ContentType="application/xml"/><Override PartName="/xl/workbook.xml" ContentType="application/vnd.openxmlformats-officedocument.spreadsheetml.sheet.main+xml"/><Override PartName="/xl/worksheets/sheet1.xml" ContentType="application/vnd.openxmlformats-officedocument.spreadsheetml.worksheet+xml"/><Override PartName="/xl/worksheets/sheet2.xml" ContentType="application/vnd.openxmlformats-officedocument.spreadsheetml.worksheet+xml"/></Types>""");
            AddZipEntry(archive, "_rels/.rels", """<?xml version="1.0" encoding="UTF-8" standalone="yes"?><Relationships xmlns="http://schemas.openxmlformats.org/package/2006/relationships"><Relationship Id="rId1" Type="http://schemas.openxmlformats.org/officeDocument/2006/relationships/officeDocument" Target="xl/workbook.xml"/></Relationships>""");
            AddZipEntry(archive, "xl/workbook.xml", """<?xml version="1.0" encoding="UTF-8" standalone="yes"?><workbook xmlns="http://schemas.openxmlformats.org/spreadsheetml/2006/main" xmlns:r="http://schemas.openxmlformats.org/officeDocument/2006/relationships"><sheets><sheet name="PO_HEADERS" sheetId="1" r:id="rId1"/><sheet name="PO_ITEMS" sheetId="2" r:id="rId2"/></sheets></workbook>""");
            AddZipEntry(archive, "xl/_rels/workbook.xml.rels", """<?xml version="1.0" encoding="UTF-8" standalone="yes"?><Relationships xmlns="http://schemas.openxmlformats.org/package/2006/relationships"><Relationship Id="rId1" Type="http://schemas.openxmlformats.org/officeDocument/2006/relationships/worksheet" Target="worksheets/sheet1.xml"/><Relationship Id="rId2" Type="http://schemas.openxmlformats.org/officeDocument/2006/relationships/worksheet" Target="worksheets/sheet2.xml"/></Relationships>""");
            AddSheet(archive, "xl/worksheets/sheet1.xml", ["PurchaseOrder *", "PurchaseOrderType *", "SupplierId *", "CompanyCode *", "Currency *", "TotalAmount *", "POStatus *", "SourceLastChangedAt"], headers);
            AddSheet(archive, "xl/worksheets/sheet2.xml", ["PurchaseOrder *", "PurchaseOrderItem *", "Material **", "MaterialDescription **", "OrderQuantity *", "UOM *", "Currency *", "ItemAmount *", "TaxCode *", "GoodsReceiptExpected *", "SourceLastChangedAt"], items);
        }
        return output.ToArray();
    }

    private static void AddSheet(ZipArchive archive, string path, IReadOnlyList<string> columns, string?[][] rows)
    {
        var sheet = new StringBuilder("""<?xml version="1.0" encoding="UTF-8" standalone="yes"?><worksheet xmlns="http://schemas.openxmlformats.org/spreadsheetml/2006/main"><sheetData>""");
        AppendXlsxRow(sheet, columns.Select(column => (string?)column).ToArray(), 1);
        for (var index = 0; index < rows.Length; index++) AppendXlsxRow(sheet, rows[index], index + 2);
        sheet.Append("</sheetData></worksheet>");
        AddZipEntry(archive, path, sheet.ToString());
    }

    private static List<string> SheetNames(byte[] bytes)
    {
        using var archive = new ZipArchive(new MemoryStream(bytes), ZipArchiveMode.Read);
        using var stream = archive.GetEntry("xl/workbook.xml")!.Open();
        return System.Xml.Linq.XDocument.Load(stream).Descendants().Where(item => item.Name.LocalName == "sheet")
            .Select(item => item.Attribute("name")?.Value ?? string.Empty).ToList();
    }

    private static byte[] CreateXlsx(IReadOnlyList<string> columns, params string?[][] rows)
    {
        using var output = new MemoryStream();
        using (var archive = new ZipArchive(output, ZipArchiveMode.Create, true))
        {
            AddZipEntry(archive, "[Content_Types].xml", """<?xml version="1.0" encoding="UTF-8" standalone="yes"?><Types xmlns="http://schemas.openxmlformats.org/package/2006/content-types"><Default Extension="rels" ContentType="application/vnd.openxmlformats-package.relationships+xml"/><Default Extension="xml" ContentType="application/xml"/><Override PartName="/xl/workbook.xml" ContentType="application/vnd.openxmlformats-officedocument.spreadsheetml.sheet.main+xml"/><Override PartName="/xl/worksheets/sheet1.xml" ContentType="application/vnd.openxmlformats-officedocument.spreadsheetml.worksheet+xml"/></Types>""");
            AddZipEntry(archive, "_rels/.rels", """<?xml version="1.0" encoding="UTF-8" standalone="yes"?><Relationships xmlns="http://schemas.openxmlformats.org/package/2006/relationships"><Relationship Id="rId1" Type="http://schemas.openxmlformats.org/officeDocument/2006/relationships/officeDocument" Target="xl/workbook.xml"/></Relationships>""");
            AddZipEntry(archive, "xl/workbook.xml", """<?xml version="1.0" encoding="UTF-8" standalone="yes"?><workbook xmlns="http://schemas.openxmlformats.org/spreadsheetml/2006/main" xmlns:r="http://schemas.openxmlformats.org/officeDocument/2006/relationships"><sheets><sheet name="Data" sheetId="1" r:id="rId1"/></sheets></workbook>""");
            AddZipEntry(archive, "xl/_rels/workbook.xml.rels", """<?xml version="1.0" encoding="UTF-8" standalone="yes"?><Relationships xmlns="http://schemas.openxmlformats.org/package/2006/relationships"><Relationship Id="rId1" Type="http://schemas.openxmlformats.org/officeDocument/2006/relationships/worksheet" Target="worksheets/sheet1.xml"/></Relationships>""");

            var sheet = new StringBuilder("""<?xml version="1.0" encoding="UTF-8" standalone="yes"?><worksheet xmlns="http://schemas.openxmlformats.org/spreadsheetml/2006/main"><sheetData>""");
            AppendXlsxRow(sheet, columns.Select(column => (string?)column).ToArray(), 1);
            for (var index = 0; index < rows.Length; index++) AppendXlsxRow(sheet, rows[index], index + 2);
            sheet.Append("</sheetData></worksheet>");
            AddZipEntry(archive, "xl/worksheets/sheet1.xml", sheet.ToString());
        }
        return output.ToArray();
    }

    private static void AppendXlsxRow(StringBuilder sheet, IReadOnlyList<string?> values, int rowNumber)
    {
        sheet.Append($"<row r=\"{rowNumber}\">");
        for (var index = 0; index < values.Count; index++)
        {
            var escaped = System.Security.SecurityElement.Escape(values[index] ?? string.Empty) ?? string.Empty;
            sheet.Append($"<c r=\"{ColumnName(index + 1)}{rowNumber}\" t=\"inlineStr\"><is><t>{escaped}</t></is></c>");
        }
        sheet.Append("</row>");
    }

    private static string ColumnName(int number)
    {
        var result = string.Empty;
        while (number > 0)
        {
            var remainder = (number - 1) % 26;
            result = (char)('A' + remainder) + result;
            number = (number - 1) / 26;
        }
        return result;
    }

    private static void AddZipEntry(ZipArchive archive, string name, string content)
    {
        using var writer = new StreamWriter(archive.CreateEntry(name, CompressionLevel.Fastest).Open(), new UTF8Encoding(false));
        writer.Write(content);
    }
}