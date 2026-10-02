using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using Microsoft.EntityFrameworkCore;
using SilaMe.Api.Data;
using SilaMe.Api.DTOs;
using SilaMe.Api.Models;

namespace SilaMe.Api.Services;

public sealed record AdvancedOcrRun(
    AdvancedInvoiceExtractionResponse Response,
    string RawText,
    string ContentHash,
    string ConfigurationSnapshotJson,
    InvoiceOcrConfiguration Configuration);

/// <summary>
/// PROTECTED SILA INVOICE RECEIVING FLOW — See docs/PROTECTED_INVOICE_FLOW.md.
/// Backend Advanced OCR. Re-read must not create a new invoice/document.
/// </summary>
public sealed class AdvancedInvoiceOcrService(
    SilaMeDbContext db,
    IPdfTextExtractor pdfTextExtractor,
    IExtractionProviderResolver providerResolver,
    IInvoiceExtractionService invoiceExtractor,
    ILogger<AdvancedInvoiceOcrService> logger)
{
    public async Task<InvoiceOcrConfiguration> GetConfigurationAsync(Guid organizationId, CancellationToken cancellationToken)
    {
        var configuration = await db.InvoiceOcrConfigurations.AsNoTracking()
            .SingleOrDefaultAsync(item => item.OrganizationId == organizationId, cancellationToken);
        return configuration ?? DefaultConfiguration(organizationId);
    }

    public async Task<bool> IsMobileFallbackRequiredAsync(
        Guid organizationId,
        string? supplierName,
        string? invoiceNumber,
        string? purchaseOrderNumber,
        decimal? invoiceAmount,
        decimal? confidence,
        CancellationToken cancellationToken)
    {
        var configuration = await GetConfigurationAsync(organizationId, cancellationToken);
        var reason = "REQUIRED_FIELDS_OR_CONFIDENCE";
        if (!configuration.MobileBasicOcrEnabled || !configuration.AutomaticBackendFallbackEnabled)
        {
            reason = !configuration.MobileBasicOcrEnabled ? "MOBILE_BASIC_DISABLED" : "AUTOMATIC_FALLBACK_DISABLED";
            logger.LogInformation("[ADV-OCR] Organization={OrganizationId} Decision=SKIP Reason={Reason} ConfigVersion={Version}",
                organizationId, reason, configuration.Version);
            return false;
        }

        var threshold = confidence ?? 0m;
        var required = configuration.RequireSupplierName && string.IsNullOrWhiteSpace(supplierName)
            || configuration.RequireInvoiceNumber && string.IsNullOrWhiteSpace(invoiceNumber)
            || configuration.RequirePurchaseOrderNumber && string.IsNullOrWhiteSpace(purchaseOrderNumber)
            || configuration.RequireInvoiceAmount && invoiceAmount is null
            || threshold < configuration.MinimumMobileConfidence;
        logger.LogInformation("[ADV-OCR] Organization={OrganizationId} Decision={Decision} Reason={Reason} Confidence={Confidence} Threshold={Threshold} ConfigVersion={Version}",
            organizationId, required ? "FALLBACK" : "SKIP", required ? reason : "MOBILE_RESULT_SUFFICIENT",
            threshold, configuration.MinimumMobileConfidence, configuration.Version);
        return required;
    }

    public AdvancedInvoiceExtractionResponse CreateSkippedResponse(Document document, ExtractionTrigger trigger, InvoiceOcrConfiguration configuration) =>
        new(
            document.Id,
            "BUILT_IN_ADVANCED",
            "SKIPPED",
            trigger.ToString(),
            null,
            EmptyHeader(),
            [],
            new AdvancedInvoiceValidationResponse(false, false, false, null, false, false),
            false,
            false,
            null,
            DateTime.UtcNow,
            document.PageCount ?? 1,
            "Organization OCR policy did not require backend fallback.")
        {
            OcrRequestId = document.OcrRequestId,
            EffectiveConfiguration = Snapshot(configuration),
        };

    public async Task<AdvancedOcrRun> ExtractAsync(
        Document document,
        Stream content,
        ExtractionTrigger trigger,
        CancellationToken cancellationToken)
    {
        var started = DateTime.UtcNow;
        var correlationId = document.OcrRequestId ?? $"ocr-{Guid.NewGuid():N}";
        document.OcrRequestId ??= correlationId;
        logger.LogInformation("[ADV-OCR] Request={RequestId} Event=REQUEST_RECEIVED Document={DocumentId} Trigger={Trigger} ContentType={ContentType} Bytes={Bytes}",
            correlationId, document.Id, trigger, document.ContentType, content.Length);
        using var memory = new MemoryStream();
        await content.CopyToAsync(memory, cancellationToken);
        var bytes = memory.ToArray();
        var contentHash = Convert.ToHexString(SHA256.HashData(bytes));
        var configuration = await GetConfigurationAsync(document.OrganizationId, cancellationToken);
        if (configuration.ReuseCachedOcr)
        {
            var cached = await db.DocumentExtractions.AsNoTracking()
                .Where(item => item.ContentHash == contentHash && item.StructuredPayloadJson != null)
                .OrderByDescending(item => item.CreatedAt)
                .FirstOrDefaultAsync(cancellationToken);
            if (cached?.StructuredPayloadJson is { Length: > 0 } payload)
            {
                try
                {
                    var cachedResponse = JsonSerializer.Deserialize<AdvancedInvoiceExtractionResponse>(payload);
                    if (cachedResponse is not null)
                    {
                        logger.LogInformation("[OCR] DocumentId={DocumentId} Event=CACHE_HIT Hash={Hash} Engine={Engine}",
                            document.Id, contentHash, cached.Provider);
                        return new AdvancedOcrRun(
                            cachedResponse with { DocumentId = document.Id, OcrRequestId = correlationId },
                            cached.RawText ?? string.Empty,
                            contentHash,
                            cached.ConfigurationSnapshotJson ?? JsonSerializer.Serialize(configuration),
                            configuration);
                    }
                }
                catch (JsonException)
                {
                    // Fall through to a fresh extraction when a cached payload cannot be read.
                }
            }
        }
        logger.LogInformation("[ADV-OCR] Request={RequestId} Event=CONFIGURATION_LOADED Organization={OrganizationId} Version={Version} Provider={Provider} Fallback={Fallback} LineExtraction={LineExtraction} Reconciliation={Reconciliation}",
            correlationId, document.OrganizationId, configuration.Version, configuration.BackendProvider,
            configuration.AutomaticBackendFallbackEnabled, configuration.DetailedLineExtractionEnabled,
            configuration.FinancialReconciliationEnabled);

        string? text = null;
        var method = "EMBEDDED_TEXT";
        logger.LogInformation("[ADV-OCR] Request={RequestId} Event=PDF_INSPECTED IsPdf={IsPdf} DeclaredPages={Pages}",
            correlationId, document.ContentType.Contains("pdf", StringComparison.OrdinalIgnoreCase), document.PageCount ?? 1);
        if (document.ContentType.Contains("pdf", StringComparison.OrdinalIgnoreCase))
        {
            await using var embeddedContent = new MemoryStream(bytes, writable: false);
            text = await pdfTextExtractor.ExtractAsync(embeddedContent, cancellationToken);
            logger.LogInformation("[ADV-OCR] Request={RequestId} Event=EMBEDDED_TEXT_RESULT Available={Available} Characters={Characters}",
                correlationId, !string.IsNullOrWhiteSpace(text), text?.Length ?? 0);
        }

        if (string.IsNullOrWhiteSpace(text))
        {
            method = "BUILT_IN_OCR";
            logger.LogInformation("[ADV-OCR] Request={RequestId} Event=OCR_STARTED Provider=BUILT_IN_OCR", correlationId);
            var resolved = await providerResolver.ResolveAsync(document.OrganizationId, document.DocumentType, cancellationToken);
            await using var ocrContent = new MemoryStream(bytes, writable: false);
            var ocr = await resolved.Provider.ExtractAsync(document, ocrContent, cancellationToken);
            text = ocr.Text;
            logger.LogInformation("[ADV-OCR] Request={RequestId} Event=OCR_RESULT Characters={Characters} Provider={Provider}",
                correlationId, text?.Length ?? 0, resolved.Provider.GetType().Name);
        }

        text = NormalizeText(text);
        var response = await BuildResponseAsync(document, text, trigger, method, configuration, started, cancellationToken);
        logger.LogInformation(
            "[ADV-OCR] Request={RequestId} Event=RESPONSE_READY Document={DocumentId} Trigger={Trigger} Pages={Pages} EmbeddedText={EmbeddedText} HeaderFields={HeaderFields} Lines={Lines} Confidence={Confidence} Status={Status} RequiresReview={RequiresReview}",
            correlationId,
            document.Id,
            trigger,
            response.PageCount,
            method == "EMBEDDED_TEXT" ? "YES" : "NO",
            CountHeaderFields(response.Header),
            response.Lines.Count,
            response.Confidence,
            response.RequiresReview,
            response.Status);

        return new AdvancedOcrRun(
            response,
            text,
            contentHash,
            JsonSerializer.Serialize(configuration),
            configuration);
    }

    private async Task<AdvancedInvoiceExtractionResponse> BuildResponseAsync(
        Document document,
        string text,
        ExtractionTrigger trigger,
        string method,
        InvoiceOcrConfiguration configuration,
        DateTime started,
        CancellationToken cancellationToken)
    {
        var extracted = string.IsNullOrWhiteSpace(text) ? null : invoiceExtractor.Extract(text);
        if (extracted is null)
        {
            return new AdvancedInvoiceExtractionResponse(
                document.Id, "BUILT_IN_ADVANCED", "FAILED", trigger.ToString(), null,
                EmptyHeader(), [], new AdvancedInvoiceValidationResponse(false, false, false, null, false, false),
                true, false, started, DateTime.UtcNow, document.PageCount ?? 1,
                "The invoice document did not contain readable text.")
            {
                OcrRequestId = document.OcrRequestId,
                EffectiveConfiguration = Snapshot(configuration),
            };
        }

        var supplierNameValue = InvoiceOcrFieldReader.NormalizeSupplierCandidate(
            extracted.SupplierName ?? InvoiceOcrFieldReader.ReadSupplierName(text));
        var supplierName = Field(supplierNameValue, "Supplier", text);
        var supplierLegalName = Field(LabelValue(text, "Supplier Legal Name", "Legal Name"), "Supplier Legal Name", text);
        var supplierTrn = Field(extracted.SupplierTaxNumber ?? InvoiceOcrFieldReader.ReadSupplierTrn(text), "TRN", text);
        var supplierAddress = Field(LabelValue(text, "Supplier Address", "Vendor Address", "Address"), "Supplier Address", text);
        var supplierEmail = Field(LabelValue(text, "Supplier Email", "Email"), "Supplier Email", text);
        var supplierPhone = Field(LabelValue(text, "Supplier Phone", "Phone", "Telephone"), "Supplier Phone", text);
        var invoiceNumber = Field(extracted.InvoiceNumber ?? InvoiceOcrFieldReader.ReadInvoiceNumber(text), "Invoice Number", text);
        var invoiceDate = Field(extracted.InvoiceDate ?? InvoiceOcrFieldReader.ReadInvoiceDate(text), "Invoice Date", text);
        var invoiceTypeValue = LabelValue(text, "Invoice Type", "Document Type");
        if (string.IsNullOrWhiteSpace(invoiceTypeValue))
            invoiceTypeValue = extracted.Lines.Count == 0 ? InvoiceType.SERVICE.ToString() : InvoiceType.MATERIAL.ToString();
        var invoiceType = Field(invoiceTypeValue, "Invoice Type", text);
        var poNumber = Field(extracted.PoNumber ?? InvoiceOcrFieldReader.ReadPurchaseOrderNumber(text), "PO Number", text);
        var currency = Field(extracted.Currency, "Currency", text);
        var netAmount = Field(extracted.NetAmount, "Net Amount", text);
        var discount = Field(ParseDecimal(LabelValue(text, "Discount Amount", "Discount")), "Discount Amount", text);
        var freight = Field(ParseDecimal(LabelValue(text, "Freight", "Delivery Charge", "Freight Charge")), "Freight", text);
        var otherCharges = Field(ParseDecimal(LabelValue(text, "Other Charges", "Additional Charges")), "Other Charges", text);
        var taxableAmount = Field(ParseDecimal(LabelValue(text, "Taxable Amount", "Taxable")), "Taxable Amount", text);
        var taxAmount = Field(extracted.TaxAmount, "Tax Amount", text);
        var grossAmount = Field(extracted.GrossAmount ?? InvoiceOcrFieldReader.ReadGrossAmount(text), "Grand Total", text);
        var amountDue = Field(ParseDecimal(LabelValue(text, "Amount Due", "Net Payable", "Total Payable")), "Amount Due", text);
        var paymentTerms = Field(LabelValue(text, "Payment Terms", "Terms"), "Payment Terms", text);
        var dueDate = Field(ParseDate(LabelValue(text, "Due Date", "Payment Due Date")), "Due Date", text);

        var lines = configuration.DetailedLineExtractionEnabled
            ? extracted.Lines.Select(line => new AdvancedInvoiceLineResponse(
                line.LineNumber,
                Field(line.SupplierMaterialCode, "Item Code", text),
                Field(line.Description, "Description", text),
                Field<string>(null, "PO Item", text),
                Field(line.Quantity, "Quantity", text),
                Field(line.Uom, "UOM", text),
                Field(line.UnitPrice, "Unit Price", text),
                Field<decimal?>(null, "Discount", text),
                Field(line.LineAmount, "Line Amount", text),
                Field(line.TaxRate, "Tax Rate", text),
                Field(line.TaxAmount, "Tax Amount", text),
                Field(line.LineAmount, "Line Total", text),
                Field<string>(null, "Batch Number", text),
                Field<DateOnly?>(null, "Expiry Date", text))).ToList()
            : [];

        var amounts = Reconcile(netAmount.Value, taxAmount.Value, grossAmount.Value, lines, configuration.AmountTolerance);
        var supplierCode = InvoiceOcrFieldReader.ReadSupplierId(text);
        var supplierMatched = (!string.IsNullOrWhiteSpace(supplierName.Value) || !string.IsNullOrWhiteSpace(supplierCode)) &&
            await db.Suppliers.AsNoTracking().AnyAsync(item =>
                item.OrganizationId == document.OrganizationId &&
                (item.NormalizedName == NormalizeKey(supplierName.Value) ||
                 item.TaxNumber == supplierTrn.Value ||
                 item.Trn == supplierTrn.Value ||
                 (!string.IsNullOrWhiteSpace(supplierCode) && item.SupplierCode == supplierCode)), cancellationToken);
        var purchaseOrderMatched = !string.IsNullOrWhiteSpace(poNumber.Value) &&
            await db.PurchaseOrders.AsNoTracking().AnyAsync(item =>
                item.OrganizationId == document.OrganizationId && item.PoNumber == poNumber.Value, cancellationToken);

        var header = new AdvancedInvoiceHeaderResponse(
            supplierName, supplierLegalName, supplierTrn, supplierAddress, supplierEmail, supplierPhone,
            invoiceNumber, invoiceDate, invoiceType, poNumber, currency, netAmount, discount, freight,
            otherCharges, taxableAmount, taxAmount, grossAmount, amountDue, paymentTerms, dueDate);

        var requiredMissing =
            configuration.RequireSupplierName && string.IsNullOrWhiteSpace(supplierName.Value)
            || configuration.RequireInvoiceNumber && string.IsNullOrWhiteSpace(invoiceNumber.Value)
            || configuration.RequirePurchaseOrderNumber && string.IsNullOrWhiteSpace(poNumber.Value)
            || configuration.RequireInvoiceAmount && grossAmount.Value is null
            || configuration.RequireInvoiceDate && invoiceDate.Value is null
            || configuration.RequireCurrency && string.IsNullOrWhiteSpace(currency.Value)
            || configuration.RequireSupplierTrn && string.IsNullOrWhiteSpace(supplierTrn.Value);
        var tier1Count = new[]
        {
            !string.IsNullOrWhiteSpace(supplierName.Value),
            !string.IsNullOrWhiteSpace(invoiceNumber.Value),
            !string.IsNullOrWhiteSpace(poNumber.Value),
            grossAmount.Value.HasValue,
        }.Count(found => found);
        var reconFailed = configuration.FinancialReconciliationEnabled && !amounts.AmountsReconciled;
        var status = tier1Count == 0
            ? "FAILED"
            : reconFailed || requiredMissing || tier1Count <= 2
                ? "REVIEW_REQUIRED"
                : tier1Count == 3 ? "PARTIAL" : "SUCCESS";
        var requiresReview = status is "REVIEW_REQUIRED" or "FAILED" or "PARTIAL";
        var confidence = Math.Round(tier1Count / 4m, 4);
        if (lines.Count == 0 && configuration.DetailedLineExtractionEnabled && Regex.IsMatch(text, @"(?im)\b(item|qty|quantity|description)\b"))
        {
            status = "REVIEW_REQUIRED";
            requiresReview = true;
        }
        logger.LogInformation("[OCR] DocumentId={DocumentId} DocumentType={DocumentType} PageCount={PageCount} EmbeddedTextLength={EmbeddedTextLength} Engine={Engine} Tier1Found={Tier1} HeaderConfidence={HeaderConfidence} LinesExtracted={Lines} ReconciliationStatus={Reconciliation} TotalDurationMs={Duration} Status={Status}",
            document.Id, document.ContentType, document.PageCount ?? 1, text.Length, method, tier1Count, confidence, lines.Count, amounts.AmountsReconciled, (long)(DateTime.UtcNow - started).TotalMilliseconds, status);
        logger.LogInformation("[ADV-OCR] Request={RequestId} Event=HEADER_EXTRACTED Present={Present} Supplier={Supplier} InvoiceNumber={InvoiceNumber} InvoiceDate={InvoiceDate} PurchaseOrder={PurchaseOrder} GrossAmount={GrossAmount} Currency={Currency} RequiredMissing={RequiredMissing} Confidence={Confidence}",
            document.OcrRequestId, CountHeaderFields(header),
            !string.IsNullOrWhiteSpace(header.SupplierName.Value),
            !string.IsNullOrWhiteSpace(header.InvoiceNumber.Value),
            header.InvoiceDate.Value.HasValue,
            !string.IsNullOrWhiteSpace(header.PurchaseOrderNumber.Value),
            header.GrossAmount.Value.HasValue,
            !string.IsNullOrWhiteSpace(header.Currency.Value),
            requiredMissing, confidence);
        logger.LogInformation("[ADV-OCR] Request={RequestId} Event=LINE_EXTRACTION Lines={Lines} Enabled={Enabled} AmountsReconciled={AmountsReconciled} Difference={Difference}",
            document.OcrRequestId, lines.Count, configuration.DetailedLineExtractionEnabled, amounts.AmountsReconciled, amounts.Difference);
        var completed = DateTime.UtcNow;
        return new AdvancedInvoiceExtractionResponse(
            document.Id,
            "BUILT_IN_ADVANCED",
            status,
            trigger.ToString(),
            confidence,
            header,
            lines,
            new AdvancedInvoiceValidationResponse(
                amounts.AmountsReconciled,
                amounts.LineTotalMatchesNet,
                amounts.TaxReconciled,
                amounts.Difference,
                supplierMatched,
                purchaseOrderMatched),
            requiresReview,
            false,
            started,
            completed,
            document.PageCount ?? Math.Max(1, text.Split("\f", StringSplitOptions.RemoveEmptyEntries).Length),
            status == "REVIEW_REQUIRED" && lines.Count == 0 && configuration.DetailedLineExtractionEnabled ? "LINE_EXTRACTION_FAILED" : null)
        {
            OcrRequestId = document.OcrRequestId,
            RawText = text,
            EffectiveConfiguration = Snapshot(configuration),
        };
    }

    private static AdvancedOcrConfigurationSnapshot Snapshot(InvoiceOcrConfiguration configuration) =>
        new(
            configuration.MobileBasicOcrEnabled,
            configuration.AutomaticBackendFallbackEnabled,
            configuration.MinimumMobileConfidence,
            configuration.RequireSupplierName,
            configuration.RequireInvoiceNumber,
            configuration.RequirePurchaseOrderNumber,
            configuration.RequireInvoiceAmount,
            configuration.RequireInvoiceDate,
            configuration.RequireCurrency,
            configuration.RequireSupplierTrn,
            configuration.AlwaysBackendOnReread,
            configuration.DetailedLineExtractionEnabled,
            configuration.SupplierMasterValidationEnabled,
            configuration.PurchaseOrderValidationEnabled,
            configuration.FinancialReconciliationEnabled,
            configuration.Version);

    private static (bool AmountsReconciled, bool LineTotalMatchesNet, bool TaxReconciled, decimal? Difference) Reconcile(
        decimal? net,
        decimal? tax,
        decimal? gross,
        IReadOnlyList<AdvancedInvoiceLineResponse> lines,
        decimal tolerance)
    {
        var amountsReconciled = net.HasValue && tax.HasValue && gross.HasValue
            ? Math.Abs(net.Value + tax.Value - gross.Value) <= tolerance
            : false;
        var lineTotal = lines.Where(line => line.GrossAmount.Value.HasValue).Sum(line => line.GrossAmount.Value!.Value);
        var lineTotalMatchesNet = net.HasValue && lines.Count > 0
            ? Math.Abs(lineTotal - net.Value) <= tolerance
            : false;
        var taxReconciled = lines.Count > 0 && tax.HasValue && lines.All(line => line.TaxAmount.Value.HasValue)
            ? Math.Abs(lines.Sum(line => line.TaxAmount.Value!.Value) - tax.Value) <= tolerance
            : false;
        decimal? difference = net.HasValue && tax.HasValue && gross.HasValue
            ? gross.Value - (net.Value + tax.Value)
            : null;
        return (amountsReconciled, lineTotalMatchesNet, taxReconciled, difference);
    }

    private static AdvancedInvoiceHeaderResponse EmptyHeader() =>
        new(
            EmptyField<string>(), EmptyField<string>(), EmptyField<string>(), EmptyField<string>(), EmptyField<string>(),
            EmptyField<string>(), EmptyField<string>(), EmptyField<DateOnly?>(), EmptyField<string>(), EmptyField<string>(),
            EmptyField<string>(), EmptyField<decimal?>(), EmptyField<decimal?>(), EmptyField<decimal?>(), EmptyField<decimal?>(),
            EmptyField<decimal?>(), EmptyField<decimal?>(), EmptyField<decimal?>(), EmptyField<decimal?>(), EmptyField<string>(),
            EmptyField<DateOnly?>());

    private static AdvancedExtractedField<T> Field<T>(T? value, string label, string text)
    {
        var found = value is not null && (value is not string stringValue || !string.IsNullOrWhiteSpace(stringValue));
        return new AdvancedExtractedField<T>(
            value,
            found ? 0.86m : null,
            found ? "MEDIUM" : "NOT_FOUND",
            found ? PageFor(text, label) : null,
            found ? label : null,
            found ? "LABEL_AND_LAYOUT" : null);
    }

    private static AdvancedExtractedField<T> EmptyField<T>() => new(default, null, "NOT_FOUND", null, null, null);

    private static int PageFor(string text, string label)
    {
        var index = text.IndexOf(label, StringComparison.OrdinalIgnoreCase);
        return index < 0 ? 1 : text[..index].Count(character => character == '\f') + 1;
    }

    private static string NormalizeText(string? value) =>
        InvoiceOcrFieldReader.NormalizeOcrText(value ?? string.Empty);

    private static DateOnly? ParseDate(string? value) => InvoiceOcrFieldReader.ParseDate(value);

    private static string? LabelValue(string text, params string[] labels)
    {
        var pattern = string.Join("|", labels.OrderByDescending(item => item.Length).Select(Regex.Escape));
        var match = Regex.Match(text, $@"(?im)^\s*(?:{pattern})\s*(?:[:#=\-]\s*|\s+)(?<value>[^\r\n|]+)");
        return match.Success ? match.Groups["value"].Value.Trim() : null;
    }

    private static decimal? ParseDecimal(string? value) =>
        decimal.TryParse(value?.Replace(",", string.Empty), NumberStyles.Any, CultureInfo.InvariantCulture, out var parsed) ? parsed : null;

    private static string NormalizeKey(string? value) =>
        Regex.Replace(value ?? string.Empty, @"[^A-Za-z0-9]", string.Empty).ToUpperInvariant();

    private static int CountHeaderFields(AdvancedInvoiceHeaderResponse header) =>
        new object?[]
        {
            header.SupplierName.Value, header.InvoiceNumber.Value, header.InvoiceDate.Value, header.PurchaseOrderNumber.Value,
            header.Currency.Value, header.NetAmount.Value, header.TaxAmount.Value, header.GrossAmount.Value,
        }.Count(value => value is not null && (value is not string text || !string.IsNullOrWhiteSpace(text)));

    private static decimal? HeaderConfidence(
        AdvancedInvoiceHeaderResponse header,
        IReadOnlyList<AdvancedInvoiceLineResponse> lines)
    {
        var values = new decimal?[]
        {
            header.SupplierName.Confidence, header.InvoiceNumber.Confidence, header.InvoiceDate.Confidence,
            header.PurchaseOrderNumber.Confidence, header.Currency.Confidence, header.NetAmount.Confidence,
            header.TaxAmount.Confidence, header.GrossAmount.Confidence,
        }.Where(value => value.HasValue).Select(value => value!.Value).ToList();
        values.AddRange(lines.SelectMany(line => new[] { line.Description.Confidence, line.Quantity.Confidence, line.GrossAmount.Confidence }.Where(value => value.HasValue).Select(value => value!.Value)));
        return values.Count == 0 ? null : Math.Round(values.Average(), 4);
    }

    private static InvoiceOcrConfiguration DefaultConfiguration(Guid organizationId) =>
        new()
        {
            Id = Guid.Empty,
            OrganizationId = organizationId,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
            BackendProvider = "BUILT_IN_ADVANCED",
        };
}