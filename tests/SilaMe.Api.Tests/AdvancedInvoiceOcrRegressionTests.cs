using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using SilaMe.Api.Data;
using SilaMe.Api.DTOs;
using SilaMe.Api.Tests.Fixtures;
using SilaMe.Api.Models;
using SilaMe.Api.Services;
using Xunit;

namespace SilaMe.Api.Tests;

public sealed class AdvancedInvoiceOcrRegressionTests(ApiFactory factory) : IClassFixture<ApiFactory>
{
    [Fact]
    public async Task Digital_invoice_fixture_extracts_headers_lines_and_reconciles_amounts()
    {
        await using var scope = factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<SilaMeDbContext>();
        var service = scope.ServiceProvider.GetRequiredService<AdvancedInvoiceOcrService>();
        var document = Document("digital.pdf", "application/pdf", pageCount: 1);

        var run = await service.ExtractAsync(
            document,
            new MemoryStream(AdvancedInvoiceOcrFixtures.DigitalInvoice),
            ExtractionTrigger.INITIAL_BACKEND,
            CancellationToken.None);

        Assert.Equal("SUCCESS", run.Response.Status);
        Assert.False(run.Response.RequiresReview);
        Assert.Equal("Gulf Kitchen Foods LLC", run.Response.Header.SupplierName.Value);
        Assert.Equal("DIGITAL-1001", run.Response.Header.InvoiceNumber.Value);
        Assert.Equal(997.50m, run.Response.Header.GrossAmount.Value);
        Assert.Equal(2, run.Response.Lines.Count);
        Assert.True(run.Response.Validation.AmountsReconciled);
        Assert.True(run.Response.Validation.LineTotalMatchesNet);
        Assert.Equal(0m, run.Response.Validation.Difference);
        Assert.Equal(64, run.ContentHash.Length);
        Assert.Equal(
            Convert.ToHexString(SHA256.HashData(AdvancedInvoiceOcrFixtures.DigitalInvoice)),
            run.ContentHash);
        Assert.NotNull(run.Response.EffectiveConfiguration);
        Assert.NotEmpty(run.ConfigurationSnapshotJson);
        _ = db;
    }

    [Fact]
    public async Task Scanned_invoice_fixture_uses_builtin_ocr_when_embedded_text_is_unavailable()
    {
        await using var scope = factory.Services.CreateAsyncScope();
        var service = scope.ServiceProvider.GetRequiredService<AdvancedInvoiceOcrService>();
        var document = Document("scanned.png", "image/png", pageCount: 1);

        var run = await service.ExtractAsync(
            document,
            new MemoryStream(AdvancedInvoiceOcrFixtures.ScannedInvoice),
            ExtractionTrigger.AUTO_FALLBACK,
            CancellationToken.None);

        Assert.Equal("SUCCESS", run.Response.Status);
        Assert.Equal("SCANNED-2001", run.Response.Header.InvoiceNumber.Value);
        Assert.Equal("Desert Harvest General Trading", run.Response.Header.SupplierName.Value);
        Assert.Contains("SCANNED-2001", run.RawText);
        Assert.Single(run.Response.Lines);
        Assert.Equal(ExtractionTrigger.AUTO_FALLBACK.ToString(), run.Response.Trigger);
    }

    [Fact]
    public async Task Multi_page_fixture_preserves_page_count_and_all_line_items()
    {
        await using var scope = factory.Services.CreateAsyncScope();
        var service = scope.ServiceProvider.GetRequiredService<AdvancedInvoiceOcrService>();
        var document = Document("multi-page.pdf", "application/pdf", pageCount: 3);

        var run = await service.ExtractAsync(
            document,
            new MemoryStream(AdvancedInvoiceOcrFixtures.MultiPageLineItemInvoice),
            ExtractionTrigger.INITIAL_BACKEND,
            CancellationToken.None);

        Assert.Equal("SUCCESS", run.Response.Status);
        Assert.Equal(3, run.Response.PageCount);
        Assert.Equal(new[] { 10, 20, 30 }, run.Response.Lines.Select(line => line.LineNumber));
        Assert.Equal(new[] { "ITEM-001", "ITEM-002", "ITEM-003" }, run.Response.Lines.Select(line => line.SupplierItemCode.Value));
        Assert.Equal(1250m, run.Response.Header.NetAmount.Value);
    }

    [Fact]
    public async Task Reconciliation_failure_fixture_requires_review_and_exposes_difference()
    {
        await using var scope = factory.Services.CreateAsyncScope();
        var service = scope.ServiceProvider.GetRequiredService<AdvancedInvoiceOcrService>();
        var document = Document("reconciliation-failure.pdf", "application/pdf", pageCount: 1);

        var run = await service.ExtractAsync(
            document,
            new MemoryStream(AdvancedInvoiceOcrFixtures.ReconciliationFailureInvoice),
            ExtractionTrigger.INITIAL_BACKEND,
            CancellationToken.None);

        Assert.Equal("REVIEW_REQUIRED", run.Response.Status);
        Assert.True(run.Response.RequiresReview);
        Assert.False(run.Response.Validation.AmountsReconciled);
        Assert.Equal(15m, run.Response.Validation.Difference);
    }

    [Fact]
    public async Task Missing_required_field_fixture_requires_review_when_policy_requires_date_and_currency()
    {
        await using var scope = factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<SilaMeDbContext>();
        var service = scope.ServiceProvider.GetRequiredService<AdvancedInvoiceOcrService>();
        var organizationId = Guid.NewGuid();
        await SaveConfigurationAsync(db, organizationId, new InvoiceOcrConfiguration
        {
            RequireInvoiceDate = true,
            RequireCurrency = true,
            FinancialReconciliationEnabled = false,
        });
        var document = Document("missing-required.pdf", "application/pdf", pageCount: 1, organizationId);

        var run = await service.ExtractAsync(
            document,
            new MemoryStream(AdvancedInvoiceOcrFixtures.MissingRequiredFieldInvoice),
            ExtractionTrigger.INITIAL_BACKEND,
            CancellationToken.None);

        Assert.Equal("REVIEW_REQUIRED", run.Response.Status);
        Assert.True(run.Response.RequiresReview);
        Assert.Null(run.Response.Header.InvoiceDate.Value);
        Assert.Null(run.Response.Header.Currency.Value);
    }

    [Fact]
    public async Task Fallback_policy_requires_backend_for_missing_or_low_confidence_mobile_results()
    {
        await using var scope = factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<SilaMeDbContext>();
        var service = scope.ServiceProvider.GetRequiredService<AdvancedInvoiceOcrService>();
        var organizationId = Guid.NewGuid();
        await SaveConfigurationAsync(db, organizationId, new InvoiceOcrConfiguration
        {
            MinimumMobileConfidence = 0.80m,
            RequirePurchaseOrderNumber = true,
        });

        Assert.True(await service.IsMobileFallbackRequiredAsync(
            organizationId, "Supplier", "INV-1", null, 100m, 0.95m, CancellationToken.None));
        Assert.True(await service.IsMobileFallbackRequiredAsync(
            organizationId, "Supplier", "INV-1", "PO-1", 100m, 0.79m, CancellationToken.None));
        Assert.False(await service.IsMobileFallbackRequiredAsync(
            organizationId, "Supplier", "INV-1", "PO-1", 100m, 0.95m, CancellationToken.None));

        var disabledOrganizationId = Guid.NewGuid();
        await SaveConfigurationAsync(db, disabledOrganizationId, new InvoiceOcrConfiguration
        {
            AutomaticBackendFallbackEnabled = false,
        });
        Assert.False(await service.IsMobileFallbackRequiredAsync(
            disabledOrganizationId, null, null, null, null, 0m, CancellationToken.None));
    }

    [Fact]
    public async Task Re_reads_append_immutable_history_with_hashes_and_preserve_manual_values()
    {
        await using var scope = factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<SilaMeDbContext>();
        var storage = scope.ServiceProvider.GetRequiredService<IDocumentStorageService>();
        var fixture = await CreatePersistenceFixtureAsync(db, storage);

        await using (var firstScope = factory.Services.CreateAsyncScope())
        {
            var operations = firstScope.ServiceProvider.GetRequiredService<OperationalService>();
            await operations.RunAdvancedInvoiceExtractionAsync(
                fixture.Session,
                fixture.Document.Id,
                ExtractionTrigger.INITIAL_BACKEND,
                CancellationToken.None);
        }

        await using var firstReadScope = factory.Services.CreateAsyncScope();
        var firstReadDb = firstReadScope.ServiceProvider.GetRequiredService<SilaMeDbContext>();
        var first = await firstReadDb.DocumentExtractions.AsNoTracking()
            .Where(item => item.DocumentId == fixture.Document.Id)
            .Select(item => new
            {
                item.Id,
                item.ContentHash,
                item.ConfigurationSnapshotJson,
                item.StructuredPayloadJson,
                item.CreatedAt,
            })
            .SingleAsync();
        var firstPayload = first.StructuredPayloadJson;

        await using (var rereadScope = factory.Services.CreateAsyncScope())
        {
            var operations = rereadScope.ServiceProvider.GetRequiredService<OperationalService>();
            await operations.RunAdvancedInvoiceExtractionAsync(
                fixture.Session,
                fixture.Document.Id,
                ExtractionTrigger.MANUAL_REREAD,
                CancellationToken.None);
        }

        await using var finalReadScope = factory.Services.CreateAsyncScope();
        var finalReadDb = finalReadScope.ServiceProvider.GetRequiredService<SilaMeDbContext>();
        var rows = await finalReadDb.DocumentExtractions.AsNoTracking()
            .Where(item => item.DocumentId == fixture.Document.Id)
            .Select(item => new
            {
                item.Id,
                item.ContentHash,
                item.ConfigurationSnapshotJson,
                item.StructuredPayloadJson,
                item.CreatedAt,
            })
            .OrderBy(item => item.CreatedAt)
            .ToListAsync();
        var invoice = await finalReadDb.Invoices.AsNoTracking().SingleAsync(item => item.Id == fixture.Invoice.Id);

        Assert.Equal(2, rows.Count);
        Assert.NotEqual(rows[0].Id, rows[1].Id);
        Assert.All(rows, row =>
        {
            Assert.Equal(first.ContentHash, row.ContentHash);
            Assert.Equal(Convert.ToHexString(SHA256.HashData(AdvancedInvoiceOcrFixtures.DigitalInvoice)), row.ContentHash);
            Assert.False(string.IsNullOrWhiteSpace(row.ConfigurationSnapshotJson));
            Assert.False(string.IsNullOrWhiteSpace(row.StructuredPayloadJson));
        });
        Assert.Equal(firstPayload, rows[0].StructuredPayloadJson);
        Assert.Equal("MANUAL-INV-9001", invoice.InvoiceNumber);
        Assert.Equal(777m, invoice.GrossAmount);
        Assert.Equal("Gulf Kitchen Foods LLC", invoice.SupplierNameRaw);
        Assert.Single(await finalReadDb.Invoices.Where(item => item.OrganizationId == fixture.OrganizationId).ToListAsync());
        Assert.Equal(2, await finalReadDb.InvoiceLines.CountAsync(item => item.InvoiceId == fixture.Invoice.Id));
        var extRows = await finalReadDb.InvoiceExtData
            .Where(item => item.DocumentId == fixture.Document.Id)
            .ToListAsync();
        Assert.Equal(2, extRows.Count);
        Assert.Equal(2, extRows.Select(item => item.LineItemNumber).Distinct().Count());
    }

    private static Document Document(
        string filename,
        string contentType,
        int pageCount,
        Guid? organizationId = null) =>
        new()
        {
            Id = Guid.NewGuid(),
            OrganizationId = organizationId ?? Guid.NewGuid(),
            UploadedByUserId = Guid.NewGuid(),
            DocumentType = DocumentType.INVOICE,
            OriginalFilename = filename,
            ContentType = contentType,
            PageCount = pageCount,
            StorageProvider = "TEST",
            StorageReference = "TEST",
            SourceChannel = DocumentSourceChannel.MOBILE_SCANNER,
            Status = DocumentStatus.READING,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
        };

    private static async Task SaveConfigurationAsync(
        SilaMeDbContext db,
        Guid organizationId,
        InvoiceOcrConfiguration configuration)
    {
        var user = new User
        {
            Id = Guid.NewGuid(),
            Email = $"advanced-ocr-config-{Guid.NewGuid():N}@silame.local",
            NormalizedEmail = string.Empty,
            DisplayName = "Advanced OCR Configuration Test",
            PasswordHash = "not-used",
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
        };
        user.NormalizedEmail = user.Email.ToUpperInvariant();
        var organization = new Organization
        {
            Id = organizationId,
            Code = $"OCR-{Guid.NewGuid():N}"[..16].ToUpperInvariant(),
            Name = "Advanced OCR Test Organization",
            Kind = OrganizationKind.CUSTOMER,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
        };
        configuration.Id = Guid.NewGuid();
        configuration.OrganizationId = organizationId;
        configuration.CreatedByUserId = user.Id;
        configuration.UpdatedByUserId = user.Id;
        configuration.CreatedAt = DateTime.UtcNow;
        configuration.UpdatedAt = DateTime.UtcNow;
        db.AddRange(user, organization, configuration);
        await db.SaveChangesAsync();
    }

    private static async Task<PersistenceFixture> CreatePersistenceFixtureAsync(
        SilaMeDbContext db,
        IDocumentStorageService storage)
    {
        var now = DateTime.UtcNow;
        var organization = new Organization
        {
            Id = Guid.NewGuid(),
            Code = $"OCR-{Guid.NewGuid():N}"[..16].ToUpperInvariant(),
            Name = "Advanced OCR Persistence Organization",
            Kind = OrganizationKind.CUSTOMER,
            CreatedAt = now,
            UpdatedAt = now,
        };
        var user = new User
        {
            Id = Guid.NewGuid(),
            Email = $"advanced-ocr-run-{Guid.NewGuid():N}@silame.local",
            NormalizedEmail = string.Empty,
            DisplayName = "Advanced OCR Persistence Test",
            PasswordHash = "not-used",
            CreatedAt = now,
            UpdatedAt = now,
        };
        user.NormalizedEmail = user.Email.ToUpperInvariant();
        var role = new Role
        {
            Id = Guid.NewGuid(),
            Key = $"OCR_TEST_ROLE_{Guid.NewGuid():N}"[..24].ToUpperInvariant(),
            Name = "Advanced OCR test role",
            ApplicationScope = "CLOUD",
            CreatedAt = now,
            UpdatedAt = now,
        };
        var editExtraction = await db.Permissions.SingleAsync(item => item.Key == PermissionKeys.EditExtraction);
        role.Permissions.Add(new RolePermission { RoleId = role.Id, PermissionId = editExtraction.Id });
        db.AddRange(
            organization,
            user,
            role,
            new UserApplicationAccess
            {
                Id = Guid.NewGuid(),
                UserId = user.Id,
                Application = ApplicationKind.CLOUD,
                CreatedAt = now,
            },
            new UserOrganizationMembership
            {
                Id = Guid.NewGuid(),
                UserId = user.Id,
                OrganizationId = organization.Id,
                CreatedAt = now,
            },
            new UserRoleAssignment
            {
                Id = Guid.NewGuid(),
                UserId = user.Id,
                RoleId = role.Id,
                OrganizationId = organization.Id,
                CreatedAt = now,
            });
        await db.SaveChangesAsync();

        var stored = await storage.StoreAsync(
            new MemoryStream(AdvancedInvoiceOcrFixtures.DigitalInvoice),
            "digital.pdf",
            CancellationToken.None);
        var document = Document("digital.pdf", "application/pdf", 1, organization.Id);
        document.UploadedByUserId = user.Id;
        document.FileSizeBytes = stored.Size;
        document.StorageProvider = stored.Provider;
        document.StorageReference = stored.Reference;
        document.OcrRequestId = $"ocr-{Guid.NewGuid():N}";
        var invoice = new Invoice
        {
            Id = Guid.NewGuid(),
            OrganizationId = organization.Id,
            DocumentId = document.Id,
            InvoiceNumber = "MANUAL-INV-9001",
            GrossAmount = 777m,
            ManualEditedFieldsJson = JsonSerializer.Serialize(new[] { "InvoiceNumber", "GrossAmount" }),
            InvoiceType = InvoiceType.UNKNOWN,
            Status = InvoiceStatus.PROCESSING,
            CreatedByUserId = user.Id,
            CreatedAt = now,
            UpdatedAt = now,
        };
        db.AddRange(document, invoice);
        await db.SaveChangesAsync();
        return new PersistenceFixture(
            organization.Id,
            document,
            invoice,
            new Session
            {
                Id = Guid.NewGuid(),
                UserId = user.Id,
                Application = ApplicationKind.CLOUD,
                TokenHash = $"ocr-test-{Guid.NewGuid():N}",
                CreatedAt = now,
                ExpiresAt = now.AddHours(1),
                LastUsedAt = now,
            });
    }

    private sealed record PersistenceFixture(
        Guid OrganizationId,
        Document Document,
        Invoice Invoice,
        Session Session);
}