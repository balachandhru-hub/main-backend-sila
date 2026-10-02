using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using Microsoft.EntityFrameworkCore;
using SilaMe.Api.Auth;
using SilaMe.Api.Data;
using SilaMe.Api.DTOs;
using SilaMe.Api.Models;
using SilaMe.Api.Tenancy;

namespace SilaMe.Api.Services;

public sealed record InvoiceLineDraft(
    int LineNumber,
    string? Description,
    decimal? Quantity,
    string? Uom,
    decimal? UnitPrice,
    decimal? LineAmount,
    string? MaterialCode,
    string? PoItemNumber);

public sealed record StoredDocument(string Provider, string Reference, long Size);
public sealed record ExtractedInvoice(
    string? InvoiceNumber,
    DateOnly? InvoiceDate,
    string? SupplierName,
    string? SupplierTaxNumber,
    string? PoNumber,
    string? Currency,
    decimal? NetAmount,
    decimal? TaxAmount,
    decimal? GrossAmount,
    IReadOnlyList<ExtractedInvoiceLine> Lines);
public sealed record ExtractedInvoiceLine(
    int LineNumber,
    string? SupplierMaterialCode,
    string Description,
    decimal? Quantity,
    string? Uom,
    decimal? UnitPrice,
    decimal? TaxRate,
    decimal? TaxAmount,
    decimal? LineAmount);

public interface IDocumentStorageService
{
    Task<StoredDocument> StoreAsync(Stream content, string originalFilename, CancellationToken cancellationToken);
    Task<Stream> GetAsync(string reference, CancellationToken cancellationToken);
}

public interface IPdfTextExtractor
{
    Task<string?> ExtractAsync(Stream content, CancellationToken cancellationToken);
}

public interface IOcrProvider
{
    string Name { get; }
    Task<(string? Text, decimal? Confidence)> ExtractAsync(Document document, Stream content, CancellationToken cancellationToken);
}

public interface IInvoiceExtractionService
{
    ExtractedInvoice Extract(string text);
}

public interface IGrnPostingProvider
{
    string Name { get; }
}

public sealed class LocalDocumentStorageService(IHostEnvironment environment, ITenantContextAccessor tenants) : IDocumentStorageService
{
    private readonly string root = Path.Combine(environment.ContentRootPath, "storage", "documents");

    public async Task<StoredDocument> StoreAsync(Stream content, string originalFilename, CancellationToken cancellationToken)
    {
        var folder = Folder();
        Directory.CreateDirectory(folder);
        var reference = Relative($"{Guid.NewGuid():N}{Path.GetExtension(originalFilename).ToLowerInvariant()}");
        var path = Path.Combine(root, reference.Replace('/', Path.DirectorySeparatorChar));
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        await using var output = File.Create(path);
        await content.CopyToAsync(output, cancellationToken);
        return new StoredDocument("LOCAL_DEVELOPMENT", reference, output.Length);
    }

    public Task<Stream> GetAsync(string reference, CancellationToken cancellationToken)
    {
        var namespaced = Path.Combine(root, reference.Replace('/', Path.DirectorySeparatorChar));
        var legacy = Path.Combine(root, Path.GetFileName(reference));
        var path = File.Exists(namespaced) ? namespaced : legacy;
        if (!File.Exists(path))
        {
            throw new FileNotFoundException("The stored document could not be found.", path);
        }

        return Task.FromResult<Stream>(File.OpenRead(path));
    }

    private string Folder()
    {
        var context = tenants.Current;
        return context is null
            ? root
            : Path.Combine(root, "tenants", context.TenantId.ToString("N"), context.EnvironmentId.ToString("N"));
    }

    private string Relative(string fileName)
    {
        var context = tenants.Current;
        return context is null
            ? fileName
            : $"tenants/{context.TenantId:N}/{context.EnvironmentId:N}/{fileName}";
    }
}

public sealed class EmbeddedPdfTextExtractor : IPdfTextExtractor
{
    public async Task<string?> ExtractAsync(Stream content, CancellationToken cancellationToken)
    {
        using var memory = new MemoryStream();
        await content.CopyToAsync(memory, cancellationToken);
        var value = Encoding.Latin1.GetString(memory.ToArray());
        var text = Regex.Replace(value, @"[^\u0009\u000A\u000D\u0020-\u007E]", " ");
        text = Regex.Replace(text, @"[ \t]+", " ");
        text = Regex.Replace(text, @"\r?\n[ \t]*", "\n").Trim();
        return text.Length < 20 || !text.Contains("Invoice", StringComparison.OrdinalIgnoreCase) ? null : text;
    }
}

public sealed class DevelopmentOcrProvider : IOcrProvider
{
    public string Name => "DEVELOPMENT_MOCK_OCR";

    public async Task<(string? Text, decimal? Confidence)> ExtractAsync(
        Document document,
        Stream content,
        CancellationToken cancellationToken)
    {
        using var memory = new MemoryStream();
        await content.CopyToAsync(memory, cancellationToken);
        var filename = document.OriginalFilename.ToLowerInvariant();
        if (filename.Contains("demo") || filename.Contains("invoice"))
        {
            return ("""
                Invoice Number: INV-1001
                Invoice Date: 2026-09-14
                Supplier: Demo Hospitality Supplier
                Tax Number: DEMO-TAX-001
                PO Number: 4500001001
                Currency: AED
                Net Amount: 5150.00
                Tax Amount: 257.50
                Gross Amount: 5407.50
                LINE 10 | Chicken Breast | 50 KG | 22.50
                LINE 20 | Basmati Rice | 80 KG | 12.50
                LINE 30 | Cooking Oil | 20 L | 8.00
                """, 0.86m);
        }

        return (null, null);
    }
}

public sealed class InvoiceExtractionService : IInvoiceExtractionService
{
    public ExtractedInvoice Extract(string text)
    {
        text = InvoiceOcrFieldReader.NormalizeOcrText(text);
        string? LabelValue(params string[] labels)
        {
            var pattern = string.Join("|", labels.OrderByDescending(label => label.Length).Select(Regex.Escape));
            var match = Regex.Match(text, $@"(?:{pattern})\s*(?:[:#\-]|\s)\s*(?<value>[^\r\n|]+)", RegexOptions.IgnoreCase);
            return match.Success ? match.Groups["value"].Value.Trim() : null;
        }
        decimal? Number(string pattern) =>
            decimal.TryParse(
                (Regex.Match(text, pattern, RegexOptions.IgnoreCase).Groups["value"].Value ?? string.Empty).Replace(",", string.Empty).Trim(),
                NumberStyles.Any, CultureInfo.InvariantCulture, out var value) ? value : null;
        DateOnly? Date(string pattern)
        {
            var raw = Regex.Match(text, pattern, RegexOptions.IgnoreCase).Groups["value"].Value.Trim();
            return InvoiceOcrFieldReader.ParseDate(raw);
        }

        var lines = new List<ExtractedInvoiceLine>();
        foreach (Match match in Regex.Matches(text, @"LINE\s+(?<line>\d+)\s*\|\s*(?:(?<sku>[^|]+)\s*\|\s*)?(?<description>[^|]+)\s*\|\s*(?:(?<qty>[\d.,]+)\s*(?<uom>[A-Za-z]+)\s*\|\s*)?(?<amount>[\d.,]+)(?:\s*\|\s*(?<net>[\d.,]+))?", RegexOptions.IgnoreCase))
        {
            var hasQuantity = decimal.TryParse(match.Groups["qty"].Value.Replace(",", ""), NumberStyles.Any, CultureInfo.InvariantCulture, out var quantity);
            var amount = decimal.TryParse(match.Groups["amount"].Value.Replace(",", ""), NumberStyles.Any, CultureInfo.InvariantCulture, out var parsedAmount) ? parsedAmount : (decimal?)null;
            var net = decimal.TryParse(match.Groups["net"].Value.Replace(",", ""), NumberStyles.Any, CultureInfo.InvariantCulture, out var parsedNet) ? parsedNet : amount;
            lines.Add(new ExtractedInvoiceLine(
                int.Parse(match.Groups["line"].Value, CultureInfo.InvariantCulture),
                match.Groups["sku"].Success ? match.Groups["sku"].Value.Trim() : null,
                match.Groups["description"].Value.Trim(),
                hasQuantity ? quantity : null,
                match.Groups["uom"].Success ? match.Groups["uom"].Value.Trim().ToUpperInvariant() : null,
                hasQuantity && amount.HasValue && quantity != 0 ? amount / quantity : null,
                null,
                null,
                amount ?? net));
        }

        if (lines.Count == 0)
        {
            var tableHeader = Regex.Match(text, @"(?im)^(?=.*\b(item|sku|code|description|material)\b)(?=.*\b(qty|quantity|amount|unit\s+price)\b).+$");
            if (tableHeader.Success)
            {
                var lineNumber = 1;
                foreach (var raw in text[(tableHeader.Index + tableHeader.Length)..].Split('\n'))
                {
                    var row = raw.Trim();
                    if (row.Length == 0) continue;
                    if (Regex.IsMatch(row, @"^(subtotal|vat|tax|grand\s+total|total|payment\s+terms|due\s+date|reference)\b", RegexOptions.IgnoreCase)) break;
                    if (Regex.IsMatch(row, @"^(description|po\s+item|quantity|unit\s+price|amount)\b", RegexOptions.IgnoreCase)) continue;
                    var parsed = ParseTableRow(row, lineNumber);
                    if (parsed is null) continue;
                    lines.Add(parsed);
                    lineNumber = parsed.LineNumber >= 10 ? parsed.LineNumber + 10 : parsed.LineNumber + 1;
                }
            }
        }

        return new ExtractedInvoice(
            InvoiceOcrFieldReader.ReadInvoiceNumber(text) ?? LabelValue("Supplier Invoice Number", "Invoice Number", "Invoice No.", "Invoice No", "Invoice #", "Invoice#", "Tax Invoice No", "Tax Invoice Number", "Inv No", "Inv No.", "Inv #", "Document Number", "Document No"),
            InvoiceOcrFieldReader.ReadInvoiceDate(text) ?? Date(@"(?:Invoice Date|Tax Invoice Date|Document Date|Date)\s*(?:[:#\-]|\s)\s*(?<value>\d{1,4}[./-]\d{1,2}[./-]\d{1,4})"),
            InvoiceOcrFieldReader.ReadSupplierName(text)
                ?? InvoiceOcrFieldReader.NormalizeSupplierCandidate(LabelValue("Supplier Name", "Vendor Name", "Vendor", "Supplier")),
            InvoiceOcrFieldReader.ReadSupplierTrn(text) ?? LabelValue("TRN", "VAT TRN", "Tax Registration Number", "VAT Registration Number", "Tax Number", "Tax No"),
            InvoiceOcrFieldReader.ReadPurchaseOrderNumber(text) ?? LabelValue("PO Number", "PO No", "Purchase Order", "Purchase Order Number", "Customer PO", "Order Ref"),
            InvoiceOcrFieldReader.ReadCurrency(text) ?? LabelValue("Currency"),
            Number(@"(?:Net Amount|Net Total|Subtotal)\s*(?:[:#\-]|\s)\s*(?:AED|USD|EUR|GBP|SAR|QAR|OMR|BHD|KWD)?\s*(?<value>[\d.,]+)"),
            Number(@"(?:Tax Amount|VAT Amount|VAT|Tax)\s*(?:[:#\-]|\s)\s*(?:AED|USD|EUR|GBP|SAR|QAR|OMR|BHD|KWD)?\s*(?<value>[\d.,]+)"),
            InvoiceOcrFieldReader.ReadGrossAmount(text) ?? Number(@"(?:Grand Total|Gross Total|Invoice Total|Total Amount|Amount Due|Total Including VAT|Total Incl VAT|Gross Amount|Total)\s*(?:[:#\-]|\s)\s*(?:AED|USD|EUR|GBP|SAR|QAR|OMR|BHD|KWD)?\s*(?<value>[\d.,]+)"),
            lines);
    }

    private static ExtractedInvoiceLine? ParseTableRow(string row, int fallbackLineNumber)
    {
        var qtyWithUom = Regex.Match(row, @"\b(\d+(?:[.,]\d+)?)\s*(KG|PC|PCS|EA|LTR|L|BOX|CTN)\b", RegexOptions.IgnoreCase);
        var material = Regex.Match(row, @"\(Material:\s*([^)]+)\)", RegexOptions.IgnoreCase);
        var amounts = Regex.Matches(row, @"\d{1,3}(?:,\d{3})+(?:\.\d{1,2})?|\d+\.\d{2}|\b\d+\b");
        if (amounts.Count == 0 && !qtyWithUom.Success) return null;

        decimal? Parse(string value) =>
            decimal.TryParse(value.Replace(",", ""), NumberStyles.Any, CultureInfo.InvariantCulture, out var parsed) ? parsed : null;

        decimal? quantity = null;
        decimal? unitPrice = null;
        decimal? amount = null;
        var lineNumber = fallbackLineNumber;
        string? uom = null;

        if (qtyWithUom.Success)
        {
            quantity = Parse(qtyWithUom.Groups[1].Value);
            uom = qtyWithUom.Groups[2].Value.ToUpperInvariant();
            if (amounts.Count > 0) amount = Parse(amounts[^1].Value);
        }
        else if (amounts.Count >= 4)
        {
            lineNumber = int.TryParse(amounts[0].Value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var poItem) && poItem > 0
                ? poItem
                : fallbackLineNumber;
            quantity = Parse(amounts[1].Value);
            unitPrice = Parse(amounts[2].Value);
            amount = Parse(amounts[3].Value);
        }
        else if (amounts.Count == 3)
        {
            quantity = Parse(amounts[0].Value);
            unitPrice = Parse(amounts[1].Value);
            amount = Parse(amounts[2].Value);
        }
        else if (amounts.Count >= 1)
        {
            if (qtyWithUom.Success) amount = Parse(amounts[^1].Value);
            else return null;
        }

        if (quantity is null && amount is null) return null;
        if (quantity is > 0 && amount is > 0 && unitPrice is null)
            unitPrice = amount / quantity;

        var description = Regex.Replace(row, @"\b\d{1,3}(?:,\d{3})+(?:\.\d{1,2})?|\d+\.\d{2}|\b\d+\b", " ");
        description = Regex.Replace(description, @"\s+", " ").Trim().Trim('|');
        if (string.IsNullOrWhiteSpace(description)) return null;

        string? sku = material.Success ? material.Groups[1].Value.Trim() : null;
        if (string.IsNullOrWhiteSpace(sku))
        {
            var code = Regex.Match(description, @"^([A-Z0-9][A-Z0-9._/-]{1,24})\b", RegexOptions.IgnoreCase);
            if (code.Success && Regex.IsMatch(code.Groups[1].Value, @"\d"))
                sku = code.Groups[1].Value;
        }

        return new ExtractedInvoiceLine(lineNumber, sku, description, quantity, uom, unitPrice, null, null, amount);
    }
}

public sealed class LocalGrnPostingProvider : IGrnPostingProvider
{
    public string Name => "LOCAL_DEVELOPMENT";
}

public sealed class OperationalException(
    string code,
    string message,
    IReadOnlyList<string>? missingFields = null,
    Guid? existingInvoiceId = null,
    string? duplicateType = null) : Exception(message)
{
    public string Code { get; } = code;
    public IReadOnlyList<string>? MissingFields { get; } = missingFields;
    public Guid? ExistingInvoiceId { get; } = existingInvoiceId;
    public string? DuplicateType { get; } = duplicateType;
}

public sealed class OperationalService(
    SilaMeDbContext db,
    IDocumentStorageService storage,
    IPdfTextExtractor pdfTextExtractor,
    IOcrProvider ocrProvider,
    IInvoiceExtractionService invoiceExtractor,
    IGrnPostingProvider grnPostingProvider,
    IAccessService accessService,
    IExtractionProviderResolver extractionProviderResolver,
    BasicInvoiceExtractionService basicInvoiceExtractionService,
    AdvancedInvoiceOcrService advancedInvoiceOcrService,
    IntegrationService integrationService,
    InvoiceProcessingQueue invoiceProcessingQueue,
    DocumentRoutingService documentRoutingService,
    ITenantContextAccessor tenants,
    ILogger<OperationalService> logger)
{
    private static readonly HashSet<string> AllowedExtensions = [".pdf", ".png", ".jpg", ".jpeg"];
    private static readonly HashSet<string> AllowedContentTypes = ["application/pdf", "image/png", "image/jpeg"];
    private const long MaxDocumentSize = 20 * 1024 * 1024;

    private static bool HasSupportedFileIdentity(IFormFile file)
    {
        var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
        var contentType = file.ContentType?.Split(';', 2)[0].Trim().ToLowerInvariant();
        return AllowedExtensions.Contains(extension) || (contentType is not null && AllowedContentTypes.Contains(contentType));
    }

    private static bool HasPdfSignature(ReadOnlySpan<byte> bytes) =>
        bytes.Length >= 4 && bytes[..4].SequenceEqual("%PDF"u8);

    // PROTECTED SILA INVOICE RECEIVING FLOW — See docs/PROTECTED_INVOICE_FLOW.md.
    // Persists reviewed invoice/lines. Does not post GRN.
    public async Task<DocumentResponse> UploadInvoiceAsync(
        Session session,
        Guid organizationId,
        Guid? operatingUnitId,
        DocumentSourceChannel sourceChannel,
        IFormFile file,
        CancellationToken cancellationToken,
        int? pageCount = null,
        string? scanSessionId = null,
        string? supplierName = null,
        Guid? supplierId = null,
        string? supplierTrn = null,
        string? supplierInvoiceNumber = null,
        DateOnly? invoiceDate = null,
        string? purchaseOrderNumber = null,
        decimal? invoiceGross = null,
        string? currency = null,
        string? ocrRequestId = null,
        string? manualEditedFieldsJson = null,
        bool deferFullExtraction = false,
        bool noPurchaseOrder = false,
        string? idempotencyKey = null,
        string? invoiceLinesJson = null)
    {
        if (file.Length <= 0 || file.Length > MaxDocumentSize)
            throw new OperationalException("INVALID_DOCUMENT", "Invoice files must be smaller than 20 MB.");
        if (!HasSupportedFileIdentity(file))
            throw new OperationalException("UNSUPPORTED_FILE", "Only PDF, PNG, JPG, and JPEG invoices are supported.");

        await EnsureScopeAsync(session, organizationId, operatingUnitId, PermissionKeys.UploadInvoice, cancellationToken);
        var reviewed = sourceChannel == DocumentSourceChannel.MOBILE_SCANNER;
        if (sourceChannel == DocumentSourceChannel.MOBILE_SCANNER)
        {
            if (string.IsNullOrWhiteSpace(scanSessionId))
                throw new OperationalException("SCAN_SESSION_REQUIRED", "A scan session ID is required for scanner uploads.");
            if (pageCount is < 1 or > 20)
                throw new OperationalException("INVALID_PAGE_COUNT", "A scan must contain between 1 and 20 pages.");
            var existing = await db.Documents.Include(item => item.Invoice)
                .SingleOrDefaultAsync(item => item.OrganizationId == organizationId && item.ScanSessionId == scanSessionId, cancellationToken);
            if (existing?.Invoice is not null)
                return ToDocumentResponse(existing, existing.Invoice.Id, "IDEMPOTENT_REPLAY", "PO_MATCH", "This scan was already saved.");
        }

        logger.LogInformation("[INVOICE-SAVE] VALIDATION_STARTED organization={OrganizationId} scanSession={ScanSessionId}", organizationId, scanSessionId);
        if (reviewed)
        {
            var missingFields = await GetMissingReviewedFieldsAsync(
                organizationId, supplierName, supplierInvoiceNumber, purchaseOrderNumber, invoiceGross, invoiceDate, currency, supplierTrn, noPurchaseOrder, cancellationToken);
            if (missingFields.Count > 0)
                throw new OperationalException("INVOICE_REVIEW_FIELDS_REQUIRED",
                    $"Complete the required invoice fields: {string.Join(", ", missingFields)}.", missingFields: missingFields);
        }

        await using var content = new MemoryStream();
        await file.CopyToAsync(content, cancellationToken);
        var contentHash = Convert.ToHexString(SHA256.HashData(content.GetBuffer().AsSpan(0, (int)content.Length)));
        content.Position = 0;

        if (!string.IsNullOrWhiteSpace(idempotencyKey))
        {
            var existingKey = await db.IdempotencyRecords.SingleOrDefaultAsync(item =>
                item.OrganizationId == organizationId && item.UserId == session.UserId &&
                item.IdempotencyKey == idempotencyKey.Trim() && item.Operation == "SAVE_INVOICE", cancellationToken);
            if (existingKey is not null)
            {
                if (!string.Equals(existingKey.RequestHash, contentHash, StringComparison.Ordinal))
                    throw new OperationalException("IDEMPOTENCY_KEY_REUSED", "The save request key was already used for different invoice content.");
                if (existingKey.Status == IdempotencyStatus.COMPLETED && Guid.TryParse(existingKey.ResponseReference, out var savedInvoiceId))
                {
                    var savedDocument = await db.Documents.Include(item => item.Invoice)
                        .SingleOrDefaultAsync(item => item.Invoice != null && item.Invoice.Id == savedInvoiceId, cancellationToken);
                    if (savedDocument?.Invoice is not null)
                        return ToDocumentResponse(savedDocument, savedDocument.Invoice.Id, "IDEMPOTENT_REPLAY", "PO_MATCH", "This invoice was already saved.");
                }
                throw new OperationalException("SAVE_IN_PROGRESS", "This invoice save is already being processed. Retry shortly.");
            }
        }

        var exactDuplicate = await db.Documents.Include(item => item.Invoice)
            .SingleOrDefaultAsync(item => item.OrganizationId == organizationId && item.ContentHash == contentHash && item.Invoice != null, cancellationToken);
        if (exactDuplicate?.Invoice is not null)
            return ToDocumentResponse(exactDuplicate, exactDuplicate.Invoice.Id, "IDEMPOTENT_REPLAY", "GRN_COMPARISON", "This document was already saved as an invoice.");

        Supplier? selectedSupplier = null;
        if (reviewed)
        {
            if (supplierId is null)
                throw new OperationalException("SUPPLIER_REQUIRED", "Select the supplier from Supplier Master before continuing.");
            selectedSupplier = await db.Suppliers.SingleOrDefaultAsync(item =>
                item.Id == supplierId && item.OrganizationId == organizationId &&
                item.Status == StatusKind.ACTIVE && !item.IsBlocked && !item.IsDeleted, cancellationToken)
                ?? throw new OperationalException("SUPPLIER_NOT_FOUND", "The selected supplier was not found in this organization.");
            logger.LogInformation("[INVOICE-SAVE] SUPPLIER_VALIDATED supplierId={SupplierId} supplierCode={SupplierCode}", selectedSupplier.Id, selectedSupplier.SupplierCode);
            logger.LogInformation("[INVOICE-SAVE] DUPLICATE_CHECK_STARTED");
            var normalizedInvoiceNumber = Normalize(supplierInvoiceNumber!);
            var probableDuplicate = (await db.Invoices
                .Where(item => item.OrganizationId == organizationId && item.SupplierId == selectedSupplier.Id)
                .Select(item => new { item.Id, item.InvoiceNumber })
                .ToListAsync(cancellationToken))
                .FirstOrDefault(item => Normalize(item.InvoiceNumber) == normalizedInvoiceNumber);
            if (probableDuplicate is not null)
                throw new OperationalException("DUPLICATE_INVOICE", "This supplier invoice already exists in SILA ME.",
                    existingInvoiceId: probableDuplicate.Id, duplicateType: "PROBABLE");
        }

        IdempotencyRecord? idempotency = null;
        if (!string.IsNullOrWhiteSpace(idempotencyKey))
        {
            idempotency = new IdempotencyRecord
            {
                Id = Guid.NewGuid(),
                OrganizationId = organizationId,
                UserId = session.CustomerUserId(),
                IdempotencyKey = idempotencyKey.Trim(),
                Operation = "SAVE_INVOICE",
                RequestHash = contentHash,
                Status = IdempotencyStatus.STARTED,
                CreatedAt = DateTime.UtcNow,
                ExpiresAt = DateTime.UtcNow.AddHours(24),
            };
            db.IdempotencyRecords.Add(idempotency);
        }
        var stored = await storage.StoreAsync(content, file.FileName, cancellationToken);
        var now = DateTime.UtcNow;
        var document = new Document
        {
            Id = Guid.NewGuid(),
            OrganizationId = organizationId,
            OperatingUnitId = operatingUnitId,
            UploadedByUserId = session.CustomerUserId(),
            DocumentType = DocumentType.INVOICE,
            OriginalFilename = Path.GetFileName(file.FileName),
            ContentType = file.ContentType ?? "application/octet-stream",
            FileSizeBytes = stored.Size,
            StorageProvider = stored.Provider,
            StorageReference = stored.Reference,
            ScanSessionId = scanSessionId?.Trim(),
            OcrRequestId = string.IsNullOrWhiteSpace(ocrRequestId) ? null : ocrRequestId.Trim(),
            ContentHash = contentHash,
            PageCount = pageCount,
            Status = deferFullExtraction ? DocumentStatus.UPLOADED : DocumentStatus.READING,
            SourceChannel = sourceChannel,
            CreatedAt = now,
            UpdatedAt = now,
        };
        var invoice = new Invoice
        {
            Id = Guid.NewGuid(),
            OrganizationId = organizationId,
            OperatingUnitId = operatingUnitId,
            DocumentId = document.Id,
            SupplierId = selectedSupplier?.Id,
            InvoiceNumber = string.IsNullOrWhiteSpace(supplierInvoiceNumber) ? $"PENDING-{document.Id:N}"[..24] : supplierInvoiceNumber.Trim(),
            InvoiceDate = invoiceDate,
            SupplierNameRaw = selectedSupplier?.Name ?? InvoiceOcrFieldReader.NormalizeSupplierCandidate(supplierName) ?? supplierName?.Trim(),
            SupplierTaxNumberRaw = supplierTrn?.Trim(),
            PoNumberRaw = purchaseOrderNumber?.Trim(),
            Currency = currency?.Trim().ToUpperInvariant(),
            GrossAmount = invoiceGross,
            ManualEditedFieldsJson = manualEditedFieldsJson,
            InvoiceType = InvoiceType.UNKNOWN,
            Status = InvoiceStatus.PROCESSING,
            CreatedByUserId = session.CustomerUserId(),
            ReviewedAt = reviewed ? now : null,
            ReviewedByUserId = reviewed ? session.UserId : null,
            CreatedAt = now,
            UpdatedAt = now,
        };
        if (reviewed && selectedSupplier is not null && !noPurchaseOrder && !string.IsNullOrWhiteSpace(purchaseOrderNumber))
        {
            var po = await ResolveAuthoritativePurchaseOrderAsync(organizationId, selectedSupplier.Id, selectedSupplier.EntityCode, purchaseOrderNumber, cancellationToken);
            if (po is not null)
            {
                invoice.PurchaseOrderId = po.Id;
                invoice.Currency = string.IsNullOrWhiteSpace(invoice.Currency) ? po.Currency : invoice.Currency;
                if (PurchaseOrderReceiving.IsServiceOnly(po)) invoice.InvoiceType = InvoiceType.SERVICE;
                logger.LogInformation("[INVOICE-SAVE] PO_VALIDATED purchaseOrderId={PurchaseOrderId} poNumber={PoNumber}", po.Id, po.PoNumber);
                AttachReviewedInvoiceLines(invoice, po, invoiceLinesJson);
            }
        }
        else if (reviewed)
            AttachReviewedInvoiceLines(invoice, null, invoiceLinesJson);
        db.Documents.Add(document);
        db.Invoices.Add(invoice);
        db.AuditEvents.Add(Audit(session, organizationId, operatingUnitId, "INVOICE_UPLOADED", "Invoice", invoice.Id, invoice.InvoiceNumber));
        if (sourceChannel == DocumentSourceChannel.MOBILE_SCANNER)
        {
            db.DocumentExtractions.Add(new DocumentExtraction
            {
                Id = Guid.NewGuid(),
                DocumentId = document.Id,
                ExtractionType = ExtractionType.BASIC_INVOICE,
                Provider = "MOBILE_OCR",
                ExtractionMethod = string.IsNullOrWhiteSpace(supplierInvoiceNumber) ? ExtractionMethod.OCR : ExtractionMethod.USER_CORRECTED,
                OcrRequestId = document.OcrRequestId,
                Status = ProcessingStatus.COMPLETED,
                CreatedAt = now,
                ProcessingStartedAt = now,
                ProcessingCompletedAt = now,
            });
            db.InvoiceExtData.Add(new InvoiceExtData
            {
                Id = Guid.NewGuid(),
                DocumentId = document.Id,
                OrganizationId = organizationId,
                OperatingUnitId = operatingUnitId,
                SupplierName = invoice.SupplierNameRaw,
                SupplierTrn = invoice.SupplierTaxNumberRaw,
                SupplierInvoiceNumber = invoice.InvoiceNumber,
                InvoiceDate = invoice.InvoiceDate,
                PurchaseOrderNumber = invoice.PoNumberRaw,
                InvoiceGross = invoice.GrossAmount,
                Currency = invoice.Currency,
                SourceProvider = "MOBILE_OCR",
                ExtractionMethod = string.IsNullOrWhiteSpace(manualEditedFieldsJson) ? ExtractionMethod.OCR.ToString() : ExtractionMethod.USER_CORRECTED.ToString(),
                CreatedAt = now,
                UpdatedAt = now,
            });
            db.AuditEvents.Add(Audit(session, organizationId, operatingUnitId, "MOBILE_INVOICE_SAVED", "Invoice", invoice.Id, invoice.InvoiceNumber));
        }
        if (reviewed)
            db.AuditEvents.Add(Audit(session, organizationId, operatingUnitId, "INVOICE_REVIEW_SAVED", "Invoice", invoice.Id, invoice.InvoiceNumber));
        await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);
        await db.SaveChangesAsync(cancellationToken);
        if (idempotency is not null)
        {
            idempotency.Status = IdempotencyStatus.COMPLETED;
            idempotency.ResponseReference = invoice.Id.ToString();
            await db.SaveChangesAsync(cancellationToken);
        }
        logger.LogInformation("[INVOICE-SAVE] DOCUMENT_ROUTING_STARTED documentId={DocumentId}", document.Id);
        await documentRoutingService.QueueTransferAsync(document, session, cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        logger.LogInformation("[INVOICE-SAVE] INVOICE_PERSISTED invoiceId={InvoiceId} documentId={DocumentId}", invoice.Id, document.Id);
        logger.LogInformation("[INVOICE-SAVE] SAVE_COMPLETED invoiceId={InvoiceId}", invoice.Id);
        if (deferFullExtraction)
        {
            if (tenants.Current is { } context)
            {
                await invoiceProcessingQueue.EnqueueAsync(new InvoiceProcessingJob(session, invoice.Id, context.TenantId, context.EnvironmentId), cancellationToken);
            }
        }
        else
        {
            await ProcessInvoiceAsync(session, invoice.Id, cancellationToken);
        }
        document.Invoice = invoice;
        return ToDocumentResponse(document, invoice.Id, "SAVED", invoice.PurchaseOrderId is null ? "PO_MATCH" : "GRN_COMPARISON",
            invoice.PurchaseOrderId is null ? "Invoice saved. Continue with purchase-order matching." : "Invoice saved. Continue with GRN comparison.");
    }

    private async Task<List<string>> GetMissingReviewedFieldsAsync(
        Guid organizationId,
        string? supplierName,
        string? supplierInvoiceNumber,
        string? purchaseOrderNumber,
        decimal? invoiceGross,
        DateOnly? invoiceDate,
        string? currency,
        string? supplierTrn,
        bool noPurchaseOrder,
        CancellationToken cancellationToken)
    {
        var config = await db.InvoiceOcrConfigurations.AsNoTracking()
            .SingleOrDefaultAsync(item => item.OrganizationId == organizationId, cancellationToken);
        var missing = new List<string>();
        if ((config?.RequireSupplierName ?? true) && string.IsNullOrWhiteSpace(supplierName)) missing.Add("supplierName");
        if ((config?.RequireInvoiceNumber ?? true) && string.IsNullOrWhiteSpace(supplierInvoiceNumber)) missing.Add("supplierInvoiceNumber");
        if (!noPurchaseOrder && (config?.RequirePurchaseOrderNumber ?? true) && string.IsNullOrWhiteSpace(purchaseOrderNumber)) missing.Add("purchaseOrderNumber");
        if ((config?.RequireInvoiceAmount ?? true) && invoiceGross is null) missing.Add("invoiceGross");
        if ((config?.RequireInvoiceDate ?? false) && invoiceDate is null) missing.Add("invoiceDate");
        if ((config?.RequireCurrency ?? false) && string.IsNullOrWhiteSpace(currency)) missing.Add("currency");
        if ((config?.RequireSupplierTrn ?? false) && string.IsNullOrWhiteSpace(supplierTrn)) missing.Add("supplierTrn");
        return missing;
    }

    private static DocumentResponse ToDocumentResponse(Document document, Guid invoiceId, string saveStatus, string nextStep, string? message = null) =>
        new(document.Id, document.OriginalFilename, document.ContentType, document.FileSizeBytes, document.PageCount,
            document.SourceChannel, document.Status, document.CreatedAt, invoiceId, saveStatus, nextStep, message);

    public async Task<BasicOcrResponse> ExtractBasicInvoiceAsync(
        Session session,
        Guid organizationId,
        Guid? operatingUnitId,
        int? pageCount,
        string? ocrRequestId,
        IFormFile file,
        CancellationToken cancellationToken)
    {
        logger.LogInformation(
            "[BASIC-OCR] Request={RequestId} BACKEND_DOCUMENT_RECEIVED fileName={FileName} fileSize={FileSize} contentType={ContentType} pageCount={PageCount}",
            ocrRequestId ?? "not-recorded",
            Path.GetFileName(file.FileName),
            file.Length,
            file.ContentType,
            pageCount);
        if (file.Length <= 0 || file.Length > MaxDocumentSize)
            throw new OperationalException("INVALID_DOCUMENT", "Invoice files must be smaller than 20 MB.");
        if (!HasSupportedFileIdentity(file))
            throw new OperationalException("UNSUPPORTED_FILE", "Only PDF, PNG, JPG, and JPEG invoices are supported.");
        if (pageCount is < 1 or > 20)
            throw new OperationalException("INVALID_PAGE_COUNT", "A scan must contain between 1 and 20 pages.");

        await EnsureScopeAsync(session, organizationId, operatingUnitId, PermissionKeys.UploadInvoice, cancellationToken);
        var document = new Document
        {
            Id = Guid.NewGuid(),
            OrganizationId = organizationId,
            OperatingUnitId = operatingUnitId,
            UploadedByUserId = session.CustomerUserId(),
            DocumentType = DocumentType.INVOICE,
            OriginalFilename = Path.GetFileName(file.FileName),
            ContentType = file.ContentType ?? "application/octet-stream",
            StorageProvider = "TEMPORARY",
            StorageReference = "BASIC_OCR",
            SourceChannel = DocumentSourceChannel.MOBILE_SCANNER,
            OcrRequestId = string.IsNullOrWhiteSpace(ocrRequestId) ? $"ocr-{Guid.NewGuid():N}" : ocrRequestId.Trim(),
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
        };
        logger.LogInformation("[BASIC-OCR] Request={RequestId} TEXT_EXTRACTION_STARTED", document.OcrRequestId);
        await using var uploaded = file.OpenReadStream();
        using var memory = new MemoryStream();
        await uploaded.CopyToAsync(memory, cancellationToken);
        var bytes = memory.ToArray();
        var isPdf = Path.GetExtension(file.FileName).Equals(".pdf", StringComparison.OrdinalIgnoreCase)
            || string.Equals(file.ContentType?.Split(';', 2)[0].Trim(), "application/pdf", StringComparison.OrdinalIgnoreCase);
        if (isPdf && !HasPdfSignature(bytes))
            throw new OperationalException("INVALID_DOCUMENT", "The uploaded PDF is empty or invalid.");

        await using var content = new MemoryStream(bytes, writable: false);
        var result = await ocrProvider.ExtractAsync(document, content, cancellationToken);
        var rawText = result.Text?.Trim();
        var basic = string.IsNullOrWhiteSpace(rawText) ? null : basicInvoiceExtractionService.Extract(rawText);
        logger.LogInformation(
            "[BASIC-OCR] Request={RequestId} RAW_TEXT_LENGTH={RawTextLength} provider={Provider}",
            document.OcrRequestId,
            rawText?.Length ?? 0,
            ocrProvider.Name);
        var fieldCount = basic is null
            ? 0
            : new[]
            {
                basic.SupplierName,
                basic.SupplierTrn,
                basic.SupplierInvoiceNumber,
                basic.InvoiceDate?.ToString("yyyy-MM-dd"),
                basic.PurchaseOrderNumber,
                basic.InvoiceGross?.ToString(CultureInfo.InvariantCulture),
                basic.Currency,
            }.Count(value => !string.IsNullOrWhiteSpace(value));
        var status = string.IsNullOrWhiteSpace(rawText)
            ? "FAILED"
            : fieldCount == 7 ? "SUCCESS" : "PARTIAL";
        logger.LogInformation(
            "[BASIC-OCR] PARSER_RESULT fieldCount={FieldCount} status={Status}",
            fieldCount,
            status);
        return new BasicOcrResponse(
            basic?.SupplierName,
            basic?.SupplierTrn,
            basic?.SupplierInvoiceNumber,
            basic?.InvoiceDate,
            basic?.PurchaseOrderNumber,
            basic?.InvoiceGross,
            basic?.Currency,
            "BUILT_IN_BACKEND",
            !string.IsNullOrWhiteSpace(rawText),
            status,
            result.Confidence,
            rawText);
    }

    public async Task ProcessInvoiceAsync(Session session, Guid invoiceId, CancellationToken cancellationToken)
    {
        var invoice = await db.Invoices
            .Include(item => item.Document)
            .Include(item => item.Lines)
            .SingleOrDefaultAsync(item => item.Id == invoiceId, cancellationToken)
            ?? throw new OperationalException("INVOICE_NOT_FOUND", "The invoice was not found.");
        await EnsureScopeAsync(session, invoice.OrganizationId, invoice.OperatingUnitId, PermissionKeys.EditInvoice, cancellationToken);

        invoice.Status = InvoiceStatus.PROCESSING;
        invoice.UpdatedAt = DateTime.UtcNow;
        invoice.Document.Status = DocumentStatus.READING;
        var started = DateTime.UtcNow;
        db.AuditEvents.Add(Audit(session, invoice.OrganizationId, invoice.OperatingUnitId, "BASIC_EXTRACTION_STARTED", "Invoice", invoice.Id, invoice.InvoiceNumber));
        var extraction = new DocumentExtraction
        {
            Id = Guid.NewGuid(),
            DocumentId = invoice.DocumentId,
            ExtractionType = ExtractionType.PDF_TEXT,
            Provider = "EMBEDDED_PDF_TEXT",
            ProcessingStartedAt = started,
            Status = ProcessingStatus.PROCESSING,
            CreatedAt = started,
        };
        db.DocumentExtractions.Add(extraction);
        await db.SaveChangesAsync(cancellationToken);

        await using var content = await storage.GetAsync(invoice.Document.StorageReference, cancellationToken);
        string? text = invoice.Document.ContentType.Contains("pdf", StringComparison.OrdinalIgnoreCase)
            ? await pdfTextExtractor.ExtractAsync(content, cancellationToken)
            : null;
        var extractionMethod = ExtractionMethod.PDF_TEXT;
        var providerName = "EMBEDDED_PDF_TEXT";
        var fallbackUsed = false;
        if (!string.IsNullOrWhiteSpace(text))
        {
            extraction.RawText = text;
            extraction.Confidence = 0.95m;
            extraction.Status = ProcessingStatus.COMPLETED;
            extraction.ProcessingCompletedAt = DateTime.UtcNow;
        }
        else
        {
            await content.DisposeAsync();
            await using var ocrContent = await storage.GetAsync(invoice.Document.StorageReference, cancellationToken);
            extraction.ExtractionType = ExtractionType.OCR;
            var resolved = await extractionProviderResolver.ResolveAsync(invoice.OrganizationId, invoice.Document.DocumentType, cancellationToken);
            providerName = resolved.Provider.Name;
            extraction.Provider = providerName;
            extractionMethod = resolved.Configuration is null ? ExtractionMethod.OCR : ExtractionMethod.EXTERNAL_AGENT;
            if (resolved.Configuration is not null)
                db.AuditEvents.Add(Audit(session, invoice.OrganizationId, invoice.OperatingUnitId, "EXTERNAL_AGENT_SELECTED", "Invoice", invoice.Id, providerName));
            (string? Text, decimal? Confidence) ocr;
            try
            {
                ocr = await resolved.Provider.ExtractAsync(invoice.Document, ocrContent, cancellationToken);
            }
            catch (Exception exception) when (resolved.Configuration is not null)
            {
                fallbackUsed = true;
                extraction.FallbackUsed = true;
                extraction.ErrorCategory = exception is OperationalException operational ? operational.Code : "EXTERNAL_PROVIDER_ERROR";
                extraction.ErrorMessage = "The configured external provider failed; built-in extraction was used.";
                db.AuditEvents.Add(Audit(session, invoice.OrganizationId, invoice.OperatingUnitId, "EXTERNAL_AGENT_FAILED", "Invoice", invoice.Id, providerName));
                db.AuditEvents.Add(Audit(session, invoice.OrganizationId, invoice.OperatingUnitId, "BUILTIN_OCR_FALLBACK", "Invoice", invoice.Id, ocrProvider.Name));
                await ocrContent.DisposeAsync();
                await using var fallbackContent = await storage.GetAsync(invoice.Document.StorageReference, cancellationToken);
                extractionMethod = ExtractionMethod.OCR;
                providerName = ocrProvider.Name;
                extraction.Provider = providerName;
                ocr = await ocrProvider.ExtractAsync(invoice.Document, fallbackContent, cancellationToken);
            }
            extraction.RawText = ocr.Text;
            extraction.Confidence = ocr.Confidence;
            extraction.Status = string.IsNullOrWhiteSpace(ocr.Text) ? ProcessingStatus.FAILED : ProcessingStatus.COMPLETED;
            extraction.ProcessingCompletedAt = DateTime.UtcNow;
            if (string.IsNullOrWhiteSpace(ocr.Text))
            {
                invoice.Status = InvoiceStatus.OCR_FAILED;
                invoice.Document.Status = DocumentStatus.FAILED;
                extraction.ErrorCode = "OCR_FAILED";
                extraction.ErrorMessage ??= "The built-in OCR provider could not read this document.";
                await db.SaveChangesAsync(cancellationToken);
                return;
            }
            text = ocr.Text;
        }

        invoice.Document.Status = DocumentStatus.BASIC_EXTRACTION_COMPLETE;
        var basic = basicInvoiceExtractionService.Extract(text!);
        db.AuditEvents.Add(Audit(session, invoice.OrganizationId, invoice.OperatingUnitId, "BASIC_EXTRACTION_COMPLETED", "Invoice", invoice.Id, invoice.InvoiceNumber));
        invoice.Document.Status = DocumentStatus.FULL_EXTRACTION_PROCESSING;
        var extracted = invoiceExtractor.Extract(text!);
        var duplicate = extracted.InvoiceNumber is not null && await db.Invoices.AnyAsync(item =>
            item.OrganizationId == invoice.OrganizationId &&
            item.SupplierId != null &&
            item.InvoiceNumber == extracted.InvoiceNumber &&
            item.Id != invoice.Id, cancellationToken);
        if (duplicate)
        {
            invoice.Status = InvoiceStatus.FAILED;
            invoice.Document.Status = DocumentStatus.FAILED;
            throw new OperationalException("DUPLICATE_INVOICE", "This invoice appears to have already been uploaded.");
        }

        var mobileConfirmed = invoice.Document.SourceChannel == DocumentSourceChannel.MOBILE_SCANNER;
        var preserveSupplier = mobileConfirmed && invoice.SupplierId is not null;
        var preservePo = mobileConfirmed && invoice.PurchaseOrderId is not null;
        invoice.InvoiceNumber = mobileConfirmed && !invoice.InvoiceNumber.StartsWith("PENDING-", StringComparison.Ordinal)
            ? invoice.InvoiceNumber
            : extracted.InvoiceNumber ?? invoice.InvoiceNumber;
        invoice.InvoiceDate = mobileConfirmed && invoice.InvoiceDate is not null ? invoice.InvoiceDate : extracted.InvoiceDate ?? invoice.InvoiceDate;
        invoice.SupplierNameRaw = mobileConfirmed && !string.IsNullOrWhiteSpace(invoice.SupplierNameRaw) ? invoice.SupplierNameRaw : extracted.SupplierName ?? invoice.SupplierNameRaw;
        invoice.SupplierTaxNumberRaw = mobileConfirmed && !string.IsNullOrWhiteSpace(invoice.SupplierTaxNumberRaw) ? invoice.SupplierTaxNumberRaw : extracted.SupplierTaxNumber ?? invoice.SupplierTaxNumberRaw;
        invoice.PoNumberRaw = mobileConfirmed && !string.IsNullOrWhiteSpace(invoice.PoNumberRaw) ? invoice.PoNumberRaw : extracted.PoNumber ?? invoice.PoNumberRaw;
        invoice.Currency = mobileConfirmed && !string.IsNullOrWhiteSpace(invoice.Currency) ? invoice.Currency : extracted.Currency ?? invoice.Currency;
        invoice.NetAmount = extracted.NetAmount ?? invoice.NetAmount;
        invoice.TaxAmount = extracted.TaxAmount ?? invoice.TaxAmount;
        invoice.GrossAmount = mobileConfirmed && invoice.GrossAmount is not null ? invoice.GrossAmount : extracted.GrossAmount ?? invoice.GrossAmount;
        invoice.OverallConfidence = extraction.Confidence;
        invoice.InvoiceType = extracted.Lines.Count == 0 ? InvoiceType.SERVICE : InvoiceType.MATERIAL;
        db.InvoiceLines.RemoveRange(invoice.Lines);
        foreach (var line in extracted.Lines)
        {
            invoice.Lines.Add(new InvoiceLine
            {
                Id = Guid.NewGuid(),
                InvoiceId = invoice.Id,
                LineNumber = line.LineNumber,
                SupplierMaterialCode = line.SupplierMaterialCode,
                DescriptionRaw = line.Description,
                Quantity = line.Quantity,
                Uom = line.Uom,
                UnitPrice = line.UnitPrice,
                TaxRate = line.TaxRate,
                TaxAmount = line.TaxAmount,
                LineAmount = line.LineAmount,
                Confidence = extraction.Confidence,
                MatchStatus = InvoiceLineMatchStatus.UNMATCHED,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
            });
        }

        if (!preserveSupplier) await MatchSupplierInternalAsync(invoice, cancellationToken);
        if (!preservePo) await MatchPurchaseOrderInternalAsync(invoice, cancellationToken);
        await MatchLinesInternalAsync(invoice, cancellationToken);
        invoice.Document.Status = DocumentStatus.FULL_EXTRACTION_COMPLETE;
        invoice.Status = invoice.PurchaseOrderId is not null && invoice.Lines.All(line => line.MatchStatus == InvoiceLineMatchStatus.MATCHED)
            ? InvoiceStatus.READY_FOR_GRN
            : InvoiceStatus.REVIEW_REQUIRED;
        invoice.UpdatedAt = DateTime.UtcNow;
        extraction.ExtractionType = ExtractionType.FULL_INVOICE;
        extraction.Provider = providerName;
        extraction.ExtractionMethod = extractionMethod;
        extraction.FallbackUsed = fallbackUsed;
        extraction.ProcessingDurationMs = (long)(DateTime.UtcNow - started).TotalMilliseconds;
        extraction.Status = ProcessingStatus.COMPLETED;
        extraction.ProcessingCompletedAt = DateTime.UtcNow;
        await ReplaceInvoiceExtDataAsync(invoice, extraction, extractionMethod, providerName, cancellationToken);
        db.AuditEvents.Add(Audit(session, invoice.OrganizationId, invoice.OperatingUnitId, "FULL_EXTRACTION_COMPLETED", "Invoice", invoice.Id, invoice.InvoiceNumber));
        db.AuditEvents.Add(Audit(session, invoice.OrganizationId, invoice.OperatingUnitId, "INVOICE_EXTRACTION_COMPLETED", "Invoice", invoice.Id, invoice.InvoiceNumber));
        await db.SaveChangesAsync(cancellationToken);
    }

    public async Task MatchSupplierAsync(Session session, Guid invoiceId, Guid? supplierId, CancellationToken cancellationToken)
    {
        var invoice = await GetInvoiceEntityAsync(invoiceId, cancellationToken);
        await EnsureScopeAsync(session, invoice.OrganizationId, invoice.OperatingUnitId, PermissionKeys.EditInvoice, cancellationToken);
        if (supplierId is not null)
        {
            var supplier = await db.Suppliers.SingleOrDefaultAsync(item => item.Id == supplierId && item.OrganizationId == invoice.OrganizationId && item.Status == StatusKind.ACTIVE && !item.IsBlocked && !item.IsDeleted, cancellationToken)
                ?? throw new OperationalException("SUPPLIER_NOT_FOUND", "The supplier was not found in this organization.");
            invoice.SupplierId = supplier.Id;
        }
        else await MatchSupplierInternalAsync(invoice, cancellationToken);
        invoice.UpdatedAt = DateTime.UtcNow;
        await db.SaveChangesAsync(cancellationToken);
    }

    public async Task MatchPurchaseOrderAsync(Session session, Guid invoiceId, Guid? purchaseOrderId, string? poNumber, CancellationToken cancellationToken)
    {
        var invoice = await GetInvoiceEntityAsync(invoiceId, cancellationToken);
        await EnsureScopeAsync(session, invoice.OrganizationId, invoice.OperatingUnitId, PermissionKeys.MatchPurchaseOrder, cancellationToken);
        invoice.PoNumberRaw = poNumber ?? invoice.PoNumberRaw;
        if (invoice.SupplierId is null)
            throw new OperationalException("SUPPLIER_REQUIRED", "Select the supplier from Supplier Master before selecting a purchase order.");
        if (purchaseOrderId is not null)
        {
            var supplierEntityCode = await db.Suppliers
                .Where(item => item.Id == invoice.SupplierId)
                .Select(item => item.EntityCode)
                .SingleAsync(cancellationToken);
            var selected = await db.PurchaseOrders.Include(item => item.Items)
                .SingleOrDefaultAsync(item => item.Id == purchaseOrderId, cancellationToken)
                ?? throw new OperationalException("PO_NOT_FOUND", "The selected purchase order was not found.");
            if (selected.OrganizationId != invoice.OrganizationId ||
                (!PurchaseOrderReceiving.IsUnscopedEntity(supplierEntityCode) &&
                 !PurchaseOrderReceiving.MatchesRequestedEntity(supplierEntityCode, selected.EntityCode)))
                throw new OperationalException("PO_SCOPE_MISMATCH", "The selected purchase order is outside the current organization or entity.");
            if (selected.SupplierId != invoice.SupplierId)
                throw new OperationalException("PO_SUPPLIER_MISMATCH", "The selected purchase order does not belong to this supplier.");
            if (!PurchaseOrderReceiving.IsHeaderOpen(selected))
                throw new OperationalException("PO_NOT_OPEN", "The selected purchase order is not open.");
            if (!selected.Items.Any(PurchaseOrderReceiving.IsGoodsReceiptEligible))
                throw new OperationalException("PO_NOT_GR_ELIGIBLE", "The selected purchase order has no goods-receipt-eligible open items.");
            invoice.PurchaseOrderId = selected.Id;
            invoice.PoNumberRaw = selected.PoNumber;
        }
        else
        {
            await MatchPurchaseOrderInternalAsync(invoice, cancellationToken);
        }
        await MatchLinesInternalAsync(invoice, cancellationToken);
        invoice.Status = invoice.PurchaseOrderId is not null && invoice.Lines.All(item => item.MatchStatus == InvoiceLineMatchStatus.MATCHED) ? InvoiceStatus.READY_FOR_GRN : InvoiceStatus.REVIEW_REQUIRED;
        invoice.UpdatedAt = DateTime.UtcNow;
        await db.SaveChangesAsync(cancellationToken);
    }

    public async Task MatchLinesAsync(Session session, Guid invoiceId, MatchInvoiceLinesRequest request, CancellationToken cancellationToken)
    {
        var invoice = await GetInvoiceEntityAsync(invoiceId, cancellationToken);
        await EnsureScopeAsync(session, invoice.OrganizationId, invoice.OperatingUnitId, PermissionKeys.MatchPurchaseOrder, cancellationToken);
        if (invoice.PurchaseOrderId is null) throw new OperationalException("PO_NOT_FOUND", "Select a purchase order before matching lines.");
        var itemIds = await db.PurchaseOrderItems.Where(item => item.PurchaseOrderId == invoice.PurchaseOrderId).Select(item => item.Id).ToListAsync(cancellationToken);
        foreach (var match in request.Lines)
        {
            var line = invoice.Lines.SingleOrDefault(item => item.Id == match.InvoiceLineId) ?? throw new OperationalException("INVOICE_LINE_NOT_FOUND", "The invoice line was not found.");
            if (match.PurchaseOrderItemId is null || !itemIds.Contains(match.PurchaseOrderItemId.Value))
            {
                line.PurchaseOrderItemId = null;
                line.MatchStatus = InvoiceLineMatchStatus.UNMATCHED;
            }
            else
            {
                line.PurchaseOrderItemId = match.PurchaseOrderItemId;
                line.MatchStatus = InvoiceLineMatchStatus.MATCHED;
            }
            line.UpdatedAt = DateTime.UtcNow;
        }
        invoice.Status = invoice.Lines.All(item => item.MatchStatus == InvoiceLineMatchStatus.MATCHED) ? InvoiceStatus.READY_FOR_GRN : InvoiceStatus.REVIEW_REQUIRED;
        await db.SaveChangesAsync(cancellationToken);
    }

    public async Task<BasicInvoiceExtractionResponse> GetBasicExtractionAsync(Session session, Guid invoiceId, CancellationToken cancellationToken)
    {
        var invoice = await GetInvoiceEntityAsync(invoiceId, cancellationToken);
        await EnsureScopeAsync(session, invoice.OrganizationId, invoice.OperatingUnitId, PermissionKeys.ViewExtraction, cancellationToken);
        return new BasicInvoiceExtractionResponse(
            invoice.DocumentId,
            invoice.SupplierNameRaw,
            invoice.SupplierTaxNumberRaw,
            invoice.InvoiceNumber.StartsWith("PENDING-", StringComparison.Ordinal) ? null : invoice.InvoiceNumber,
            invoice.InvoiceDate,
            invoice.PoNumberRaw ?? invoice.PurchaseOrder?.PoNumber,
            invoice.GrossAmount,
            invoice.Currency,
            invoice.Status);
    }

    public async Task<InvoiceOcrConfigurationResponse> GetInvoiceOcrConfigurationAsync(Session session, Guid? organizationId, CancellationToken cancellationToken)
    {
        var organization = organizationId ?? await db.UserOrganizationMemberships
            .Where(item => item.UserId == session.UserId && item.Status == StatusKind.ACTIVE)
            .Select(item => item.OrganizationId)
            .FirstOrDefaultAsync(cancellationToken);
        if (organization == Guid.Empty) throw new OperationalException("ACCESS_SCOPE_NOT_FOUND", "An organization scope is required.");
        await EnsureScopeAsync(session, organization, null, "VIEW_CONFIGURATION", cancellationToken);
        return ToInvoiceOcrConfigurationResponse(await advancedInvoiceOcrService.GetConfigurationAsync(organization, cancellationToken));
    }

    public async Task<InvoiceOcrConfigurationResponse> UpsertInvoiceOcrConfigurationAsync(
        Session session,
        Guid? organizationId,
        UpsertInvoiceOcrConfigurationRequest request,
        CancellationToken cancellationToken)
    {
        var organization = organizationId ?? await db.UserOrganizationMemberships
            .Where(item => item.UserId == session.UserId && item.Status == StatusKind.ACTIVE)
            .Select(item => item.OrganizationId)
            .FirstOrDefaultAsync(cancellationToken);
        if (organization == Guid.Empty) throw new OperationalException("ACCESS_SCOPE_NOT_FOUND", "An organization scope is required.");
        await EnsureScopeAsync(session, organization, null, "EDIT_CONFIGURATION", cancellationToken);
        var config = await db.InvoiceOcrConfigurations.SingleOrDefaultAsync(item => item.OrganizationId == organization, cancellationToken);
        var now = DateTime.UtcNow;
        if (config is null)
        {
            config = new InvoiceOcrConfiguration
            {
                Id = Guid.NewGuid(),
                OrganizationId = organization,
                CreatedByUserId = session.CustomerUserId(),
                CreatedAt = now,
            };
            db.InvoiceOcrConfigurations.Add(config);
        }

        config.MobileBasicOcrEnabled = request.MobileBasicOcrEnabled;
        config.AutomaticBackendFallbackEnabled = request.AutomaticBackendFallbackEnabled;
        config.MinimumMobileConfidence = request.MinimumMobileConfidence;
        config.RequireSupplierName = request.RequireSupplierName;
        config.RequireInvoiceNumber = request.RequireInvoiceNumber;
        config.RequirePurchaseOrderNumber = request.RequirePurchaseOrderNumber;
        config.RequireInvoiceAmount = request.RequireInvoiceAmount;
        config.RequireInvoiceDate = request.RequireInvoiceDate;
        config.RequireCurrency = request.RequireCurrency;
        config.RequireSupplierTrn = request.RequireSupplierTrn;
        config.BackendProvider = request.BackendProvider.Trim().ToUpperInvariant();
        config.AlwaysBackendOnReread = request.AlwaysBackendOnReread;
        config.DetailedLineExtractionEnabled = request.DetailedLineExtractionEnabled;
        config.SupplierMasterValidationEnabled = request.SupplierMasterValidationEnabled;
        config.PurchaseOrderValidationEnabled = request.PurchaseOrderValidationEnabled;
        config.FinancialReconciliationEnabled = request.FinancialReconciliationEnabled;
        config.AmountTolerance = request.AmountTolerance;
        config.BackendTimeoutSeconds = request.BackendTimeoutSeconds;
        config.BackendRetryCount = request.BackendRetryCount;
        config.ReuseCachedOcr = request.ReuseCachedOcr;
        config.Version++;
        config.UpdatedByUserId = session.CustomerUserId();
        config.UpdatedAt = now;
        db.AuditEvents.Add(Audit(session, organization, null, "INVOICE_OCR_CONFIGURATION_UPDATED", "InvoiceOcrConfiguration", config.Id, config.BackendProvider));
        await db.SaveChangesAsync(cancellationToken);
        return ToInvoiceOcrConfigurationResponse(config);
    }

    public async Task<AdvancedInvoiceExtractionResponse> RunAdvancedInvoiceExtractionAsync(
        Session session,
        Guid documentId,
        ExtractionTrigger trigger,
        CancellationToken cancellationToken)
    {
        var document = await db.Documents.SingleOrDefaultAsync(item => item.Id == documentId, cancellationToken)
            ?? throw new OperationalException("DOCUMENT_NOT_FOUND", "The document was not found.");
        logger.LogInformation("[ADV-OCR] Request={RequestId} Event=STORED_DOCUMENT_RUN Document={DocumentId} Trigger={Trigger}",
            document.OcrRequestId ?? "not-recorded", document.Id, trigger);
        if (!await accessService.HasPermissionAsync(session, PermissionKeys.EditExtraction, document.OrganizationId, document.OperatingUnitId, cancellationToken))
            await EnsureScopeAsync(session, document.OrganizationId, document.OperatingUnitId, PermissionKeys.UploadInvoice, cancellationToken);
        await using var content = await storage.GetAsync(document.StorageReference, cancellationToken);
        var run = await advancedInvoiceOcrService.ExtractAsync(document, content, trigger, cancellationToken);
        var invoice = await db.Invoices.Include(item => item.Lines).SingleOrDefaultAsync(item => item.DocumentId == documentId, cancellationToken);
        if (invoice is not null)
            await ApplyAdvancedExtractionAsync(session, invoice, document, run, trigger, cancellationToken);
        return run.Response;
    }

    public async Task<AdvancedInvoiceExtractionResponse> RunAdvancedInvoiceExtractionFileAsync(
        Session session,
        AdvancedInvoiceExtractionRequest request,
        IFormFile file,
        CancellationToken cancellationToken)
    {
        if (file.Length <= 0 || file.Length > MaxDocumentSize) throw new OperationalException("INVALID_DOCUMENT", "Invoice files must be smaller than 20 MB.");
        if (!HasSupportedFileIdentity(file)) throw new OperationalException("UNSUPPORTED_FILE", "Only PDF, PNG, JPG, and JPEG invoices are supported.");
        await EnsureScopeAsync(session, request.OrganizationId, request.OperatingUnitId, PermissionKeys.UploadInvoice, cancellationToken);
        var document = new Document
        {
            Id = Guid.NewGuid(),
            OrganizationId = request.OrganizationId,
            OperatingUnitId = request.OperatingUnitId,
            UploadedByUserId = session.CustomerUserId(),
            DocumentType = DocumentType.INVOICE,
            OriginalFilename = Path.GetFileName(file.FileName),
            ContentType = file.ContentType ?? "application/octet-stream",
            FileSizeBytes = file.Length,
            StorageProvider = "TEMPORARY",
            StorageReference = "ADVANCED_OCR",
            PageCount = null,
            Status = DocumentStatus.READING,
            SourceChannel = DocumentSourceChannel.MOBILE_SCANNER,
            OcrRequestId = string.IsNullOrWhiteSpace(request.OcrRequestId) ? $"ocr-{Guid.NewGuid():N}" : request.OcrRequestId.Trim(),
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
        };
        var trigger = ParseTrigger(request.Trigger);
        if (trigger == ExtractionTrigger.AUTO_FALLBACK &&
            !await advancedInvoiceOcrService.IsMobileFallbackRequiredAsync(
                request.OrganizationId,
                request.MobileSupplierName,
                request.MobileSupplierInvoiceNumber,
                request.MobilePurchaseOrderNumber,
                request.MobileInvoiceGross,
                request.MobileConfidence,
                cancellationToken))
        {
            var configuration = await advancedInvoiceOcrService.GetConfigurationAsync(request.OrganizationId, cancellationToken);
            return advancedInvoiceOcrService.CreateSkippedResponse(document, trigger, configuration);
        }
        await using var content = file.OpenReadStream();
        return (await advancedInvoiceOcrService.ExtractAsync(document, content, trigger, cancellationToken)).Response;
    }

    private async Task ApplyAdvancedExtractionAsync(
        Session session,
        Invoice invoice,
        Document document,
        AdvancedOcrRun run,
        ExtractionTrigger trigger,
        CancellationToken cancellationToken)
    {
        var response = run.Response;
        var manualFields = ParseManualFields(invoice.ManualEditedFieldsJson);
        var lockReviewed = invoice.ReviewedAt is not null && trigger == ExtractionTrigger.INITIAL_BACKEND;
        if (!lockReviewed || (!manualFields.Contains("InvoiceNumber") && invoice.InvoiceNumber.StartsWith("PENDING-", StringComparison.Ordinal)))
        {
            if (!manualFields.Contains("InvoiceNumber") && !string.IsNullOrWhiteSpace(response.Header.InvoiceNumber.Value))
                invoice.InvoiceNumber = response.Header.InvoiceNumber.Value!;
        }
        if (!lockReviewed && !manualFields.Contains("InvoiceDate")) invoice.InvoiceDate = response.Header.InvoiceDate.Value;
        if (!lockReviewed && !manualFields.Contains("SupplierName")) invoice.SupplierNameRaw = response.Header.SupplierName.Value;
        if (!lockReviewed && !manualFields.Contains("SupplierTaxNumber")) invoice.SupplierTaxNumberRaw = response.Header.SupplierTrn.Value;
        if (!lockReviewed && !manualFields.Contains("PoNumber")) invoice.PoNumberRaw = response.Header.PurchaseOrderNumber.Value;
        if (!lockReviewed && !manualFields.Contains("Currency")) invoice.Currency = response.Header.Currency.Value?.ToUpperInvariant();
        if (!lockReviewed && !manualFields.Contains("NetAmount")) invoice.NetAmount = response.Header.NetAmount.Value;
        if (!lockReviewed && !manualFields.Contains("TaxAmount")) invoice.TaxAmount = response.Header.TaxAmount.Value;
        if (!lockReviewed && !manualFields.Contains("GrossAmount")) invoice.GrossAmount = response.Header.GrossAmount.Value;
        invoice.SupplierLegalName = response.Header.SupplierLegalName.Value;
        invoice.SupplierAddress = response.Header.SupplierAddress.Value;
        invoice.SupplierEmail = response.Header.SupplierEmail.Value;
        invoice.SupplierPhone = response.Header.SupplierPhone.Value;
        invoice.DiscountAmount = response.Header.DiscountAmount.Value;
        invoice.FreightAmount = response.Header.FreightAmount.Value;
        invoice.OtherCharges = response.Header.OtherCharges.Value;
        invoice.TaxableAmount = response.Header.TaxableAmount.Value;
        invoice.AmountDue = response.Header.AmountDue.Value;
        invoice.PaymentTerms = response.Header.PaymentTerms.Value;
        invoice.DueDate = response.Header.DueDate.Value;
        invoice.OverallConfidence = response.Confidence;
        invoice.ExtractionStatus = response.Status;
        invoice.ExtractionProvider = response.Provider;
        invoice.ExtractedAt = response.CompletedAt;
        invoice.InvoiceType = Enum.TryParse<InvoiceType>(response.Header.InvoiceType.Value, true, out var invoiceType) ? invoiceType : InvoiceType.UNKNOWN;

        var existingLines = await db.InvoiceLines
            .Where(item => item.InvoiceId == invoice.Id)
            .ToListAsync(cancellationToken);
        db.InvoiceLines.RemoveRange(existingLines);
        var replacementLines = response.Lines.Select(line => new InvoiceLine
            {
                Id = Guid.NewGuid(),
                InvoiceId = invoice.Id,
                LineNumber = line.LineNumber,
                SupplierMaterialCode = line.SupplierItemCode.Value,
                MaterialCodeRaw = line.SupplierItemCode.Value,
                DescriptionRaw = line.Description.Value ?? string.Empty,
                Quantity = line.Quantity.Value,
                Uom = line.Uom.Value,
                UnitPrice = line.UnitPrice.Value,
                TaxRate = line.TaxRate.Value,
                TaxAmount = line.TaxAmount.Value,
                LineAmount = line.NetAmount.Value ?? line.GrossAmount.Value,
                DiscountAmount = line.DiscountAmount.Value,
                GrossAmount = line.GrossAmount.Value,
                PoItemNumber = line.PoItemNumber.Value,
                BatchNumber = line.BatchNumber.Value,
                ExpiryDate = line.ExpiryDate.Value,
                Confidence = line.GrossAmount.Confidence ?? line.Description.Confidence,
                MatchStatus = InvoiceLineMatchStatus.UNMATCHED,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
            }).ToList();
        invoice.Lines = replacementLines;
        db.InvoiceLines.AddRange(replacementLines);

        if (invoice.SupplierId is null) await MatchSupplierInternalAsync(invoice, cancellationToken);
        if (invoice.PurchaseOrderId is null) await MatchPurchaseOrderInternalAsync(invoice, cancellationToken);
        await MatchLinesInternalAsync(invoice, cancellationToken);
        invoice.Status = response.RequiresReview
            ? InvoiceStatus.REVIEW_REQUIRED
            : invoice.PurchaseOrderId is not null && invoice.Lines.All(item => item.MatchStatus == InvoiceLineMatchStatus.MATCHED)
                ? InvoiceStatus.READY_FOR_GRN
                : InvoiceStatus.REVIEW_REQUIRED;
        document.Status = response.RequiresReview ? DocumentStatus.REVIEW_REQUIRED : DocumentStatus.FULL_EXTRACTION_COMPLETE;
        invoice.UpdatedAt = DateTime.UtcNow;

        var extraction = new DocumentExtraction
        {
            Id = Guid.NewGuid(),
            DocumentId = document.Id,
            ExtractionType = ExtractionType.FULL_INVOICE,
            Provider = response.Provider,
            RawText = run.RawText,
            Confidence = response.Confidence,
            ProcessingStartedAt = response.StartedAt,
            ProcessingCompletedAt = response.CompletedAt,
            Status = response.Status == "SUCCESS" ? ProcessingStatus.COMPLETED : ProcessingStatus.REVIEW_REQUIRED,
            ExtractionMethod = ExtractionMethod.OCR,
            Trigger = trigger,
            OcrRequestId = document.OcrRequestId,
            ContentHash = run.ContentHash,
            ConfigurationSnapshotJson = run.ConfigurationSnapshotJson,
            StructuredPayloadJson = JsonSerializer.Serialize(response),
            ProcessingDurationMs = response.StartedAt.HasValue && response.CompletedAt.HasValue
                ? (long)(response.CompletedAt.Value - response.StartedAt.Value).TotalMilliseconds
                : null,
            CreatedAt = DateTime.UtcNow,
        };
        db.DocumentExtractions.Add(extraction);
        await ReplaceInvoiceExtDataAsync(invoice, extraction, ExtractionMethod.OCR, response.Provider, cancellationToken);
        db.AuditEvents.Add(Audit(session, invoice.OrganizationId, invoice.OperatingUnitId,
            trigger == ExtractionTrigger.MANUAL_REREAD ? "INVOICE_EXTRACTION_UPDATED" : "BACKEND_OCR_COMPLETED",
            "Invoice", invoice.Id, invoice.InvoiceNumber));
        await db.SaveChangesAsync(cancellationToken);
    }

    private static HashSet<string> ParseManualFields(string? json)
    {
        if (string.IsNullOrWhiteSpace(json)) return [];
        try
        {
            var raw = JsonSerializer.Deserialize<HashSet<string>>(json) ?? [];
            var fields = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (var field in raw)
            {
                fields.Add(field switch
                {
                    "supplierName" => "SupplierName",
                    "supplierTaxNumber" => "SupplierTaxNumber",
                    "supplierInvoiceNumber" => "InvoiceNumber",
                    "invoiceDate" => "InvoiceDate",
                    "purchaseOrderNumber" => "PoNumber",
                    "invoiceGross" => "GrossAmount",
                    "currency" => "Currency",
                    _ => field,
                });
            }
            return fields;
        }
        catch (JsonException) { return []; }
    }

    private static ExtractionTrigger ParseTrigger(string? value) =>
        Enum.TryParse<ExtractionTrigger>(value, true, out var trigger) ? trigger : ExtractionTrigger.AUTO_FALLBACK;

    private static InvoiceOcrConfigurationResponse ToInvoiceOcrConfigurationResponse(InvoiceOcrConfiguration config) =>
        new(config.Id, config.OrganizationId, config.MobileBasicOcrEnabled, config.AutomaticBackendFallbackEnabled,
            config.MinimumMobileConfidence, config.RequireSupplierName, config.RequireInvoiceNumber, config.RequirePurchaseOrderNumber,
            config.RequireInvoiceAmount, config.RequireInvoiceDate, config.RequireCurrency, config.RequireSupplierTrn, config.BackendProvider,
            config.AlwaysBackendOnReread, config.DetailedLineExtractionEnabled, config.SupplierMasterValidationEnabled,
            config.PurchaseOrderValidationEnabled, config.FinancialReconciliationEnabled, config.AmountTolerance,
            config.BackendTimeoutSeconds, config.BackendRetryCount, config.ReuseCachedOcr, config.Version, config.UpdatedAt);

    public async Task<InvoiceExtractionResponse> GetFullExtractionAsync(Session session, Guid invoiceId, CancellationToken cancellationToken)
    {
        var invoice = await GetInvoiceEntityAsync(invoiceId, cancellationToken);
        await EnsureScopeAsync(session, invoice.OrganizationId, invoice.OperatingUnitId, PermissionKeys.ViewExtraction, cancellationToken);
        var extraction = await db.DocumentExtractions.AsNoTracking()
            .Where(item => item.DocumentId == invoice.DocumentId && item.ExtractionType == ExtractionType.FULL_INVOICE)
            .OrderByDescending(item => item.CreatedAt)
            .FirstOrDefaultAsync(cancellationToken);
        var rows = await db.InvoiceExtData.AsNoTracking()
            .Where(item => item.DocumentId == invoice.DocumentId)
            .OrderBy(item => item.LineItemNumber)
            .ToListAsync(cancellationToken);
        var lines = rows.Count > 0
            ? rows.Select(item => new InvoiceExtractionLineResponse(item.ItemSkuId, item.ItemAmount, item.ItemNet, item.ItemDescription, item.LineItemNumber, item.PurchaseOrderItemId)).ToList()
            : invoice.Lines.OrderBy(item => item.LineNumber).Select(item => new InvoiceExtractionLineResponse(item.MaterialCodeRaw, item.LineAmount, item.LineAmount, item.DescriptionRaw, item.LineNumber.ToString(CultureInfo.InvariantCulture), item.PurchaseOrderItemId)).ToList();
        return new InvoiceExtractionResponse(
            new InvoiceExtractionHeaderResponse(invoice.DocumentId, invoice.SupplierNameRaw, invoice.SupplierTaxNumberRaw,
                invoice.InvoiceNumber.StartsWith("PENDING-", StringComparison.Ordinal) ? null : invoice.InvoiceNumber, invoice.InvoiceDate,
                invoice.PoNumberRaw ?? invoice.PurchaseOrder?.PoNumber, invoice.GrossAmount, invoice.NetAmount, invoice.Currency),
            lines,
            extraction?.Provider ?? "BUILT_IN_OCR",
            extraction?.ExtractionMethod.ToString() ?? ExtractionMethod.PDF_TEXT.ToString(),
            extraction?.Confidence ?? invoice.OverallConfidence,
            extraction?.FallbackUsed ?? false,
            extraction?.Status.ToString() ?? ProcessingStatus.FAILED.ToString(),
            extraction?.ProcessingCompletedAt);
    }

    public async Task<IReadOnlyList<ExtractionAgentConfigResponse>> GetExtractionAgentConfigsAsync(Session session, Guid? organizationId, CancellationToken cancellationToken)
    {
        var organization = organizationId ?? await db.UserOrganizationMemberships.Where(item => item.UserId == session.UserId && item.Status == StatusKind.ACTIVE).Select(item => item.OrganizationId).FirstOrDefaultAsync(cancellationToken);
        if (organization == Guid.Empty) throw new OperationalException("ACCESS_SCOPE_NOT_FOUND", "An organization scope is required.");
        await EnsureScopeAsync(session, organization, null, PermissionKeys.ViewAgentConfig, cancellationToken);
        return await db.ExtractionAgentConfigs.AsNoTracking()
            .Where(item => item.OrganizationId == organization || item.OrganizationId == null)
            .OrderBy(item => item.Priority)
            .Select(item => ToExtractionAgentConfigResponse(item))
            .ToListAsync(cancellationToken);
    }

    public async Task<ExtractionAgentConfigResponse> UpsertExtractionAgentConfigAsync(Session session, Guid? organizationId, Guid? configId, UpsertExtractionAgentConfigRequest request, CancellationToken cancellationToken)
    {
        var organization = organizationId ?? await db.UserOrganizationMemberships.Where(item => item.UserId == session.UserId && item.Status == StatusKind.ACTIVE).Select(item => item.OrganizationId).FirstOrDefaultAsync(cancellationToken);
        if (organization == Guid.Empty) throw new OperationalException("ACCESS_SCOPE_NOT_FOUND", "An organization scope is required.");
        await EnsureScopeAsync(session, organization, null, PermissionKeys.ManageAgentConfig, cancellationToken);
        if (!string.Equals(request.DocumentType, DocumentType.INVOICE.ToString(), StringComparison.OrdinalIgnoreCase))
            throw new OperationalException("UNSUPPORTED_DOCUMENT_TYPE", "Only INVOICE extraction configuration is enabled.");
        var config = configId is null
            ? new ExtractionAgentConfig { Id = Guid.NewGuid(), OrganizationId = organization, CreatedByUserId = session.CustomerUserId(), CreatedAt = DateTime.UtcNow, Name = request.Name.Trim(), DocumentType = "INVOICE", ProviderType = request.ProviderType.Trim(), AuthenticationType = request.AuthenticationType }
            : await db.ExtractionAgentConfigs.SingleOrDefaultAsync(item => item.Id == configId && item.OrganizationId == organization, cancellationToken)
                ?? throw new OperationalException("AGENT_CONFIG_NOT_FOUND", "The extraction agent configuration was not found.");
        config.Name = request.Name.Trim();
        config.DocumentType = "INVOICE";
        config.ProviderType = request.ProviderType.Trim().ToUpperInvariant();
        config.EndpointUrl = request.EndpointUrl?.Trim();
        config.AuthenticationType = request.AuthenticationType;
        config.CredentialReference = request.CredentialReference?.Trim();
        if (!string.IsNullOrWhiteSpace(config.CredentialReference) &&
            !config.CredentialReference.StartsWith("secret://", StringComparison.OrdinalIgnoreCase))
            throw new OperationalException("CREDENTIAL_REFERENCE_REQUIRED", "Store a secret:// reference, never a raw provider credential.");
        config.CredentialLast4 = string.IsNullOrWhiteSpace(config.CredentialReference) ? null : MaskCredential(config.CredentialReference);
        config.ConfigurationJson = request.ConfigurationJson;
        config.Priority = request.Priority;
        config.IsActive = request.IsActive;
        config.UpdatedAt = DateTime.UtcNow;
        if (configId is null) db.ExtractionAgentConfigs.Add(config);
        db.AuditEvents.Add(Audit(session, organization, null, configId is null ? "AGENT_CONFIG_CREATED" : "AGENT_CONFIG_UPDATED", "ExtractionAgentConfig", config.Id, config.Name));
        if (config.IsActive) db.AuditEvents.Add(Audit(session, organization, null, "AGENT_CONFIG_ACTIVATED", "ExtractionAgentConfig", config.Id, config.Name));
        await db.SaveChangesAsync(cancellationToken);
        return ToExtractionAgentConfigResponse(config);
    }

    public async Task<ExtractionAgentTestResponse> TestExtractionAgentConfigAsync(Session session, Guid configId, CancellationToken cancellationToken)
    {
        var config = await db.ExtractionAgentConfigs.SingleOrDefaultAsync(item => item.Id == configId, cancellationToken)
            ?? throw new OperationalException("AGENT_CONFIG_NOT_FOUND", "The extraction agent configuration was not found.");
        await EnsureScopeAsync(session, config.OrganizationId ?? Guid.Empty, null, PermissionKeys.TestAgentConfig, cancellationToken);
        var valid = Uri.TryCreate(config.EndpointUrl, UriKind.Absolute, out var endpoint) && endpoint.Scheme is "http" or "https";
        db.AuditEvents.Add(Audit(session, config.OrganizationId ?? Guid.Empty, null, "AGENT_CONNECTION_TESTED", "ExtractionAgentConfig", config.Id, config.Name));
        await db.SaveChangesAsync(cancellationToken);
        return valid
            ? new ExtractionAgentTestResponse(true, "The extraction endpoint configuration is valid.")
            : new ExtractionAgentTestResponse(false, "Set a valid HTTP or HTTPS endpoint before testing the connection.");
    }

    private async Task ReplaceInvoiceExtDataAsync(Invoice invoice, DocumentExtraction extraction, ExtractionMethod method, string provider, CancellationToken cancellationToken)
    {
        var previous = await db.InvoiceExtData
            .AsNoTracking()
            .Where(item => item.DocumentId == invoice.DocumentId)
            .OrderByDescending(item => item.UpdatedAt)
            .FirstOrDefaultAsync(cancellationToken);
        if (invoice.Document.SourceChannel == DocumentSourceChannel.MOBILE_SCANNER)
        {
            invoice.SupplierNameRaw ??= previous?.SupplierName;
            invoice.SupplierTaxNumberRaw ??= previous?.SupplierTrn;
            invoice.PoNumberRaw ??= previous?.PurchaseOrderNumber;
            invoice.GrossAmount ??= previous?.InvoiceGross;
            invoice.Currency ??= previous?.Currency;
        }
        await db.InvoiceExtData
            .Where(item => item.DocumentId == invoice.DocumentId)
            .ExecuteDeleteAsync(cancellationToken);
        var rows = invoice.Lines.Count == 0
            ? new[] { new InvoiceLine { LineNumber = 0, DescriptionRaw = string.Empty } }
            : invoice.Lines.OrderBy(item => item.LineNumber).ToArray();
        foreach (var line in rows)
        {
            db.InvoiceExtData.Add(new InvoiceExtData
            {
                Id = Guid.NewGuid(),
                DocumentId = invoice.DocumentId,
                OrganizationId = invoice.OrganizationId,
                OperatingUnitId = invoice.OperatingUnitId,
                SupplierName = invoice.SupplierNameRaw,
                SupplierTrn = invoice.SupplierTaxNumberRaw,
                SupplierInvoiceNumber = invoice.InvoiceNumber.StartsWith("PENDING-", StringComparison.Ordinal) ? null : invoice.InvoiceNumber,
                InvoiceDate = invoice.InvoiceDate,
                PurchaseOrderNumber = invoice.PoNumberRaw,
                InvoiceGross = invoice.GrossAmount,
                InvoiceNet = invoice.NetAmount,
                Currency = invoice.Currency,
                ItemSkuId = line.MaterialCodeRaw,
                ItemAmount = line.LineAmount,
                ItemNet = line.LineAmount,
                ItemDescription = string.IsNullOrWhiteSpace(line.DescriptionRaw) ? null : line.DescriptionRaw,
                LineItemNumber = line.LineNumber == 0 ? null : line.LineNumber.ToString(CultureInfo.InvariantCulture),
                PurchaseOrderItemId = line.PurchaseOrderItemId,
                SourceProvider = provider,
                ExtractionMethod = method.ToString(),
                ExtractionConfidence = extraction.Confidence,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
            });
        }
    }

    private static ExtractionAgentConfigResponse ToExtractionAgentConfigResponse(ExtractionAgentConfig config) =>
        new(config.Id, config.OrganizationId, config.Name, config.DocumentType, config.ProviderType, config.EndpointUrl,
            config.AuthenticationType, config.CredentialLast4 is null ? null : $"••••••••{config.CredentialLast4}", config.Priority, config.IsActive, config.CreatedAt, config.UpdatedAt);

    private static string MaskCredential(string reference) =>
        reference.Length <= 4 ? reference : reference[^4..];

    public async Task<GrnValidationResponse> ValidateGrnAsync(Session session, ValidateGrnRequest request, CancellationToken cancellationToken)
    {
        var errors = await ValidateGrnInternalAsync(session, request, cancellationToken);
        return new GrnValidationResponse(errors.Count == 0, errors, []);
    }

    // Downstream of Invoice Review. Consumes saved InvoiceId. Do not change OCR/review here.
    public async Task<GoodsReceipt> PostGrnAsync(Session session, PostGrnRequest request, string? idempotencyKey, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(idempotencyKey)) throw new OperationalException("IDEMPOTENCY_KEY_REQUIRED", "Idempotency-Key is required when posting a GRN.");
        var invoice = await GetInvoiceEntityAsync(request.InvoiceId, cancellationToken);
        await EnsureScopeAsync(session, invoice.OrganizationId, NormalizeOperatingUnitId(request.OperatingUnitId), PermissionKeys.PostGrn, cancellationToken);
        var existing = await db.IdempotencyRecords.SingleOrDefaultAsync(item =>
            item.OrganizationId == invoice.OrganizationId && item.UserId == session.UserId && item.IdempotencyKey == idempotencyKey && item.Operation == "POST_GRN", cancellationToken);
        if (existing?.Status == IdempotencyStatus.COMPLETED && Guid.TryParse(existing.ResponseReference, out var existingId))
            return await GetGoodsReceiptEntityAsync(existingId, cancellationToken);
        if (existing?.Status == IdempotencyStatus.FAILED && Guid.TryParse(existing.ResponseReference, out var failedId))
        {
            var failed = await db.GoodsReceipts.AsNoTracking().SingleOrDefaultAsync(item => item.Id == failedId, cancellationToken);
            if (failed is { Status: GoodsReceiptStatus.UNKNOWN })
                throw new OperationalException("ERP_POST_UNKNOWN", "The previous ERP posting outcome is unknown. Reconcile before posting again.");
            if (failed is not null && failed.Status != GoodsReceiptStatus.POSTED)
                return await RetryGrnAsync(session, failed.Id, cancellationToken);
        }
        if (existing is not null) throw new OperationalException("IDEMPOTENCY_CONFLICT", "This posting request is already in progress.");
        var errors = await ValidateGrnInternalAsync(session, request, cancellationToken);
        if (errors.Count > 0) throw new OperationalException("GRN_VALIDATION_FAILED", string.Join(" ", errors));

        var idempotency = new IdempotencyRecord
        {
            Id = Guid.NewGuid(), OrganizationId = invoice.OrganizationId, UserId = session.CustomerUserId(),
            IdempotencyKey = idempotencyKey, Operation = "POST_GRN", Status = IdempotencyStatus.STARTED, CreatedAt = DateTime.UtcNow,
        };
        db.IdempotencyRecords.Add(idempotency);
        await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);
        try
        {
            await db.Database.ExecuteSqlInterpolatedAsync(
                $"SELECT \"Id\" FROM \"purchase_order_items\" WHERE \"PurchaseOrderId\" = {request.PurchaseOrderId} FOR UPDATE",
                cancellationToken);
            var po = await db.PurchaseOrders.Include(item => item.Items).SingleAsync(item => item.Id == request.PurchaseOrderId, cancellationToken);
            var postedAccepted = await PostedAcceptedByPoItemAsync(po.Id, null, cancellationToken);
            ReconcilePurchaseOrderOpenQuantities(po, postedAccepted);
            var receiptNumber = await NextSrcGrnNumberAsync(invoice.OrganizationId, cancellationToken);
            var grn = new GoodsReceipt
            {
                Id = Guid.NewGuid(),
                OrganizationId = invoice.OrganizationId,
                OperatingUnitId = NormalizeOperatingUnitId(request.OperatingUnitId),
                GrnNumber = receiptNumber,
                PurchaseOrderId = po.Id,
                InvoiceId = invoice.Id,
                SupplierId = po.SupplierId,
                ReceiptDate = request.ReceiptDate ?? DateTime.UtcNow,
                Status = GoodsReceiptStatus.POSTING,
                PostingProvider = grnPostingProvider.Name,
                BusinessStatus = "RECEIVING",
                ErpPostingStatus = "PENDING",
                ErpMaterialDocument = receiptNumber,
                CreatedByUserId = session.CustomerUserId(),
                CreatedAt = DateTime.UtcNow,
            };
            db.GoodsReceipts.Add(grn);
            foreach (var input in request.Lines)
            {
                var item = po.Items.Single(line => line.Id == input.PurchaseOrderItemId);
                input.ReceivedQuantity = CapToRemaining(input.ReceivedQuantity, item.OpenQuantity);
                input.AcceptedQuantity = CapToRemaining(input.AcceptedQuantity, input.ReceivedQuantity);
                grn.Lines.Add(new GoodsReceiptLine
                {
                    Id = Guid.NewGuid(), GoodsReceiptId = grn.Id, PurchaseOrderItemId = item.Id, MaterialId = item.MaterialId,
                    MaterialCode = item.MaterialCode, Description = item.Description, OpenQuantityBefore = item.OpenQuantity,
                    InvoiceQuantity = invoice.Lines.SingleOrDefault(line => line.PurchaseOrderItemId == item.Id)?.Quantity,
                    ReceivedQuantity = input.ReceivedQuantity, AcceptedQuantity = input.AcceptedQuantity,
                    DamagedQuantity = input.DamagedQuantity, RejectedQuantity = input.RejectedQuantity, Uom = item.Uom,
                    BatchNumber = input.BatchNumber, ExpiryDate = input.ExpiryDate, CreatedAt = DateTime.UtcNow,
                });
            }

            await db.SaveChangesAsync(cancellationToken);
            await AssignErpReceiptNumberAsync(grn, cancellationToken);
            grn.Status = GoodsReceiptStatus.READY_TO_POST;
            grn.BusinessStatus = "READY_TO_POST";
            grn.ErpPostingStatus = "NOT_POSTED";
            idempotency.Status = IdempotencyStatus.COMPLETED;
            idempotency.ResponseReference = grn.Id.ToString();
            db.AuditEvents.Add(Audit(session, invoice.OrganizationId, request.OperatingUnitId, "GRN_PREPARED", "GoodsReceipt", grn.Id, grn.GrnNumber));
            await db.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);
            return await GetGoodsReceiptEntityAsync(grn.Id, cancellationToken);
        }
        catch
        {
            await transaction.RollbackAsync(cancellationToken);
            throw;
        }
    }

    // Downstream of Save and Continue. Starts from saved InvoiceId, not OCR.
    // Receiving scope is organization + PO company code/property, not operating unit.
    public async Task<GoodsReceipt> PrepareGrnAsync(Session session, PrepareGrnRequest request, CancellationToken cancellationToken)
    {
        var invoice = await GetInvoiceEntityAsync(request.InvoiceId, cancellationToken);
        var purchaseOrderId = request.PurchaseOrderId ?? invoice.PurchaseOrderId
            ?? throw new OperationalException("PURCHASE_ORDER_REQUIRED", "A purchase order is required before Finalize GRN.");
        await EnsureScopeAsync(session, invoice.OrganizationId, null, PermissionKeys.PostGrn, cancellationToken);

        var po = await db.PurchaseOrders.Include(item => item.Items).SingleOrDefaultAsync(item => item.Id == purchaseOrderId, cancellationToken)
            ?? throw new OperationalException("PO_NOT_FOUND", "The purchase order was not found.");
        var postedAccepted = await PostedAcceptedByPoItemAsync(po.Id, null, cancellationToken);
        ReconcilePurchaseOrderOpenQuantities(po, postedAccepted);
        await db.SaveChangesAsync(cancellationToken);

        var existing = await db.GoodsReceipts
            .Include(item => item.Lines)
            .Where(item => item.InvoiceId == invoice.Id && item.Status != GoodsReceiptStatus.CANCELLED)
            .OrderByDescending(item => item.CreatedAt)
            .FirstOrDefaultAsync(cancellationToken);
        if (existing is not null)
        {
            foreach (var line in existing.Lines)
            {
                var item = po.Items.Single(candidate => candidate.Id == line.PurchaseOrderItemId);
                line.OpenQuantityBefore = item.OpenQuantity;
                var invoiceQty = invoice.Lines.FirstOrDefault(candidate => candidate.PurchaseOrderItemId == item.Id)?.Quantity ?? line.InvoiceQuantity ?? 0;
                line.InvoiceQuantity = invoiceQty;
                if (existing.Status is GoodsReceiptStatus.READY_TO_POST or GoodsReceiptStatus.DRAFT or GoodsReceiptStatus.FAILED)
                {
                    var suggested = CapToRemaining(line.AcceptedQuantity > 0 ? line.AcceptedQuantity : invoiceQty, item.OpenQuantity);
                    line.AcceptedQuantity = suggested;
                    line.ReceivedQuantity = suggested;
                }
            }
            await db.SaveChangesAsync(cancellationToken);
            return await GetGoodsReceiptEntityAsync(existing.Id, cancellationToken);
        }

        var lines = po.Items.Where(PurchaseOrderReceiving.IsGoodsReceiptEligible).Select(item =>
        {
            var invoiceQty = invoice.Lines.FirstOrDefault(line => line.PurchaseOrderItemId == item.Id)?.Quantity ?? 0;
            var accepted = CapToRemaining(invoiceQty, item.OpenQuantity);
            return new GrnLineInput
            {
                PurchaseOrderItemId = item.Id,
                ReceivedQuantity = accepted,
                AcceptedQuantity = accepted,
            };
        }).Where(item => item.ReceivedQuantity > 0 || item.AcceptedQuantity > 0).ToList();
        if (lines.Count == 0)
        {
            lines = po.Items.Where(PurchaseOrderReceiving.IsGoodsReceiptEligible).Select(item => new GrnLineInput
            {
                PurchaseOrderItemId = item.Id,
                ReceivedQuantity = 0,
                AcceptedQuantity = 0,
            }).ToList();
        }
        return await PostGrnAsync(session, new PostGrnRequest
        {
            InvoiceId = invoice.Id,
            PurchaseOrderId = po.Id,
            OperatingUnitId = NormalizeOperatingUnitId(request.OperatingUnitId ?? invoice.OperatingUnitId ?? po.OperatingUnitId),
            Lines = lines,
        }, $"prepare-grn-{invoice.Id:N}", cancellationToken);
    }

    public async Task<GoodsReceipt> PostExistingGrnAsync(Session session, Guid goodsReceiptId, PostGoodsReceiptRequest? request, CancellationToken cancellationToken)
    {
        var grn = await GetGoodsReceiptEntityAsync(goodsReceiptId, cancellationToken);
        await EnsureScopeAsync(session, grn.OrganizationId, grn.OperatingUnitId, PermissionKeys.PostGrn, cancellationToken);
        if (grn.Status == GoodsReceiptStatus.POSTED) return grn;
        if (grn.Status == GoodsReceiptStatus.UNKNOWN || string.Equals(grn.ErpPostingStatus, "POSTING_UNKNOWN", StringComparison.OrdinalIgnoreCase))
            throw new OperationalException("ERP_POST_UNKNOWN", "The previous posting outcome is unknown. Reconcile before posting again.");

        await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);
        await db.Database.ExecuteSqlInterpolatedAsync(
            $"SELECT \"Id\" FROM \"purchase_order_items\" WHERE \"PurchaseOrderId\" = {grn.PurchaseOrderId} FOR UPDATE",
            cancellationToken);
        await db.Entry(grn).ReloadAsync(cancellationToken);
        var po = await db.PurchaseOrders.Include(item => item.Items).SingleAsync(item => item.Id == grn.PurchaseOrderId, cancellationToken);
        var postedAccepted = await PostedAcceptedByPoItemAsync(po.Id, grn.Id, cancellationToken);
        ReconcilePurchaseOrderOpenQuantities(po, postedAccepted);
        if (request?.Lines is { Count: > 0 })
        {
            foreach (var input in request.Lines)
            {
                var line = grn.Lines.SingleOrDefault(item => item.PurchaseOrderItemId == input.PurchaseOrderItemId)
                    ?? throw new OperationalException("GRN_LINE_NOT_FOUND", "A GRN line does not belong to this goods receipt.");
                var item = po.Items.Single(candidate => candidate.Id == input.PurchaseOrderItemId);
                var accepted = CapToRemaining(input.AcceptedQuantity, item.OpenQuantity);
                var received = CapToRemaining(input.ReceivedQuantity > 0 ? input.ReceivedQuantity : accepted, item.OpenQuantity);
                if (accepted > received) accepted = received;
                line.OpenQuantityBefore = item.OpenQuantity;
                line.ReceivedQuantity = received;
                line.AcceptedQuantity = accepted;
                line.DamagedQuantity = input.DamagedQuantity;
                line.RejectedQuantity = input.RejectedQuantity;
            }
        }
        foreach (var line in grn.Lines)
        {
            var item = po.Items.SingleOrDefault(candidate => candidate.Id == line.PurchaseOrderItemId)
                ?? throw new OperationalException("PO_LINE_NOT_FOUND", "A saved GRN line no longer belongs to its purchase order.");
            line.AcceptedQuantity = CapToRemaining(line.AcceptedQuantity, item.OpenQuantity);
            line.ReceivedQuantity = CapToRemaining(line.ReceivedQuantity, item.OpenQuantity);
            line.OpenQuantityBefore = item.OpenQuantity;
        }

        await AssignErpReceiptNumberAsync(grn, cancellationToken);
        grn.Status = GoodsReceiptStatus.POSTING;
        grn.BusinessStatus = "POSTING";
        grn.ErpPostingStatus = "POSTING";
        grn.LastErpAttemptAt = DateTime.UtcNow;
        grn.ErpAttemptCount++;
        grn.FailureCode = null;
        grn.FailureMessage = null;
        await db.SaveChangesAsync(cancellationToken);

        var erp = await integrationService.PostGoodsReceiptAsync(grn.OrganizationId, po.EntityCode, grn, cancellationToken);
        ApplyErpResult(grn, erp);
        if (!erp.Success)
        {
            ApplyErpFailure(grn, erp);
            await db.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);
            return await GetGoodsReceiptEntityAsync(grn.Id, cancellationToken);
        }

        ApplyPostedInventory(grn, po, session);
        db.AuditEvents.Add(Audit(session, grn.OrganizationId, grn.OperatingUnitId, "GRN_POSTED", "GoodsReceipt", grn.Id, grn.GrnNumber));
        await db.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return await GetGoodsReceiptEntityAsync(grn.Id, cancellationToken);
    }

    public async Task<Invoice> GetInvoiceEntityAsync(Guid id, CancellationToken cancellationToken) =>
        await db.Invoices.Include(item => item.Lines).Include(item => item.Supplier).Include(item => item.PurchaseOrder)
            .Include(item => item.OperatingUnit).SingleOrDefaultAsync(item => item.Id == id, cancellationToken)
        ?? throw new OperationalException("INVOICE_NOT_FOUND", "The invoice was not found.");

    public async Task<GoodsReceipt> RetryGrnAsync(Session session, Guid goodsReceiptId, CancellationToken cancellationToken) =>
        await PostExistingGrnAsync(session, goodsReceiptId, null, cancellationToken);

    public async Task<GoodsReceipt> GetGoodsReceiptEntityAsync(Guid id, CancellationToken cancellationToken) =>
        await db.GoodsReceipts.Include(item => item.Lines).ThenInclude(line => line.PurchaseOrderItem)
            .Include(item => item.PurchaseOrder).Include(item => item.Supplier).Include(item => item.Invoice)
            .Include(item => item.OperatingUnit).SingleOrDefaultAsync(item => item.Id == id, cancellationToken)
        ?? throw new OperationalException("GRN_NOT_FOUND", "The goods receipt was not found.");

    private async Task<List<string>> ValidateGrnInternalAsync(Session session, ValidateGrnRequest request, CancellationToken cancellationToken)
    {
        var errors = new List<string>();
        var invoice = await db.Invoices.Include(item => item.Lines).SingleOrDefaultAsync(item => item.Id == request.InvoiceId, cancellationToken);
        var po = await db.PurchaseOrders.Include(item => item.Items).SingleOrDefaultAsync(item => item.Id == request.PurchaseOrderId, cancellationToken);
        if (invoice is null) errors.Add("The invoice was not found.");
        if (po is null) errors.Add("The purchase order was not found.");
        if (invoice is null || po is null) return errors;
        var postedAccepted = await PostedAcceptedByPoItemAsync(po.Id, null, cancellationToken);
        ReconcilePurchaseOrderOpenQuantities(po, postedAccepted);
        await EnsureScopeAsync(session, invoice.OrganizationId, NormalizeOperatingUnitId(request.OperatingUnitId), PermissionKeys.PostGrn, cancellationToken);
        if (invoice.OrganizationId != po.OrganizationId) errors.Add("The invoice and purchase order do not belong together.");
        if (invoice.SupplierId is not null && invoice.SupplierId != po.SupplierId) errors.Add("The purchase order does not belong to the selected supplier.");
        if (invoice.PurchaseOrderId is not null && invoice.PurchaseOrderId != po.Id) errors.Add("The invoice and purchase order do not belong together.");
        if (po.Status is PurchaseOrderStatus.CLOSED or PurchaseOrderStatus.CANCELLED) errors.Add("The purchase order is not open for receiving.");
        if (PurchaseOrderReceiving.IsServiceOnly(po) || invoice.InvoiceType == InvoiceType.SERVICE)
            errors.Add("Service PO — goods receipt is not available in SILA ME.");
        var eligible = po.Items.Where(PurchaseOrderReceiving.IsGoodsReceiptEligible).Select(item => item.Id).ToHashSet();
        foreach (var input in request.Lines)
        {
            var item = po.Items.SingleOrDefault(line => line.Id == input.PurchaseOrderItemId);
            if (item is null) { errors.Add("A receipt line does not belong to the purchase order."); continue; }
            if (!eligible.Contains(item.Id) && !PurchaseOrderReceiving.IsGoodsReceiptEligible(item))
            {
                errors.Add($"Line {item.LineNumber} is not eligible for goods receipt.");
                continue;
            }
            if (input.ReceivedQuantity < 0 || input.AcceptedQuantity < 0 || input.DamagedQuantity < 0 || input.RejectedQuantity < 0) errors.Add($"Quantities for {item.Description} cannot be negative.");
            if (input.AcceptedQuantity + input.DamagedQuantity + input.RejectedQuantity > input.ReceivedQuantity) errors.Add($"Accepted, damaged, and rejected quantities cannot exceed received quantity for {item.Description}.");
            if (input.AcceptedQuantity > item.OpenQuantity)
                input.AcceptedQuantity = item.OpenQuantity;
            if (input.ReceivedQuantity > item.OpenQuantity)
                input.ReceivedQuantity = item.OpenQuantity;
        }
        return errors;
    }

    private async Task MatchSupplierInternalAsync(Invoice invoice, CancellationToken cancellationToken)
    {
        Supplier? supplier = null;
        var supplierCode = InvoiceOcrFieldReader.ReadSupplierId(invoice.SupplierNameRaw ?? string.Empty);
        if (string.IsNullOrWhiteSpace(supplierCode) && InvoiceOcrFieldReader.IsSupplierIdLabel(invoice.SupplierNameRaw))
            supplierCode = InvoiceOcrFieldReader.NormalizeSupplierCandidate(invoice.SupplierNameRaw);
        if (!string.IsNullOrWhiteSpace(supplierCode))
            supplier = await db.Suppliers.FirstOrDefaultAsync(item => item.OrganizationId == invoice.OrganizationId && !item.IsBlocked && !item.IsDeleted &&
                item.SupplierCode == supplierCode, cancellationToken);
        if (supplier is null && !string.IsNullOrWhiteSpace(invoice.SupplierTaxNumberRaw))
            supplier = await db.Suppliers.FirstOrDefaultAsync(item => item.OrganizationId == invoice.OrganizationId && !item.IsBlocked && !item.IsDeleted &&
                (item.TaxNumber == invoice.SupplierTaxNumberRaw || item.Trn == invoice.SupplierTaxNumberRaw), cancellationToken);
        if (supplier is null && !string.IsNullOrWhiteSpace(invoice.SupplierNameRaw) &&
            !InvoiceOcrFieldReader.IsSupplierIdLabel(invoice.SupplierNameRaw))
        {
            var normalized = Normalize(invoice.SupplierNameRaw);
            if (!normalized.StartsWith("SUPPLIER ID", StringComparison.Ordinal) &&
                !normalized.StartsWith("ID ", StringComparison.Ordinal))
            {
                supplier = await db.Suppliers.FirstOrDefaultAsync(item => item.OrganizationId == invoice.OrganizationId && !item.IsBlocked && !item.IsDeleted &&
                    (item.NormalizedName == normalized || item.SupplierCode == invoice.SupplierNameRaw.Trim() ||
                     item.Aliases.Any(alias => alias.NormalizedAlias == normalized)), cancellationToken);
            }
        }
        invoice.SupplierId = supplier?.Id;
    }

    private async Task MatchPurchaseOrderInternalAsync(Invoice invoice, CancellationToken cancellationToken)
    {
        if (invoice.SupplierId is null) return;
        var query = db.PurchaseOrders.Where(item => item.OrganizationId == invoice.OrganizationId && item.SupplierId == invoice.SupplierId &&
            item.Status != PurchaseOrderStatus.CLOSED && item.Status != PurchaseOrderStatus.CANCELLED &&
            item.Items.Any(line =>
                line.GoodsReceiptExpected &&
                !line.DeletionIndicator &&
                !line.DeliveryCompleted &&
                line.OpenQuantity > 0 &&
                line.Status != PurchaseOrderItemStatus.CLOSED &&
                line.Status != PurchaseOrderItemStatus.CANCELLED));
        if (!string.IsNullOrWhiteSpace(invoice.PoNumberRaw))
            invoice.PurchaseOrderId = await query.Where(item => item.PoNumber == invoice.PoNumberRaw).Select(item => (Guid?)item.Id).SingleOrDefaultAsync(cancellationToken);
        if (invoice.PurchaseOrderId is null && invoice.OperatingUnitId is not null)
            invoice.PurchaseOrderId = await query.Where(item => item.OperatingUnitId == invoice.OperatingUnitId && item.Status == PurchaseOrderStatus.OPEN).OrderBy(item => item.PoDate).Select(item => (Guid?)item.Id).FirstOrDefaultAsync(cancellationToken);
    }

    private async Task MatchLinesInternalAsync(Invoice invoice, CancellationToken cancellationToken)
    {
        if (invoice.PurchaseOrderId is null) return;
        var items = await db.PurchaseOrderItems.Where(item => item.PurchaseOrderId == invoice.PurchaseOrderId).ToListAsync(cancellationToken);
        foreach (var line in invoice.Lines)
        {
            var normalized = Normalize(line.DescriptionRaw);
            var match = items.FirstOrDefault(item => item.MaterialCode == line.MaterialCodeRaw)
                ?? items.FirstOrDefault(item => Normalize(item.Description) == normalized);
            line.PurchaseOrderItemId = match?.Id;
            line.MatchStatus = match is null ? InvoiceLineMatchStatus.UNMATCHED : InvoiceLineMatchStatus.MATCHED;
            line.UpdatedAt = DateTime.UtcNow;
        }
    }

    private static Guid? NormalizeOperatingUnitId(Guid? operatingUnitId) =>
        operatingUnitId is Guid id && id != Guid.Empty ? id : null;

    private async Task<Dictionary<Guid, decimal>> PostedAcceptedByPoItemAsync(Guid purchaseOrderId, Guid? excludeGoodsReceiptId, CancellationToken cancellationToken)
    {
        var rows = await db.GoodsReceiptLines.AsNoTracking()
            .Where(line => line.PurchaseOrderItem.PurchaseOrderId == purchaseOrderId
                && line.GoodsReceipt.Status == GoodsReceiptStatus.POSTED
                && (excludeGoodsReceiptId == null || line.GoodsReceiptId != excludeGoodsReceiptId))
            .GroupBy(line => line.PurchaseOrderItemId)
            .Select(group => new { ItemId = group.Key, Quantity = group.Sum(line => line.AcceptedQuantity) })
            .ToListAsync(cancellationToken);
        return rows.ToDictionary(row => row.ItemId, row => row.Quantity);
    }

    private static void ReconcilePurchaseOrderOpenQuantities(PurchaseOrder po, IReadOnlyDictionary<Guid, decimal> postedAccepted)
    {
        foreach (var item in po.Items)
        {
            var posted = postedAccepted.TryGetValue(item.Id, out var quantity) ? quantity : 0;
            item.ReceivedQuantity = posted;
            item.OpenQuantity = PurchaseOrderReceiving.RemainingQuantity(item, posted);
            if (item.OpenQuantity == 0 && item.Status == PurchaseOrderItemStatus.OPEN)
                item.Status = item.OrderedQuantity > 0 ? PurchaseOrderItemStatus.CLOSED : item.Status;
            else if (item.OpenQuantity > 0 && item.Status is PurchaseOrderItemStatus.CLOSED)
                item.Status = posted > 0 ? PurchaseOrderItemStatus.PARTIALLY_RECEIVED : PurchaseOrderItemStatus.OPEN;
            item.UpdatedAt = DateTime.UtcNow;
        }
        po.TotalReceivedQuantity = po.Items.Sum(item => item.ReceivedQuantity);
        po.Status = po.Items.All(item => item.Status == PurchaseOrderItemStatus.CLOSED || item.DeletionIndicator)
            ? PurchaseOrderStatus.CLOSED
            : po.TotalReceivedQuantity > 0 ? PurchaseOrderStatus.PARTIALLY_RECEIVED : PurchaseOrderStatus.OPEN;
        po.UpdatedAt = DateTime.UtcNow;
    }

    private static decimal CapToRemaining(decimal requested, decimal remaining) =>
        requested > remaining ? remaining : requested < 0 ? 0 : requested;

    private async Task EnsureScopeAsync(Session session, Guid organizationId, Guid? operatingUnitId, string permission, CancellationToken cancellationToken)
    {
        operatingUnitId = NormalizeOperatingUnitId(operatingUnitId);
        if (!await db.Organizations.AnyAsync(item => item.Id == organizationId && item.Status == StatusKind.ACTIVE, cancellationToken))
            throw new OperationalException("ACCESS_SCOPE_NOT_FOUND", "The organization was not found.");
        if (operatingUnitId is not null && !await db.OrganizationUnits.AnyAsync(item => item.Id == operatingUnitId && item.OrganizationId == organizationId && item.Status == StatusKind.ACTIVE, cancellationToken))
            throw new OperationalException("ACCESS_SCOPE_NOT_FOUND", "The operating unit was not found.");
        if (!await accessService.HasPermissionAsync(session, permission, organizationId, operatingUnitId, cancellationToken))
            throw new OperationalException("PERMISSION_DENIED", "You do not have permission to perform this action in this scope.");
    }

    private async Task<PurchaseOrder?> ResolveAuthoritativePurchaseOrderAsync(
        Guid organizationId,
        Guid supplierId,
        string? entityCode,
        string purchaseOrderNumber,
        CancellationToken cancellationToken)
    {
        var poNumber = purchaseOrderNumber.Trim();
        var candidates = await db.PurchaseOrders.Include(item => item.Items)
            .Where(item => item.OrganizationId == organizationId && item.PoNumber == poNumber)
            .ToListAsync(cancellationToken);
        var scoped = candidates
            .Where(item => PurchaseOrderReceiving.MatchesRequestedEntity(entityCode, item.EntityCode))
            .ToList();
        var po = scoped.FirstOrDefault(item => item.SupplierId == supplierId)
            ?? scoped.FirstOrDefault()
            ?? candidates.FirstOrDefault(item => item.SupplierId == supplierId)
            ?? candidates.FirstOrDefault();
        if (po is null)
            throw new OperationalException("PO_NOT_FOUND", "The selected purchase order was not found in this organization and entity.");
        if (po.SupplierId != supplierId)
            throw new OperationalException("PO_SUPPLIER_MISMATCH", "The selected purchase order does not belong to this supplier.");
        if (!PurchaseOrderReceiving.IsHeaderOpen(po))
            throw new OperationalException("PO_NOT_OPEN", "The selected purchase order is not open.");
        if (!po.Items.Any(PurchaseOrderReceiving.IsGoodsReceiptEligible))
            throw new OperationalException("PO_NOT_GR_ELIGIBLE", "The selected purchase order has no goods-receipt-eligible open items.");
        return po;
    }

    private static void AttachReviewedInvoiceLines(Invoice invoice, PurchaseOrder? po, string? invoiceLinesJson)
    {
        var drafts = ParseInvoiceLineDrafts(invoiceLinesJson);
        if (drafts.Count == 0) return;
        var used = new HashSet<Guid>();
        var lineNumber = 0;
        foreach (var draft in drafts)
        {
            lineNumber = draft.LineNumber > 0 ? draft.LineNumber : lineNumber + 10;
            var item = po is null ? null : MatchPurchaseOrderItem(po, draft, used);
            invoice.Lines.Add(new InvoiceLine
            {
                Id = Guid.NewGuid(),
                InvoiceId = invoice.Id,
                LineNumber = lineNumber,
                SupplierMaterialCode = draft.MaterialCode,
                DescriptionRaw = string.IsNullOrWhiteSpace(draft.Description) ? item?.Description ?? string.Empty : draft.Description,
                Quantity = draft.Quantity,
                Uom = draft.Uom ?? item?.Uom,
                UnitPrice = draft.UnitPrice ?? item?.UnitPrice,
                LineAmount = draft.LineAmount,
                MatchStatus = item is null ? InvoiceLineMatchStatus.UNMATCHED : InvoiceLineMatchStatus.MATCHED,
                PurchaseOrderItemId = item?.Id,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
            });
            if (item is not null) used.Add(item.Id);
        }
        if (invoice.PurchaseOrderId is not null && invoice.Lines.Count > 0 && invoice.Lines.All(line => line.MatchStatus == InvoiceLineMatchStatus.MATCHED))
            invoice.Status = InvoiceStatus.READY_FOR_GRN;
    }

    private static PurchaseOrderItem? MatchPurchaseOrderItem(PurchaseOrder po, InvoiceLineDraft draft, HashSet<Guid> used)
    {
        var eligible = po.Items.Where(PurchaseOrderReceiving.IsGoodsReceiptEligible).Where(item => !used.Contains(item.Id)).ToList();
        if (!string.IsNullOrWhiteSpace(draft.PoItemNumber))
        {
            var byItem = eligible.FirstOrDefault(item =>
                string.Equals(item.ItemNumber, draft.PoItemNumber, StringComparison.OrdinalIgnoreCase) ||
                item.LineNumber.ToString(CultureInfo.InvariantCulture) == draft.PoItemNumber.Trim());
            if (byItem is not null) return byItem;
        }
        if (draft.LineNumber > 0)
        {
            var byLine = eligible.FirstOrDefault(item =>
                item.LineNumber == draft.LineNumber ||
                string.Equals(item.ItemNumber, draft.LineNumber.ToString(CultureInfo.InvariantCulture), StringComparison.OrdinalIgnoreCase));
            if (byLine is not null) return byLine;
        }
        if (!string.IsNullOrWhiteSpace(draft.MaterialCode))
        {
            var code = draft.MaterialCode.Trim();
            var byMaterial = eligible.FirstOrDefault(item =>
                string.Equals(item.MaterialCode, code, StringComparison.OrdinalIgnoreCase));
            if (byMaterial is not null) return byMaterial;
            var bySupplierCode = eligible.FirstOrDefault(item =>
                item.Description.Contains(code, StringComparison.OrdinalIgnoreCase));
            if (bySupplierCode is not null) return bySupplierCode;
        }
        if (!string.IsNullOrWhiteSpace(draft.Description))
        {
            var normalized = Normalize(draft.Description);
            return eligible.FirstOrDefault(item => Normalize(item.Description) == normalized);
        }
        return null;
    }

    private static List<InvoiceLineDraft> ParseInvoiceLineDrafts(string? json)
    {
        if (string.IsNullOrWhiteSpace(json)) return [];
        try
        {
            return JsonSerializer.Deserialize<List<InvoiceLineDraft>>(json, new JsonSerializerOptions { PropertyNameCaseInsensitive = true }) ?? [];
        }
        catch (JsonException)
        {
            return [];
        }
    }

    private static void ApplyErpFailure(GoodsReceipt grn, ErpGoodsReceiptResult erp)
    {
        if (erp.ErrorCode == "POST_GRN_CONFIGURATION_NOT_FOUND" || erp.ErrorCode == "INTEGRATION_ROUTE_NOT_FOUND")
        {
            grn.Status = GoodsReceiptStatus.READY_TO_POST;
            grn.BusinessStatus = "READY_TO_POST";
            grn.ErpPostingStatus = "NOT_POSTED";
        }
        else if (erp.Unknown)
        {
            grn.Status = GoodsReceiptStatus.UNKNOWN;
            grn.BusinessStatus = "ERP_RECONCILIATION_REQUIRED";
            grn.ErpPostingStatus = "POSTING_UNKNOWN";
        }
        else
        {
            grn.Status = GoodsReceiptStatus.FAILED;
            grn.BusinessStatus = "ERP_POST_FAILED";
            grn.ErpPostingStatus = "FAILED";
        }
        grn.FailureCode = erp.ErrorCode;
        grn.FailureMessage = erp.ErrorMessage;
    }

    private static void ApplyErpResult(GoodsReceipt grn, ErpGoodsReceiptResult erp)
    {
        grn.ErpResponseJson = erp.ResponseJson;
        if (!string.IsNullOrWhiteSpace(erp.MaterialDocument)) grn.ErpMaterialDocument = erp.MaterialDocument;
        grn.ErpDocumentYear = erp.DocumentYear;
        grn.ExternalReference = erp.ExternalReference;
        grn.IntegrationRouteId = erp.IntegrationRouteId;
        grn.IntegrationConfigurationId = erp.IntegrationConfigurationId;
        grn.ExternalSystem = erp.ExternalSystem;
        if (!string.IsNullOrWhiteSpace(erp.ExternalSystem)) grn.PostingProvider = erp.ExternalSystem;
    }

    private void ApplyPostedInventory(GoodsReceipt grn, PurchaseOrder po, Session session)
    {
        foreach (var line in grn.Lines)
        {
            var item = po.Items.Single(candidate => candidate.Id == line.PurchaseOrderItemId);
            item.ReceivedQuantity += line.AcceptedQuantity;
            item.OpenQuantity = item.OrderedQuantity - item.ReceivedQuantity;
            if (item.OpenQuantity < 0) item.OpenQuantity = 0;
            item.Status = item.OpenQuantity == 0 ? PurchaseOrderItemStatus.CLOSED : PurchaseOrderItemStatus.PARTIALLY_RECEIVED;
            item.UpdatedAt = DateTime.UtcNow;
            if (line.AcceptedQuantity <= 0) continue;
            if (grn.OperatingUnitId is not Guid unitId) continue;
            var stock = db.StockBalances.Local.FirstOrDefault(balance =>
                balance.OrganizationId == grn.OrganizationId && balance.OperatingUnitId == unitId &&
                balance.MaterialCode == item.MaterialCode && balance.Uom == item.Uom)
                ?? db.StockBalances.FirstOrDefault(balance =>
                    balance.OrganizationId == grn.OrganizationId && balance.OperatingUnitId == unitId &&
                    balance.MaterialCode == item.MaterialCode && balance.Uom == item.Uom);
            if (stock is null)
            {
                stock = new StockBalance
                {
                    Id = Guid.NewGuid(), OrganizationId = grn.OrganizationId, OperatingUnitId = unitId,
                    MaterialId = item.MaterialId, MaterialCode = item.MaterialCode, Uom = item.Uom, Quantity = 0, UpdatedAt = DateTime.UtcNow,
                };
                db.StockBalances.Add(stock);
            }
            stock.Quantity += line.AcceptedQuantity;
            stock.UpdatedAt = DateTime.UtcNow;
            db.InventoryTransactions.Add(new InventoryTransaction
            {
                Id = Guid.NewGuid(), OrganizationId = grn.OrganizationId, OperatingUnitId = unitId,
                MaterialId = item.MaterialId, MaterialCode = item.MaterialCode, TransactionType = InventoryTransactionType.GRN_RECEIPT,
                ReferenceType = "GOODS_RECEIPT", ReferenceId = grn.Id, Quantity = line.AcceptedQuantity, Uom = item.Uom,
                CreatedByUserId = session.CustomerUserId(), CreatedAt = DateTime.UtcNow,
            });
        }
        po.TotalReceivedQuantity = po.Items.Sum(item => item.ReceivedQuantity);
        po.Status = po.Items.All(item => item.Status == PurchaseOrderItemStatus.CLOSED) ? PurchaseOrderStatus.CLOSED : PurchaseOrderStatus.PARTIALLY_RECEIVED;
        po.UpdatedAt = DateTime.UtcNow;
        grn.Status = GoodsReceiptStatus.POSTED;
        grn.BusinessStatus = "POSTED";
        grn.ErpPostingStatus = "POSTED";
        grn.PostedAt = DateTime.UtcNow;
        grn.ErpPostedAt = DateTime.UtcNow;
        if (grn.Invoice is not null)
        {
            grn.Invoice.Status = InvoiceStatus.GRN_POSTED;
            grn.Invoice.UpdatedAt = DateTime.UtcNow;
        }
    }

    private async Task AssignErpReceiptNumberAsync(GoodsReceipt grn, CancellationToken cancellationToken)
    {
        if (!string.IsNullOrWhiteSpace(grn.ErpMaterialDocument)) return;
        if (ParseSrcRunningNumber(grn.GrnNumber) > 0)
        {
            grn.ErpMaterialDocument = grn.GrnNumber;
            return;
        }
        grn.ErpMaterialDocument = await NextSrcGrnNumberAsync(grn.OrganizationId, cancellationToken);
    }

    private async Task<string> NextSrcGrnNumberAsync(Guid organizationId, CancellationToken cancellationToken)
    {
        var existing = await db.GoodsReceipts.AsNoTracking()
            .Where(item => item.OrganizationId == organizationId)
            .Select(item => new { item.GrnNumber, item.ErpMaterialDocument })
            .ToListAsync(cancellationToken);
        var max = 0;
        foreach (var row in existing)
        {
            max = Math.Max(max, ParseSrcRunningNumber(row.GrnNumber));
            max = Math.Max(max, ParseSrcRunningNumber(row.ErpMaterialDocument));
        }
        return $"SRC{(max + 1).ToString("D4", CultureInfo.InvariantCulture)}";
    }

    private static int ParseSrcRunningNumber(string? value)
    {
        if (string.IsNullOrWhiteSpace(value) || !value.StartsWith("SRC", StringComparison.OrdinalIgnoreCase)) return 0;
        return int.TryParse(value[3..], NumberStyles.Integer, CultureInfo.InvariantCulture, out var number) && number > 0
            ? number
            : 0;
    }

    private static string Normalize(string value) => Regex.Replace(value.Trim().ToUpperInvariant(), @"[^A-Z0-9]+", " ").Trim();
    private static AuditEvent Audit(Session session, Guid organizationId, Guid? operatingUnitId, string eventType, string entityType, Guid entityId, string reference) =>
        new()
        {
            Id = Guid.NewGuid(),
            OrganizationId = organizationId,
            OperatingUnitId = operatingUnitId,
            UserId = session.UserId,
            EventType = eventType,
            EntityType = entityType,
            EntityId = entityId,
            Reference = reference,
            MetadataJson = session.IsSupportSession
                ? $"{{\"actorType\":\"PLATFORM_USER\",\"platformUserId\":\"{session.PlatformUserId:D}\",\"platformRole\":\"{session.PlatformRole}\",\"delegatedPermissions\":\"{session.DelegatedPermissionsCsv}\"}}"
                : $"{{\"actorType\":\"{session.ActorType}\"}}",
            CreatedAt = DateTime.UtcNow,
        };
}