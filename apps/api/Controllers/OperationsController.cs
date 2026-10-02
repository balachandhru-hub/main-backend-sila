using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;
using SilaMe.Api.Auth;
using SilaMe.Api.Data;
using SilaMe.Api.DTOs;
using SilaMe.Api.Models;
using SilaMe.Api.Services;

namespace SilaMe.Api.Controllers;

// PROTECTED SILA INVOICE RECEIVING FLOW — See docs/PROTECTED_INVOICE_FLOW.md.
[ApiController]
[Route("api/v1")]
public sealed class OperationsController(
    SilaMeDbContext db,
    OperationalService operations,
    IDocumentStorageService storage,
    DocumentRoutingService documentRoutingService) : ControllerBase
{
    // Invoice Review Save and Continue. Do not post GRN from this action.
    [HttpPost("documents/invoices")]
    [RequestSizeLimit(20 * 1024 * 1024)]
    public async Task<IActionResult> UploadInvoice(
        [FromForm] Guid organizationId,
        [FromForm] Guid? operatingUnitId,
        [FromForm] DocumentSourceChannel sourceChannel,
        [FromForm] int? pageCount,
        [FromForm] string? scanSessionId,
        [FromForm] string? supplierName,
        [FromForm] Guid? supplierId,
        [FromForm] string? supplierTrn,
        [FromForm] string? supplierInvoiceNumber,
        [FromForm] DateOnly? invoiceDate,
        [FromForm] string? purchaseOrderNumber,
        [FromForm] bool noPurchaseOrder,
        [FromForm] decimal? invoiceGross,
        [FromForm] string? currency,
        [FromForm] string? ocrRequestId,
        [FromForm] string? manualEditedFieldsJson,
        [FromForm] bool deferFullExtraction,
        [FromForm] string? invoiceLinesJson,
        [FromHeader(Name = "Idempotency-Key")] string? idempotencyKey,
        IFormFile file,
        CancellationToken cancellationToken)
    {
        var session = CurrentSession();
        if (session is null) return Unauthorized(new ApiError("SESSION_INVALID", "Your session is no longer valid."));
        try
        {
            return Ok(await operations.UploadInvoiceAsync(session, organizationId, operatingUnitId, sourceChannel, file, cancellationToken,
                pageCount, scanSessionId, supplierName, supplierId, supplierTrn, supplierInvoiceNumber, invoiceDate, purchaseOrderNumber,
                invoiceGross, currency, ocrRequestId, manualEditedFieldsJson, deferFullExtraction, noPurchaseOrder, idempotencyKey, invoiceLinesJson));
        }
        catch (OperationalException exception) { return Error(exception); }
    }

    // Invoice OCR (basic). Must not create a duplicate invoice/document on Re-read.
    [HttpPost("documents/basic-extract")]
    [RequestSizeLimit(20 * 1024 * 1024)]
    public async Task<IActionResult> ExtractBasicInvoice(
        [FromForm] Guid organizationId,
        [FromForm] Guid? operatingUnitId,
        [FromForm] int? pageCount,
        [FromForm] string? ocrRequestId,
        IFormFile file,
        CancellationToken cancellationToken)
    {
        var session = CurrentSession();
        if (session is null) return Unauthorized(new ApiError("SESSION_INVALID", "Your session is no longer valid."));
        try
        {
            return Ok(await operations.ExtractBasicInvoiceAsync(session, organizationId, operatingUnitId, pageCount, ocrRequestId, file, cancellationToken));
        }
        catch (OperationalException exception) { return Error(exception); }
    }

    // Invoice OCR (advanced). Internal extractor may change; response DTO fields must not.
    [HttpPost("invoices/advanced-extract")]
    [RequestSizeLimit(20 * 1024 * 1024)]
    public async Task<IActionResult> ExtractAdvancedInvoice(
        [FromForm] AdvancedInvoiceExtractionRequest request,
        IFormFile file,
        CancellationToken cancellationToken)
    {
        var session = CurrentSession();
        if (session is null) return Unauthorized(new ApiError("SESSION_INVALID", "Your session is no longer valid."));
        try { return Ok(await operations.RunAdvancedInvoiceExtractionFileAsync(session, request, file, cancellationToken)); }
        catch (OperationalException exception) { return Error(exception); }
    }

    [HttpGet("documents/{id:guid}")]
    public async Task<IActionResult> GetDocument(Guid id, CancellationToken cancellationToken)
    {
        var session = CurrentSession();
        if (session is null) return Unauthorized(new ApiError("SESSION_INVALID", "Your session is no longer valid."));
        var document = await db.Documents.Include(item => item.Invoice).Include(item => item.OperatingUnit).SingleOrDefaultAsync(item => item.Id == id, cancellationToken);
        if (document is null) return NotFound(new ApiError("DOCUMENT_NOT_FOUND", "The document was not found."));
        try { await EnsureViewAsync(session, document.OrganizationId, document.OperatingUnitId, PermissionKeys.ViewInvoice, cancellationToken); }
        catch (OperationalException exception) { return Error(exception); }
        return Ok(new DocumentResponse(document.Id, document.OriginalFilename, document.ContentType, document.FileSizeBytes,
            document.PageCount, document.SourceChannel, document.Status, document.CreatedAt, document.Invoice?.Id ?? Guid.Empty));
    }

    [HttpGet("documents/{id:guid}/content")]
    public async Task<IActionResult> GetDocumentContent(Guid id, CancellationToken cancellationToken)
    {
        var session = CurrentSession();
        if (session is null) return Unauthorized(new ApiError("SESSION_INVALID", "Your session is no longer valid."));
        var document = await db.Documents.SingleOrDefaultAsync(item => item.Id == id, cancellationToken);
        if (document is null) return NotFound(new ApiError("DOCUMENT_NOT_FOUND", "The document was not found."));
        try
        {
            await EnsureViewAsync(session, document.OrganizationId, document.OperatingUnitId, PermissionKeys.ViewInvoice, cancellationToken);
            var content = await storage.GetAsync(document.StorageReference, cancellationToken);
            return new FileStreamResult(content, document.ContentType) { EnableRangeProcessing = true };
        }
        catch (OperationalException exception) { return Error(exception); }
        catch (FileNotFoundException) { return NotFound(new ApiError("DOCUMENT_CONTENT_NOT_FOUND", "The stored document could not be found.")); }
    }

    [HttpGet("documents/{id:guid}/status")]
    public async Task<IActionResult> GetDocumentStatus(Guid id, CancellationToken cancellationToken)
    {
        var session = CurrentSession();
        if (session is null) return Unauthorized(new ApiError("SESSION_INVALID", "Your session is no longer valid."));
        var document = await db.Documents.Include(item => item.Invoice).SingleOrDefaultAsync(item => item.Id == id, cancellationToken);
        if (document is null || document.Invoice is null) return NotFound(new ApiError("DOCUMENT_NOT_FOUND", "The document was not found."));
        try { await EnsureViewAsync(session, document.OrganizationId, document.OperatingUnitId, PermissionKeys.ViewInvoice, cancellationToken); }
        catch (OperationalException exception) { return Error(exception); }
        return Ok(new DocumentStatusResponse(document.Id, document.Invoice.Id, document.Status, document.Invoice.Status, document.Status == DocumentStatus.FAILED ? "The invoice could not be read." : null));
    }

    [HttpPost("documents/{id:guid}/advanced-extract")]
    public async Task<IActionResult> ExtractAdvancedDocument(Guid id, [FromQuery] ExtractionTrigger trigger = ExtractionTrigger.MANUAL_REREAD, CancellationToken cancellationToken = default)
    {
        var session = CurrentSession();
        if (session is null) return Unauthorized(new ApiError("SESSION_INVALID", "Your session is no longer valid."));
        try { return Ok(await operations.RunAdvancedInvoiceExtractionAsync(session, id, trigger, cancellationToken)); }
        catch (OperationalException exception) { return Error(exception); }
    }

    [HttpGet("documents/{id:guid}/transfers")]
    public async Task<IActionResult> GetDocumentTransfers(Guid id, CancellationToken cancellationToken)
    {
        var session = CurrentSession();
        if (session is null) return Unauthorized(new ApiError("SESSION_INVALID", "Your session is no longer valid."));
        try { return Ok(new { documentId = id, transfers = await documentRoutingService.GetTransfersAsync(session, id, cancellationToken) }); }
        catch (OperationalException exception) { return Error(exception); }
    }

    [HttpPost("document-transfers/{id:guid}/retry")]
    public async Task<IActionResult> RetryDocumentTransfer(Guid id, CancellationToken cancellationToken)
    {
        var session = CurrentSession();
        if (session is null) return Unauthorized(new ApiError("SESSION_INVALID", "Your session is no longer valid."));
        try
        {
            var job = await db.DocumentTransferJobs.AsNoTracking().SingleOrDefaultAsync(item => item.Id == id, cancellationToken);
            if (job is null) return NotFound(new ApiError("TRANSFER_NOT_FOUND", "The document transfer was not found."));
            await EnsureViewAsync(session, job.OrganizationId, job.OperatingUnitId, PermissionKeys.EditInvoice, cancellationToken);
            await documentRoutingService.RetryAsync(session, id, cancellationToken);
            return Ok(new { transferId = id, status = DocumentTransferStatus.PENDING });
        }
        catch (OperationalException exception) { return Error(exception); }
    }

    [HttpGet("invoices")]
    public async Task<IActionResult> GetInvoices(CancellationToken cancellationToken)
    {
        var session = CurrentSession();
        if (session is null) return Unauthorized(new ApiError("SESSION_INVALID", "Your session is no longer valid."));
        var scopedOrganizationIds = await AccessibleOrganizationIdsAsync(session, cancellationToken);
        var invoices = await db.Invoices.AsNoTracking()
            .Include(item => item.Supplier).Include(item => item.OperatingUnit).Include(item => item.PurchaseOrder)
            .Where(item => scopedOrganizationIds.Contains(item.OrganizationId))
            .OrderByDescending(item => item.CreatedAt).Take(200).ToListAsync(cancellationToken);
        var visible = new List<InvoiceResponse>();
        foreach (var invoice in invoices)
        {
            if (await CanAsync(session, invoice.OrganizationId, invoice.OperatingUnitId, PermissionKeys.ViewInvoice, cancellationToken))
                visible.Add(ToInvoice(invoice));
        }
        return Ok(visible);
    }

    [HttpGet("invoices/{id:guid}")]
    public async Task<IActionResult> GetInvoice(Guid id, CancellationToken cancellationToken)
    {
        var session = CurrentSession();
        if (session is null) return Unauthorized(new ApiError("SESSION_INVALID", "Your session is no longer valid."));
        try
        {
            var invoice = await operations.GetInvoiceEntityAsync(id, cancellationToken);
            await EnsureViewAsync(session, invoice.OrganizationId, invoice.OperatingUnitId, PermissionKeys.ViewInvoice, cancellationToken);
            return Ok(ToInvoice(invoice));
        }
        catch (OperationalException exception) { return Error(exception); }
    }

    [HttpPost("invoices/{id:guid}/process")]
    public async Task<IActionResult> ProcessInvoice(Guid id, CancellationToken cancellationToken)
    {
        var session = CurrentSession();
        if (session is null) return Unauthorized(new ApiError("SESSION_INVALID", "Your session is no longer valid."));
        try { await operations.ProcessInvoiceAsync(session, id, cancellationToken); return Ok(ToInvoice(await operations.GetInvoiceEntityAsync(id, cancellationToken))); }
        catch (OperationalException exception) { return Error(exception); }
    }

    [HttpPut("invoices/{id:guid}")]
    public async Task<IActionResult> UpdateInvoice(Guid id, [FromBody] UpdateInvoiceRequest request, CancellationToken cancellationToken)
    {
        var session = CurrentSession();
        if (session is null) return Unauthorized(new ApiError("SESSION_INVALID", "Your session is no longer valid."));
        try
        {
            var invoice = await operations.GetInvoiceEntityAsync(id, cancellationToken);
            await EnsureViewAsync(session, invoice.OrganizationId, invoice.OperatingUnitId, PermissionKeys.EditInvoice, cancellationToken);
            var required = await db.InvoiceOcrConfigurations.AsNoTracking()
                .SingleOrDefaultAsync(item => item.OrganizationId == invoice.OrganizationId, cancellationToken);
            var missingFields = new List<string>();
            if ((required?.RequireSupplierName ?? true) && string.IsNullOrWhiteSpace(request.SupplierName)) missingFields.Add("supplierName");
            if ((required?.RequireInvoiceNumber ?? true) && string.IsNullOrWhiteSpace(request.InvoiceNumber)) missingFields.Add("invoiceNumber");
            if (!request.NoPurchaseOrder && (required?.RequirePurchaseOrderNumber ?? true) && string.IsNullOrWhiteSpace(request.PoNumber)) missingFields.Add("purchaseOrderNumber");
            if ((required?.RequireInvoiceAmount ?? true) && request.GrossAmount is null) missingFields.Add("grossAmount");
            if ((required?.RequireInvoiceDate ?? false) && request.InvoiceDate is null) missingFields.Add("invoiceDate");
            if ((required?.RequireCurrency ?? false) && string.IsNullOrWhiteSpace(request.Currency)) missingFields.Add("currency");
            if ((required?.RequireSupplierTrn ?? false) && string.IsNullOrWhiteSpace(request.SupplierTaxNumber)) missingFields.Add("supplierTaxNumber");
            if (missingFields.Count > 0)
                throw new OperationalException("INVOICE_REVIEW_FIELDS_REQUIRED",
                    $"Complete the required invoice fields: {string.Join(", ", missingFields)}.", missingFields: missingFields);
            if (request.SupplierId is null && invoice.SupplierId is null)
                throw new OperationalException("SUPPLIER_REQUIRED", "Select the supplier from Supplier Master before continuing.");
            var normalizedNumber = NormalizeInvoiceValue(request.InvoiceNumber);
            var authoritativeSupplierId = request.SupplierId ?? invoice.SupplierId;
            var duplicate = authoritativeSupplierId is null ? null : (await db.Invoices
                .Where(item => item.OrganizationId == invoice.OrganizationId && item.Id != invoice.Id && item.SupplierId == authoritativeSupplierId)
                .Select(item => new { item.Id, item.InvoiceNumber })
                .ToListAsync(cancellationToken))
                .FirstOrDefault(item => NormalizeInvoiceValue(item.InvoiceNumber) == normalizedNumber);
            if (duplicate is not null)
                throw new OperationalException("DUPLICATE_INVOICE", "This supplier invoice already exists in SILA ME.",
                    existingInvoiceId: duplicate.Id, duplicateType: "PROBABLE");
            var manualFields = string.IsNullOrWhiteSpace(invoice.ManualEditedFieldsJson)
                ? new HashSet<string>(StringComparer.OrdinalIgnoreCase)
                : JsonSerializer.Deserialize<HashSet<string>>(invoice.ManualEditedFieldsJson) ?? [];
            invoice.InvoiceNumber = request.InvoiceNumber.Trim();
            manualFields.Add("InvoiceNumber");
            invoice.InvoiceDate = request.InvoiceDate;
            manualFields.Add("InvoiceDate");
            invoice.SupplierNameRaw = request.SupplierName?.Trim();
            manualFields.Add("SupplierName");
            if (request.SupplierId is not null)
            {
                var supplier = await db.Suppliers.SingleOrDefaultAsync(item =>
                    item.Id == request.SupplierId && item.OrganizationId == invoice.OrganizationId &&
                    item.Status == StatusKind.ACTIVE && !item.IsBlocked && !item.IsDeleted, cancellationToken)
                    ?? throw new OperationalException("SUPPLIER_NOT_FOUND", "The selected supplier was not found in this organization.");
                invoice.SupplierId = supplier.Id;
            }
            invoice.SupplierTaxNumberRaw = request.SupplierTaxNumber?.Trim();
            manualFields.Add("SupplierTaxNumber");
            invoice.PoNumberRaw = request.PoNumber?.Trim();
            manualFields.Add("PoNumber");
            if (request.NoPurchaseOrder) invoice.PurchaseOrderId = null;
            else if (!string.IsNullOrWhiteSpace(request.PoNumber) && (request.SupplierId ?? invoice.SupplierId) is Guid updateSupplierId)
            {
                var supplier = await db.Suppliers.AsNoTracking().SingleOrDefaultAsync(item => item.Id == updateSupplierId, cancellationToken);
                var po = await db.PurchaseOrders.Include(item => item.Items)
                    .FirstOrDefaultAsync(item => item.OrganizationId == invoice.OrganizationId && item.PoNumber == request.PoNumber.Trim(), cancellationToken)
                    ?? throw new OperationalException("PO_NOT_FOUND", "The selected purchase order was not found in this organization.");
                if (supplier is not null && !string.IsNullOrWhiteSpace(supplier.EntityCode) && po.EntityCode != supplier.EntityCode)
                    throw new OperationalException("PO_SCOPE_MISMATCH", "The selected purchase order is outside the current entity.");
                if (po.SupplierId != updateSupplierId)
                    throw new OperationalException("PO_SUPPLIER_MISMATCH", "The selected purchase order does not belong to this supplier.");
                if (!PurchaseOrderReceiving.IsHeaderOpen(po))
                    throw new OperationalException("PO_NOT_OPEN", "The selected purchase order is not open.");
                if (!po.Items.Any(PurchaseOrderReceiving.IsGoodsReceiptEligible))
                    throw new OperationalException("PO_NOT_GR_ELIGIBLE", "The selected purchase order has no goods-receipt-eligible open items.");
                invoice.PurchaseOrderId = po.Id;
            }
            invoice.Currency = request.Currency?.Trim().ToUpperInvariant();
            manualFields.Add("Currency");
            invoice.NetAmount = request.NetAmount;
            manualFields.Add("NetAmount");
            invoice.TaxAmount = request.TaxAmount;
            manualFields.Add("TaxAmount");
            invoice.GrossAmount = request.GrossAmount;
            manualFields.Add("GrossAmount");
            invoice.ManualEditedFieldsJson = JsonSerializer.Serialize(manualFields);
            invoice.ReviewedAt = DateTime.UtcNow;
            invoice.ReviewedByUserId = session.UserId;
            invoice.UpdatedAt = DateTime.UtcNow;
            var extractionRows = await db.InvoiceExtData.Where(item => item.DocumentId == invoice.DocumentId).ToListAsync(cancellationToken);
            if (extractionRows.Count == 0)
            {
                extractionRows.Add(new InvoiceExtData
                {
                    Id = Guid.NewGuid(),
                    DocumentId = invoice.DocumentId,
                    OrganizationId = invoice.OrganizationId,
                    OperatingUnitId = invoice.OperatingUnitId,
                    SourceProvider = "MOBILE_REVIEW",
                    ExtractionMethod = ExtractionMethod.USER_CORRECTED.ToString(),
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow,
                });
                db.InvoiceExtData.Add(extractionRows[0]);
            }
            foreach (var row in extractionRows)
            {
                row.SupplierInvoiceNumber = invoice.InvoiceNumber;
                row.SupplierName = invoice.SupplierNameRaw;
                row.SupplierTrn = invoice.SupplierTaxNumberRaw;
                row.InvoiceDate = invoice.InvoiceDate;
                row.PurchaseOrderNumber = invoice.PoNumberRaw;
                row.InvoiceNet = invoice.NetAmount;
                row.InvoiceGross = invoice.GrossAmount;
                row.Currency = invoice.Currency;
                row.ExtractionMethod = ExtractionMethod.USER_CORRECTED.ToString();
                row.UpdatedAt = DateTime.UtcNow;
            }
            db.AuditEvents.Add(new AuditEvent
            {
                Id = Guid.NewGuid(),
                OrganizationId = invoice.OrganizationId,
                OperatingUnitId = invoice.OperatingUnitId,
                UserId = session.UserId,
                EventType = "INVOICE_REVIEW_SAVED",
                EntityType = "Invoice",
                EntityId = invoice.Id,
                Reference = invoice.InvoiceNumber,
                CreatedAt = DateTime.UtcNow,
            });
            await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);
            await db.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);
            return Ok(ToInvoice(invoice));
        }
        catch (OperationalException exception) { return Error(exception); }
    }

    [HttpGet("invoices/{id:guid}/basic-extraction")]
    public async Task<IActionResult> GetBasicExtraction(Guid id, CancellationToken cancellationToken)
    {
        var session = CurrentSession();
        if (session is null) return Unauthorized(new ApiError("SESSION_INVALID", "Your session is no longer valid."));
        try { return Ok(await operations.GetBasicExtractionAsync(session, id, cancellationToken)); }
        catch (OperationalException exception) { return Error(exception); }
    }

    [HttpGet("invoices/{id:guid}/extraction")]
    public async Task<IActionResult> GetFullExtraction(Guid id, CancellationToken cancellationToken)
    {
        var session = CurrentSession();
        if (session is null) return Unauthorized(new ApiError("SESSION_INVALID", "Your session is no longer valid."));
        try { return Ok(await operations.GetFullExtractionAsync(session, id, cancellationToken)); }
        catch (OperationalException exception) { return Error(exception); }
    }

    [HttpGet("invoices/{id:guid}/extraction-history")]
    public async Task<IActionResult> GetExtractionHistory(Guid id, CancellationToken cancellationToken)
    {
        var session = CurrentSession();
        if (session is null) return Unauthorized(new ApiError("SESSION_INVALID", "Your session is no longer valid."));
        try
        {
            var invoice = await operations.GetInvoiceEntityAsync(id, cancellationToken);
            await EnsureViewAsync(session, invoice.OrganizationId, invoice.OperatingUnitId, "VIEW_EXTRACTION_HISTORY", cancellationToken);
            return Ok(await db.DocumentExtractions.AsNoTracking()
                .Where(item => item.DocumentId == invoice.DocumentId)
                .OrderByDescending(item => item.CreatedAt)
                .Select(item => new
                {
                    item.Id,
                    item.DocumentId,
                    item.Provider,
                    item.Status,
                    item.Trigger,
                    item.OcrRequestId,
                    item.ExtractionMethod,
                    item.Confidence,
                    item.ContentHash,
                    item.FallbackUsed,
                    item.ProcessingStartedAt,
                    item.ProcessingCompletedAt,
                    item.ProcessingDurationMs,
                    item.CreatedAt,
                }).ToListAsync(cancellationToken));
        }
        catch (OperationalException exception) { return Error(exception); }
    }

    [HttpPost("invoices/{id:guid}/reprocess")]
    public async Task<IActionResult> ReprocessInvoice(Guid id, CancellationToken cancellationToken)
    {
        var session = CurrentSession();
        if (session is null) return Unauthorized(new ApiError("SESSION_INVALID", "Your session is no longer valid."));
        try
        {
            var invoice = await operations.GetInvoiceEntityAsync(id, cancellationToken);
            return Ok(await operations.RunAdvancedInvoiceExtractionAsync(session, invoice.DocumentId, ExtractionTrigger.MANUAL_REREAD, cancellationToken));
        }
        catch (OperationalException exception) { return Error(exception); }
    }

    [HttpPost("invoices/{id:guid}/advanced-reread")]
    public async Task<IActionResult> AdvancedReread(Guid id, CancellationToken cancellationToken)
    {
        var session = CurrentSession();
        if (session is null) return Unauthorized(new ApiError("SESSION_INVALID", "Your session is no longer valid."));
        try
        {
            var invoice = await operations.GetInvoiceEntityAsync(id, cancellationToken);
            return Ok(await operations.RunAdvancedInvoiceExtractionAsync(session, invoice.DocumentId, ExtractionTrigger.MANUAL_REREAD, cancellationToken));
        }
        catch (OperationalException exception) { return Error(exception); }
    }

    [HttpGet("configuration/invoice-ocr")]
    public async Task<IActionResult> GetInvoiceOcrConfiguration([FromQuery] Guid? organizationId, CancellationToken cancellationToken)
    {
        var session = CurrentSession();
        if (session is null) return Unauthorized(new ApiError("SESSION_INVALID", "Your session is no longer valid."));
        try { return Ok(await operations.GetInvoiceOcrConfigurationAsync(session, organizationId, cancellationToken)); }
        catch (OperationalException exception) { return Error(exception); }
    }

    [HttpPut("configuration/invoice-ocr")]
    public async Task<IActionResult> UpdateInvoiceOcrConfiguration(
        [FromQuery] Guid? organizationId,
        [FromBody] UpsertInvoiceOcrConfigurationRequest request,
        CancellationToken cancellationToken)
    {
        var session = CurrentSession();
        if (session is null) return Unauthorized(new ApiError("SESSION_INVALID", "Your session is no longer valid."));
        try { return Ok(await operations.UpsertInvoiceOcrConfigurationAsync(session, organizationId, request, cancellationToken)); }
        catch (OperationalException exception) { return Error(exception); }
    }

    [HttpPost("invoices/{id:guid}/match-supplier")]
    public async Task<IActionResult> MatchSupplier(Guid id, [FromBody] MatchSupplierRequest request, CancellationToken cancellationToken)
    {
        var session = CurrentSession();
        if (session is null) return Unauthorized(new ApiError("SESSION_INVALID", "Your session is no longer valid."));
        try { await operations.MatchSupplierAsync(session, id, request.SupplierId, cancellationToken); return Ok(ToInvoice(await operations.GetInvoiceEntityAsync(id, cancellationToken))); }
        catch (OperationalException exception) { return Error(exception); }
    }

    [HttpGet("invoices/{id:guid}/supplier-candidates")]
    public async Task<IActionResult> SupplierCandidates(Guid id, CancellationToken cancellationToken)
    {
        var session = CurrentSession();
        if (session is null) return Unauthorized(new ApiError("SESSION_INVALID", "Your session is no longer valid."));
        try
        {
            var invoice = await operations.GetInvoiceEntityAsync(id, cancellationToken);
            await EnsureViewAsync(session, invoice.OrganizationId, invoice.OperatingUnitId, PermissionKeys.ViewInvoice, cancellationToken);
            var normalized = NormalizeInvoiceValue(invoice.SupplierNameRaw ?? string.Empty);
            var candidates = await db.Suppliers.AsNoTracking()
                .Where(item => item.OrganizationId == invoice.OrganizationId && item.Status == StatusKind.ACTIVE && !item.IsBlocked && !item.IsDeleted &&
                    ((invoice.SupplierTaxNumberRaw != null && item.TaxNumber == invoice.SupplierTaxNumberRaw) ||
                     (normalized != "" && (item.NormalizedName == normalized || item.Aliases.Any(alias => alias.NormalizedAlias == normalized)))))
                .Include(item => item.Aliases)
                .OrderBy(item => item.Name)
                .Take(20)
                .Select(item => new SupplierMatchCandidate(item.Id, item.SupplierCode, item.Name, item.TaxNumber,
                    item.TaxNumber == invoice.SupplierTaxNumberRaw ? 1m : item.NormalizedName == normalized ? .95m : .8m,
                    item.TaxNumber == invoice.SupplierTaxNumberRaw ? "TRN" : item.NormalizedName == normalized ? "NAME" : "ALIAS"))
                .ToListAsync(cancellationToken);
            return Ok(new SupplierMatchResponse(invoice.Id, invoice.SupplierId, invoice.SupplierNameRaw, candidates, invoice.SupplierId is null && candidates.Count != 1));
        }
        catch (OperationalException exception) { return Error(exception); }
    }

    [HttpGet("extraction-agents")]
    public async Task<IActionResult> GetExtractionAgents([FromQuery] Guid? organizationId, CancellationToken cancellationToken)
    {
        var session = CurrentSession();
        if (session is null) return Unauthorized(new ApiError("SESSION_INVALID", "Your session is no longer valid."));
        try { return Ok(await operations.GetExtractionAgentConfigsAsync(session, organizationId, cancellationToken)); }
        catch (OperationalException exception) { return Error(exception); }
    }

    [HttpPost("extraction-agents")]
    public async Task<IActionResult> CreateExtractionAgent([FromQuery] Guid? organizationId, [FromBody] UpsertExtractionAgentConfigRequest request, CancellationToken cancellationToken)
    {
        var session = CurrentSession();
        if (session is null) return Unauthorized(new ApiError("SESSION_INVALID", "Your session is no longer valid."));
        try { return Ok(await operations.UpsertExtractionAgentConfigAsync(session, organizationId, null, request, cancellationToken)); }
        catch (OperationalException exception) { return Error(exception); }
    }

    [HttpPut("extraction-agents/{id:guid}")]
    public async Task<IActionResult> UpdateExtractionAgent(Guid id, [FromBody] UpsertExtractionAgentConfigRequest request, CancellationToken cancellationToken)
    {
        var session = CurrentSession();
        if (session is null) return Unauthorized(new ApiError("SESSION_INVALID", "Your session is no longer valid."));
        try { return Ok(await operations.UpsertExtractionAgentConfigAsync(session, null, id, request, cancellationToken)); }
        catch (OperationalException exception) { return Error(exception); }
    }

    [HttpPost("extraction-agents/{id:guid}/test")]
    public async Task<IActionResult> TestExtractionAgent(Guid id, CancellationToken cancellationToken)
    {
        var session = CurrentSession();
        if (session is null) return Unauthorized(new ApiError("SESSION_INVALID", "Your session is no longer valid."));
        try { return Ok(await operations.TestExtractionAgentConfigAsync(session, id, cancellationToken)); }
        catch (OperationalException exception) { return Error(exception); }
    }

    [HttpPost("invoices/{id:guid}/match-po")]
    public async Task<IActionResult> MatchPurchaseOrder(Guid id, [FromBody] MatchPurchaseOrderRequest request, CancellationToken cancellationToken)
    {
        var session = CurrentSession();
        if (session is null) return Unauthorized(new ApiError("SESSION_INVALID", "Your session is no longer valid."));
        try { await operations.MatchPurchaseOrderAsync(session, id, request.PurchaseOrderId, request.PoNumber, cancellationToken); return Ok(ToInvoice(await operations.GetInvoiceEntityAsync(id, cancellationToken))); }
        catch (OperationalException exception) { return Error(exception); }
    }

    [HttpPost("invoices/{id:guid}/match-lines")]
    public async Task<IActionResult> MatchLines(Guid id, [FromBody] MatchInvoiceLinesRequest request, CancellationToken cancellationToken)
    {
        var session = CurrentSession();
        if (session is null) return Unauthorized(new ApiError("SESSION_INVALID", "Your session is no longer valid."));
        try { await operations.MatchLinesAsync(session, id, request, cancellationToken); return Ok(ToInvoice(await operations.GetInvoiceEntityAsync(id, cancellationToken))); }
        catch (OperationalException exception) { return Error(exception); }
    }

    [HttpGet("purchase-orders/open")]
    public Task<IActionResult> GetOpenPurchaseOrdersForSupplier(
        [FromQuery] Guid? organizationId,
        [FromQuery] string? entityCode,
        [FromQuery] Guid? supplierId,
        [FromQuery] Guid? operatingUnitId,
        CancellationToken cancellationToken) =>
        SearchPurchaseOrders(null, organizationId, entityCode, operatingUnitId, supplierId, openOnly: true, cancellationToken);

    [HttpGet("purchase-orders/search")]
    public async Task<IActionResult> SearchPurchaseOrders([FromQuery] string? query, [FromQuery] Guid? organizationId, [FromQuery] string? entityCode, [FromQuery] Guid? operatingUnitId, [FromQuery] Guid? supplierId, [FromQuery] bool openOnly = false, CancellationToken cancellationToken = default)
    {
        var session = CurrentSession();
        if (session is null) return Unauthorized(new ApiError("SESSION_INVALID", "Your session is no longer valid."));
        var ids = organizationId is null ? await AccessibleOrganizationIdsAsync(session, cancellationToken) : [organizationId.Value];
        var pos = await db.PurchaseOrders.Include(item => item.Items).Include(item => item.Supplier).Include(item => item.OperatingUnit)
             .Where(item => ids.Contains(item.OrganizationId) &&
                 (PurchaseOrderReceiving.IsUnscopedEntity(entityCode) || item.EntityCode == entityCode || item.EntityCode == "ALL") &&
                 (string.IsNullOrWhiteSpace(query) || item.PoNumber.Contains(query) || (item.SupplierName != null && item.SupplierName.Contains(query)) || (item.Supplier != null && item.Supplier.Name.Contains(query))) &&
                (operatingUnitId == null || item.OperatingUnitId == operatingUnitId) &&
                (supplierId == null || item.SupplierId == supplierId) &&
                (!openOnly || (item.Status != PurchaseOrderStatus.CLOSED && item.Status != PurchaseOrderStatus.CANCELLED &&
                     item.Supplier.Status == StatusKind.ACTIVE && !item.Supplier.IsBlocked && !item.Supplier.IsDeleted &&
                     item.Items.Any(line => line.OpenQuantity > 0 &&
                         line.Status != PurchaseOrderItemStatus.CLOSED &&
                         line.Status != PurchaseOrderItemStatus.CANCELLED &&
                         !line.DeletionIndicator &&
                         line.GoodsReceiptExpected &&
                         !line.DeliveryCompleted))))
            .OrderByDescending(item => item.PoDate).Take(50).ToListAsync(cancellationToken);
        var visible = new List<PurchaseOrderResponse>();
        foreach (var po in pos)
            if (await CanAsync(session, po.OrganizationId, null, PermissionKeys.ViewPurchaseOrder, cancellationToken) ||
                await CanAsync(session, po.OrganizationId, po.OperatingUnitId, PermissionKeys.ViewPurchaseOrder, cancellationToken))
                visible.Add(ToPurchaseOrder(po));
        return Ok(visible);
    }

    [HttpGet("purchase-orders/{poNumber}")]
    public async Task<IActionResult> GetPurchaseOrder(string poNumber, [FromQuery] Guid? organizationId, [FromQuery] string? entityCode, CancellationToken cancellationToken)
    {
        var session = CurrentSession();
        if (session is null) return Unauthorized(new ApiError("SESSION_INVALID", "Your session is no longer valid."));
        var trimmed = poNumber.Trim();
        var po = await db.PurchaseOrders.Include(item => item.Items).Include(item => item.Supplier).Include(item => item.OperatingUnit)
            .Where(item => (item.PoNumber == trimmed || item.PoNumber.Contains(trimmed)) &&
                (organizationId == null || item.OrganizationId == organizationId) &&
                (PurchaseOrderReceiving.IsUnscopedEntity(entityCode) || item.EntityCode == entityCode || item.EntityCode == "ALL"))
            .OrderBy(item => item.PoNumber == trimmed ? 0 : 1)
            .ThenBy(item => item.OrganizationId)
            .ThenBy(item => item.EntityCode)
            .FirstOrDefaultAsync(cancellationToken);
        if (po is null) return NotFound(new ApiError("PO_NOT_FOUND", "The purchase order was not found."));
        try { await EnsureViewAsync(session, po.OrganizationId, po.OperatingUnitId, PermissionKeys.ViewPurchaseOrder, cancellationToken); }
        catch (OperationalException exception) { return Error(exception); }
        return Ok(ToPurchaseOrder(po));
    }

    [HttpPost("grns/validate")]
    public async Task<IActionResult> ValidateGrn([FromBody] ValidateGrnRequest request, CancellationToken cancellationToken)
    {
        var session = CurrentSession();
        if (session is null) return Unauthorized(new ApiError("SESSION_INVALID", "Your session is no longer valid."));
        try { return Ok(await operations.ValidateGrnAsync(session, request, cancellationToken)); }
        catch (OperationalException exception) { return Error(exception); }
    }

    [HttpPost("grns")]
    public async Task<IActionResult> PostGrn([FromBody] PostGrnRequest request, [FromHeader(Name = "Idempotency-Key")] string? idempotencyKey, CancellationToken cancellationToken)
    {
        var session = CurrentSession();
        if (session is null) return Unauthorized(new ApiError("SESSION_INVALID", "Your session is no longer valid."));
        try { return Ok(ToGoodsReceipt(await operations.PostGrnAsync(session, request, idempotencyKey, cancellationToken))); }
        catch (OperationalException exception) { return Error(exception); }
    }

    [HttpPost("grns/prepare")]
    public async Task<IActionResult> PrepareGrn([FromBody] PrepareGrnRequest request, CancellationToken cancellationToken)
    {
        var session = CurrentSession();
        if (session is null) return Unauthorized(new ApiError("SESSION_INVALID", "Your session is no longer valid."));
        try { return Ok(ToGoodsReceipt(await operations.PrepareGrnAsync(session, request, cancellationToken))); }
        catch (OperationalException exception) { return Error(exception); }
    }

    [HttpPost("grns/{id:guid}/post")]
    [HttpPost("goods-receipts/{id:guid}/post")]
    public async Task<IActionResult> PostExistingGrn(Guid id, [FromBody] PostGoodsReceiptRequest? request, CancellationToken cancellationToken)
    {
        var session = CurrentSession();
        if (session is null) return Unauthorized(new ApiError("SESSION_INVALID", "Your session is no longer valid."));
        try { return Ok(ToGoodsReceipt(await operations.PostExistingGrnAsync(session, id, request, cancellationToken))); }
        catch (OperationalException exception) { return Error(exception); }
    }

    [HttpPost("grns/{id:guid}/retry")]
    public async Task<IActionResult> RetryGrn(Guid id, CancellationToken cancellationToken)
    {
        var session = CurrentSession();
        if (session is null) return Unauthorized(new ApiError("SESSION_INVALID", "Your session is no longer valid."));
        try { return Ok(ToGoodsReceipt(await operations.RetryGrnAsync(session, id, cancellationToken))); }
        catch (OperationalException exception) { return Error(exception); }
    }

    [HttpGet("grns")]
    public async Task<IActionResult> GetGrns(CancellationToken cancellationToken)
    {
        var session = CurrentSession();
        if (session is null) return Unauthorized(new ApiError("SESSION_INVALID", "Your session is no longer valid."));
        var ids = await AccessibleOrganizationIdsAsync(session, cancellationToken);
        var grns = await db.GoodsReceipts.AsNoTracking().Include(item => item.Lines).ThenInclude(line => line.PurchaseOrderItem)
            .Include(item => item.PurchaseOrder).Include(item => item.Supplier).Include(item => item.Invoice).Include(item => item.OperatingUnit)
            .Where(item => ids.Contains(item.OrganizationId)).OrderByDescending(item => item.CreatedAt).Take(200).ToListAsync(cancellationToken);
        var visible = new List<GoodsReceiptResponse>();
        foreach (var grn in grns)
            if (await CanAsync(session, grn.OrganizationId, grn.OperatingUnitId, PermissionKeys.ViewGrn, cancellationToken)) visible.Add(ToGoodsReceipt(grn));
        return Ok(visible);
    }

    [HttpGet("grns/{id:guid}")]
    public async Task<IActionResult> GetGrn(Guid id, CancellationToken cancellationToken)
    {
        var session = CurrentSession();
        if (session is null) return Unauthorized(new ApiError("SESSION_INVALID", "Your session is no longer valid."));
        try
        {
            var grn = await operations.GetGoodsReceiptEntityAsync(id, cancellationToken);
            await EnsureViewAsync(session, grn.OrganizationId, grn.OperatingUnitId, PermissionKeys.ViewGrn, cancellationToken);
            return Ok(ToGoodsReceipt(grn));
        }
        catch (OperationalException exception) { return Error(exception); }
    }

    [HttpGet("grns/by-number/{grnNumber}")]
    public async Task<IActionResult> GetGrnByNumber(string grnNumber, CancellationToken cancellationToken)
    {
        var grn = await db.GoodsReceipts.SingleOrDefaultAsync(item => item.GrnNumber == grnNumber, cancellationToken);
        return grn is null ? NotFound(new ApiError("GRN_NOT_FOUND", "The goods receipt was not found.")) : await GetGrn(grn.Id, cancellationToken);
    }

    private async Task<List<Guid>> AccessibleOrganizationIdsAsync(Session session, CancellationToken cancellationToken) =>
        await db.UserOrganizationMemberships.Where(item => item.UserId == session.UserId && item.Status == StatusKind.ACTIVE).Select(item => item.OrganizationId).Distinct().ToListAsync(cancellationToken);

    private async Task<bool> CanAsync(Session session, Guid organizationId, Guid? operatingUnitId, string permission, CancellationToken cancellationToken) =>
        await db.Organizations.AnyAsync(item => item.Id == organizationId && item.Status == StatusKind.ACTIVE, cancellationToken) &&
        (operatingUnitId is null || await db.OrganizationUnits.AnyAsync(item => item.Id == operatingUnitId && item.OrganizationId == organizationId && item.Status == StatusKind.ACTIVE, cancellationToken)) &&
        await db.UserRoleAssignments.AnyAsync(item => item.UserId == session.UserId && item.Status == StatusKind.ACTIVE &&
            item.OrganizationId == organizationId && (operatingUnitId == null || item.OrganizationUnitId == null || item.OrganizationUnitId == operatingUnitId) &&
            item.Role.Status == StatusKind.ACTIVE && item.Role.Permissions.Any(mapping => mapping.Permission.Key == permission), cancellationToken);

    private async Task EnsureViewAsync(Session session, Guid organizationId, Guid? operatingUnitId, string permission, CancellationToken cancellationToken)
    {
        if (!await CanAsync(session, organizationId, operatingUnitId, permission, cancellationToken))
            throw new OperationalException("PERMISSION_DENIED", "You do not have permission to view this record.");
    }

    private Session? CurrentSession() => HttpContext.Items[SessionContext.ItemKey] as Session;
    private IActionResult Error(OperationalException exception) =>
        StatusCode(
            exception.Code.Contains("PERMISSION") ? 403
                : exception.Code.Contains("NOT_FOUND") ? 404
                : exception.Code.Contains("DUPLICATE") ? 409
                : 400,
            new ApiError(exception.Code, exception.Message, exception.MissingFields, exception.ExistingInvoiceId, exception.DuplicateType));

    private static string NormalizeInvoiceValue(string value) =>
        System.Text.RegularExpressions.Regex.Replace(value.Trim().ToUpperInvariant(), @"[^A-Z0-9]+", " ").Trim();

    private static InvoiceResponse ToInvoice(Invoice invoice) =>
         new(invoice.Id, invoice.DocumentId, invoice.InvoiceNumber, invoice.InvoiceDate, invoice.Supplier?.Name ?? invoice.SupplierNameRaw, invoice.SupplierTaxNumberRaw,
            invoice.PurchaseOrder?.PoNumber ?? invoice.PoNumberRaw, invoice.PurchaseOrderId, invoice.Currency, invoice.NetAmount, invoice.TaxAmount, invoice.GrossAmount,
            invoice.InvoiceType, invoice.Status, invoice.OverallConfidence, invoice.OrganizationId, invoice.OperatingUnitId, invoice.OperatingUnit?.Name,
            invoice.SupplierId, invoice.Supplier?.SupplierCode, null, invoice.Lines.OrderBy(item => item.LineNumber).Select(line => new InvoiceLineResponse(
                line.Id, line.LineNumber, line.SupplierMaterialCode, line.MaterialId, line.DescriptionRaw, line.Quantity, line.Uom, line.UnitPrice,
                line.TaxRate, line.TaxAmount, line.LineAmount, line.Confidence, line.MatchStatus, line.PurchaseOrderItemId)).ToList(), invoice.CreatedAt, invoice.UpdatedAt);

    private static PurchaseOrderResponse ToPurchaseOrder(PurchaseOrder po) =>
         new(po.Id, po.PoNumber, po.PoDate, po.DeliveryDate, po.Currency, po.Status, po.OrganizationId, po.OperatingUnitId, po.OperatingUnit?.Name,
            po.SupplierId, po.SupplierName ?? po.Supplier?.Name, po.Items.OrderBy(item => item.LineNumber).Select(item => new PurchaseOrderItemResponse(
                 item.Id, item.LineNumber, item.MaterialId, item.MaterialCode, item.Description, item.OrderedQuantity, item.ReceivedQuantity, item.OpenQuantity, item.Uom, item.UnitPrice, item.Status,
                 item.ItemNumber, item.PriceQuantity, item.ItemAmount, item.TaxCode, item.TaxAmount, item.GrossItemAmount, item.Currency,
                 item.MaterialGroup, item.Plant, item.StorageLocation, item.ItemCategory, item.AccountAssignmentCategory,
                 item.GoodsReceiptExpected, item.InvoiceExpected, item.DeliveryCompleted, item.DeletionIndicator)).ToList(),
             po.EntityCode, po.PurchaseOrderType, po.CompanyCode, po.ErpSupplierId, po.PurchasingOrganization, po.PurchasingGroup,
             po.PaymentTerms, po.PoCategory, po.TotalNetAmount, po.TotalTaxAmount, po.TotalAmount, po.TotalOrderedQuantity,
             po.TotalReceivedQuantity, po.SourceSystem, po.SourceLastChangedAt, po.LastSyncedAt);

    private static GoodsReceiptResponse ToGoodsReceipt(GoodsReceipt grn) =>
        new(grn.Id, grn.GrnNumber, grn.Status, grn.PurchaseOrderId, grn.PurchaseOrder.PoNumber, grn.InvoiceId, grn.Invoice?.InvoiceNumber,
            grn.SupplierId, grn.Supplier.Name, grn.OrganizationId, grn.OperatingUnitId ?? Guid.Empty, grn.OperatingUnit?.Name ?? string.Empty, grn.ReceiptDate, grn.CreatedAt, grn.PostedAt,
            grn.BusinessStatus, grn.ErpPostingStatus, grn.ErpMaterialDocument, grn.ErpDocumentYear, grn.ErpResponseJson,
            grn.FailureCode, grn.FailureMessage, grn.ErpAttemptCount,
            grn.Lines.OrderBy(item => item.PurchaseOrderItem.LineNumber).Select(item => new GoodsReceiptLineResponse(
                item.Id, item.PurchaseOrderItemId, item.PurchaseOrderItem.LineNumber, item.MaterialCode, item.Description, item.OpenQuantityBefore, item.InvoiceQuantity,
                item.ReceivedQuantity, item.AcceptedQuantity, item.DamagedQuantity, item.RejectedQuantity, item.Uom, item.BatchNumber, item.ExpiryDate, item.PurchaseOrderItem.OrderedQuantity)).ToList(),
            grn.PurchaseOrder.CompanyCode, grn.ExternalReference, grn.AsnReference, grn.ExternalSystem);
}