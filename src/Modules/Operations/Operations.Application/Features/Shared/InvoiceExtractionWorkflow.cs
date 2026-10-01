using System.Security.Cryptography;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Operations.Application.Services.Ocr;
using Operations.Application.Services.Storage;
using Operations.Domain.Common;
using Operations.Domain.Dtos;
using Operations.Domain.Entities;
using Operations.Domain.Enums;
using Operations.Infrastructure.Contracts.IRepository;
using SharedKernel.ExceptionHandler;
using SharedKernel.LoggerServices;

namespace Operations.Application.Features.Shared
{
    /// <summary>
    /// The two extraction runs of an invoice document: the basic run made when an invoice is
    /// uploaded or processed, and the advanced field-by-field run made on a re-read or by the
    /// processing worker. Centralized here because several commands start the same run.
    /// Both fail soft: when OCR is not installed the document is kept and the invoice is handed
    /// to manual entry (status REVIEW_REQUIRED) instead of throwing.
    /// </summary>
    internal static class InvoiceExtractionWorkflow
    {
        public static async Task RunBasicAsync(
            IRepositoryWrapper repository,
            IDocumentStorageService storage,
            IInvoiceOcrPipeline pipeline,
            ILoggerManager logger,
            Guid invoiceId,
            Guid organizationId,
            Guid userId,
            CancellationToken cancellationToken)
        {
            Invoice? invoice = await repository.Invoice.GetTrackedAsync(invoiceId, organizationId, cancellationToken);
            if (invoice == null)
            {
                logger.LogError($"Invoice not found. InvoiceId: {invoiceId}, OrganizationId: {organizationId}");
                throw new NotFoundCustomException("Invoice not found.", "The invoice does not exist in your organization.");
            }

            Document document = invoice.Document;
            List<InvoiceLine> lines = await repository.InvoiceLine.GetTrackedByInvoiceAsync(invoice.Id, cancellationToken);
            byte[] content = await ReadContentAsync(storage, logger, document, cancellationToken);
            DateTime started = DateTime.UtcNow;
            ExtractionAgentConfig? agent = await ResolveAgentAsync(repository, organizationId, document.DocumentType, cancellationToken);

            AuditTrail.Add(repository, organizationId, invoice.OperatingUnitId, userId, "BASIC_EXTRACTION_STARTED", "Invoice", invoice.Id, invoice.InvoiceNumber);
            OcrTextResult read = await pipeline.ReadTextAsync(document, content, agent, cancellationToken);
            DocumentExtraction extraction = new DocumentExtraction
            {
                Id = Guid.NewGuid(),
                DocumentId = document.Id,
                ExtractionType = read.Method == ExtractionMethod.PDF_TEXT ? ExtractionType.PDF_TEXT : ExtractionType.OCR,
                Provider = read.Provider,
                RawText = read.Text,
                Confidence = read.Confidence,
                ProcessingStartedAt = started,
                ExtractionMethod = read.Method,
                FallbackUsed = read.FallbackUsed,
                ErrorCategory = read.ErrorCategory,
                OcrRequestId = document.OcrRequestId,
                ContentHash = document.ContentHash
            };
            repository.DocumentExtraction.Create(extraction);
            if (agent != null)
            {
                AuditTrail.Add(repository, organizationId, invoice.OperatingUnitId, userId, "EXTERNAL_AGENT_SELECTED", "Invoice", invoice.Id, agent.ProviderType);
            }

            if (read.FallbackUsed)
            {
                extraction.ErrorMessage = "The configured external provider failed; built-in extraction was used.";
                AuditTrail.Add(repository, organizationId, invoice.OperatingUnitId, userId, "BUILTIN_OCR_FALLBACK", "Invoice", invoice.Id, read.Provider);
            }

            if (string.IsNullOrWhiteSpace(read.Text))
            {
                // Fail soft: with the OCR binaries missing the stored document is kept and the
                // invoice goes to manual entry; an unreadable scan is reported as OCR_FAILED.
                invoice.Status = read.OcrUnavailable ? InvoiceStatus.REVIEW_REQUIRED : InvoiceStatus.OCR_FAILED;
                invoice.ExtractionStatus = read.OcrUnavailable ? Common.MANUAL_ENTRY_REQUIRED : Common.OCR_FAILED;
                document.Status = read.OcrUnavailable ? DocumentStatus.REVIEW_REQUIRED : DocumentStatus.FAILED;
                extraction.Status = ProcessingStatus.FAILED;
                extraction.ErrorCode = read.OcrUnavailable ? Common.OCR_UNAVAILABLE : Common.OCR_FAILED;
                extraction.ErrorMessage ??= read.OcrUnavailable ? Common.OCR_UNAVAILABLE_MESSAGE : "The built-in OCR provider could not read this document.";
                extraction.ProcessingCompletedAt = DateTime.UtcNow;
                await repository.SaveAsync();
                logger.LogError($"Invoice text could not be read. InvoiceId: {invoice.Id}, OcrUnavailable: {read.OcrUnavailable}");
                return;
            }

            ExtractedInvoice extracted = pipeline.Parse(read.Text);
            bool duplicate = extracted.InvoiceNumber != null && await repository.Invoice
                .FindByCondition(x => x.OrganizationId == organizationId && x.SupplierId != null
                    && x.InvoiceNumber == extracted.InvoiceNumber && x.Id != invoice.Id)
                .AnyAsync(cancellationToken);
            if (duplicate)
            {
                invoice.Status = InvoiceStatus.FAILED;
                document.Status = DocumentStatus.FAILED;
                extraction.Status = ProcessingStatus.FAILED;
                extraction.ErrorCode = "DUPLICATE_INVOICE";
                extraction.ProcessingCompletedAt = DateTime.UtcNow;
                await repository.SaveAsync();
                logger.LogError($"Duplicate invoice number read from the document. InvoiceId: {invoice.Id}, InvoiceNumber: {extracted.InvoiceNumber}");
                throw new ConflictCustomException("Duplicate invoice.", "This invoice appears to have already been uploaded.");
            }

            // Values the user confirmed on the mobile scanner are never overwritten by OCR.
            bool confirmed = document.SourceChannel == DocumentSourceChannel.MOBILE_SCANNER;
            HashSet<string> manualFields = InvoiceWorkflow.ParseManualFields(invoice.ManualEditedFieldsJson);
            if (!(confirmed && !InvoiceWorkflow.IsPendingNumber(invoice.InvoiceNumber)) && !manualFields.Contains("InvoiceNumber"))
            {
                invoice.InvoiceNumber = extracted.InvoiceNumber ?? invoice.InvoiceNumber;
            }

            invoice.InvoiceDate = Keep(invoice.InvoiceDate != null, confirmed, manualFields, "InvoiceDate") ? invoice.InvoiceDate : extracted.InvoiceDate ?? invoice.InvoiceDate;
            invoice.SupplierNameRaw = Keep(!string.IsNullOrWhiteSpace(invoice.SupplierNameRaw), confirmed, manualFields, "SupplierName") ? invoice.SupplierNameRaw : extracted.SupplierName ?? invoice.SupplierNameRaw;
            invoice.SupplierTaxNumberRaw = Keep(!string.IsNullOrWhiteSpace(invoice.SupplierTaxNumberRaw), confirmed, manualFields, "SupplierTaxNumber") ? invoice.SupplierTaxNumberRaw : extracted.SupplierTaxNumber ?? invoice.SupplierTaxNumberRaw;
            invoice.PoNumberRaw = Keep(!string.IsNullOrWhiteSpace(invoice.PoNumberRaw), confirmed, manualFields, "PoNumber") ? invoice.PoNumberRaw : extracted.PoNumber ?? invoice.PoNumberRaw;
            invoice.Currency = Keep(!string.IsNullOrWhiteSpace(invoice.Currency), confirmed, manualFields, "Currency") ? invoice.Currency : extracted.Currency?.ToUpperInvariant() ?? invoice.Currency;
            invoice.NetAmount = manualFields.Contains("NetAmount") ? invoice.NetAmount : extracted.NetAmount ?? invoice.NetAmount;
            invoice.TaxAmount = manualFields.Contains("TaxAmount") ? invoice.TaxAmount : extracted.TaxAmount ?? invoice.TaxAmount;
            invoice.GrossAmount = Keep(invoice.GrossAmount != null, confirmed, manualFields, "GrossAmount") ? invoice.GrossAmount : extracted.GrossAmount ?? invoice.GrossAmount;
            invoice.OverallConfidence = read.Confidence;
            invoice.InvoiceType = extracted.Lines.Count == 0 ? InvoiceType.SERVICE : InvoiceType.MATERIAL;
            invoice.ExtractionStatus = "SUCCESS";
            invoice.ExtractionProvider = read.Provider;
            invoice.ExtractedAt = DateTime.UtcNow;

            repository.InvoiceLine.DeleteRange(lines);
            lines = extracted.Lines.Select(line => new InvoiceLine
            {
                Id = Guid.NewGuid(),
                InvoiceId = invoice.Id,
                LineNumber = line.LineNumber,
                SupplierMaterialCode = line.SupplierMaterialCode,
                MaterialCodeRaw = line.SupplierMaterialCode,
                DescriptionRaw = line.Description,
                Quantity = line.Quantity,
                Uom = line.Uom,
                UnitPrice = line.UnitPrice,
                TaxRate = line.TaxRate,
                TaxAmount = line.TaxAmount,
                LineAmount = line.LineAmount,
                Confidence = read.Confidence,
                MatchStatus = InvoiceLineMatchStatus.UNMATCHED
            }).ToList();
            repository.InvoiceLine.CreateRange(lines);

            await InvoiceWorkflow.MatchSupplierAsync(repository, invoice, cancellationToken);
            await InvoiceWorkflow.MatchPurchaseOrderAsync(repository, invoice, cancellationToken);
            await InvoiceWorkflow.MatchLinesAsync(repository, invoice, lines, cancellationToken);
            invoice.Status = InvoiceWorkflow.ResolveStatus(invoice, lines);
            document.Status = DocumentStatus.FULL_EXTRACTION_COMPLETE;

            extraction.ExtractionType = ExtractionType.FULL_INVOICE;
            extraction.Status = ProcessingStatus.COMPLETED;
            extraction.ProcessingCompletedAt = DateTime.UtcNow;
            extraction.ProcessingDurationMs = (long)(DateTime.UtcNow - started).TotalMilliseconds;
            await InvoiceWorkflow.ReplaceExtDataAsync(repository, invoice, document, lines, read.Confidence, read.Method, read.Provider, cancellationToken);
            AuditTrail.Add(repository, organizationId, invoice.OperatingUnitId, userId, "INVOICE_EXTRACTION_COMPLETED", "Invoice", invoice.Id, invoice.InvoiceNumber);
            await repository.SaveAsync();
        }

        public static async Task<AdvancedInvoiceExtractionResponseDto> RunAdvancedAsync(
            IRepositoryWrapper repository,
            IDocumentStorageService storage,
            IInvoiceOcrPipeline pipeline,
            ILoggerManager logger,
            Document document,
            Invoice? invoice,
            ExtractionTrigger trigger,
            Guid userId,
            CancellationToken cancellationToken)
        {
            byte[] content = await ReadContentAsync(storage, logger, document, cancellationToken);
            DateTime started = DateTime.UtcNow;
            document.OcrRequestId ??= $"ocr-{Guid.NewGuid():N}";
            InvoiceOcrConfiguration configuration = await OcrConfigurationAsync(repository, document.OrganizationId, cancellationToken);
            ExtractionAgentConfig? agent = await ResolveAgentAsync(repository, document.OrganizationId, document.DocumentType, cancellationToken);

            OcrTextResult read = await pipeline.ReadTextAsync(document, content, agent, cancellationToken);
            AdvancedInvoiceExtractionResponseDto response = pipeline.BuildAdvancedResponse(document, read.Text, trigger, configuration, started, read.OcrUnavailable);
            response.FallbackUsed = read.FallbackUsed;
            if (response.Status != "FAILED")
            {
                string normalizedSupplier = InvoiceWorkflow.Normalize(response.Header.SupplierName.Value);
                string? supplierTrn = response.Header.SupplierTrn.Value;
                string? poNumber = response.Header.PurchaseOrderNumber.Value;
                response.Validation.SupplierMatched = (normalizedSupplier.Length > 0 || !string.IsNullOrWhiteSpace(supplierTrn))
                    && await repository.SupplierMaster
                        .FindByCondition(x => x.OrganizationId == document.OrganizationId
                            && ((normalizedSupplier != "" && x.NormalizedName == normalizedSupplier) || (supplierTrn != null && x.TaxNumber == supplierTrn)))
                        .AnyAsync(cancellationToken);
                response.Validation.PurchaseOrderMatched = !string.IsNullOrWhiteSpace(poNumber)
                    && await repository.PurchaseOrder
                        .FindByCondition(x => x.OrganizationId == document.OrganizationId && x.PoNumber == poNumber)
                        .AnyAsync(cancellationToken);
            }

            if (invoice != null)
            {
                string contentHash = Convert.ToHexString(SHA256.HashData(content));
                string snapshotJson = JsonSerializer.Serialize(response.EffectiveConfiguration);
                await ApplyAdvancedAsync(repository, invoice, document, response, read, contentHash, snapshotJson, trigger, userId, cancellationToken);
                await repository.SaveAsync();
            }

            logger.LogInfo($"Advanced extraction finished. DocumentId: {document.Id}, Trigger: {trigger}, Status: {response.Status}, Lines: {response.Lines.Count}, OcrUnavailable: {read.OcrUnavailable}");
            return response;
        }

        public static async Task<InvoiceOcrConfiguration> OcrConfigurationAsync(IRepositoryWrapper repository, Guid organizationId, CancellationToken cancellationToken)
        {
            InvoiceOcrConfiguration? configuration = await repository.InvoiceOcrConfiguration
                .FindByCondition(x => x.OrganizationId == organizationId)
                .FirstOrDefaultAsync(cancellationToken);

            // An organization that never saved its policy works with the built-in defaults.
            return configuration ?? new InvoiceOcrConfiguration
            {
                Id = Guid.Empty,
                OrganizationId = organizationId,
                DateCreated = DateTime.UtcNow,
                DateUpdated = DateTime.UtcNow
            };
        }

        private static async Task ApplyAdvancedAsync(
            IRepositoryWrapper repository,
            Invoice invoice,
            Document document,
            AdvancedInvoiceExtractionResponseDto response,
            OcrTextResult read,
            string contentHash,
            string snapshotJson,
            ExtractionTrigger trigger,
            Guid userId,
            CancellationToken cancellationToken)
        {
            DocumentExtraction extraction = new DocumentExtraction
            {
                Id = Guid.NewGuid(),
                DocumentId = document.Id,
                ExtractionType = ExtractionType.FULL_INVOICE,
                Provider = response.Provider,
                RawText = response.RawText,
                Confidence = response.Confidence,
                ProcessingStartedAt = response.StartedAt,
                ProcessingCompletedAt = response.CompletedAt,
                ExtractionMethod = read.Method,
                FallbackUsed = read.FallbackUsed,
                ErrorCategory = read.ErrorCategory,
                Trigger = trigger,
                OcrRequestId = document.OcrRequestId,
                ContentHash = contentHash,
                ConfigurationSnapshotJson = snapshotJson,
                ProcessingDurationMs = response.StartedAt.HasValue && response.CompletedAt.HasValue
                    ? (long)(response.CompletedAt.Value - response.StartedAt.Value).TotalMilliseconds
                    : null
            };
            repository.DocumentExtraction.Create(extraction);

            if (response.Status == "FAILED")
            {
                // Nothing could be read (unreadable scan, or the OCR binaries are not installed).
                // What the user already entered is kept and the invoice goes to manual entry.
                invoice.Status = InvoiceStatus.REVIEW_REQUIRED;
                invoice.ExtractionStatus = read.OcrUnavailable ? Common.MANUAL_ENTRY_REQUIRED : response.Status;
                invoice.ExtractionProvider = response.Provider;
                document.Status = DocumentStatus.REVIEW_REQUIRED;
                extraction.Status = ProcessingStatus.FAILED;
                extraction.ErrorCode = read.OcrUnavailable ? Common.OCR_UNAVAILABLE : Common.OCR_FAILED;
                extraction.ErrorMessage = response.ErrorMessage;
                AuditTrail.Add(repository, invoice.OrganizationId, invoice.OperatingUnitId, userId,
                    read.OcrUnavailable ? "OCR_UNAVAILABLE_MANUAL_ENTRY" : "BACKEND_OCR_FAILED", "Invoice", invoice.Id, invoice.InvoiceNumber);
                return;
            }

            AdvancedInvoiceHeaderResponseDto header = response.Header;
            HashSet<string> manualFields = InvoiceWorkflow.ParseManualFields(invoice.ManualEditedFieldsJson);
            if (!manualFields.Contains("InvoiceNumber") && !string.IsNullOrWhiteSpace(header.InvoiceNumber.Value))
            {
                invoice.InvoiceNumber = header.InvoiceNumber.Value;
            }

            if (!manualFields.Contains("InvoiceDate"))
            {
                invoice.InvoiceDate = header.InvoiceDate.Value;
            }

            if (!manualFields.Contains("SupplierName"))
            {
                invoice.SupplierNameRaw = header.SupplierName.Value;
            }

            if (!manualFields.Contains("SupplierTaxNumber"))
            {
                invoice.SupplierTaxNumberRaw = header.SupplierTrn.Value;
            }

            if (!manualFields.Contains("PoNumber"))
            {
                invoice.PoNumberRaw = header.PurchaseOrderNumber.Value;
            }

            if (!manualFields.Contains("Currency"))
            {
                invoice.Currency = header.Currency.Value?.ToUpperInvariant();
            }

            if (!manualFields.Contains("NetAmount"))
            {
                invoice.NetAmount = header.NetAmount.Value;
            }

            if (!manualFields.Contains("TaxAmount"))
            {
                invoice.TaxAmount = header.TaxAmount.Value;
            }

            if (!manualFields.Contains("GrossAmount"))
            {
                invoice.GrossAmount = header.GrossAmount.Value;
            }

            invoice.SupplierLegalName = header.SupplierLegalName.Value;
            invoice.SupplierAddress = header.SupplierAddress.Value;
            invoice.SupplierEmail = header.SupplierEmail.Value;
            invoice.SupplierPhone = header.SupplierPhone.Value;
            invoice.DiscountAmount = header.DiscountAmount.Value;
            invoice.FreightAmount = header.FreightAmount.Value;
            invoice.OtherCharges = header.OtherCharges.Value;
            invoice.TaxableAmount = header.TaxableAmount.Value;
            invoice.AmountDue = header.AmountDue.Value;
            invoice.PaymentTerms = header.PaymentTerms.Value;
            invoice.DueDate = header.DueDate.Value;
            invoice.OverallConfidence = response.Confidence;
            invoice.ExtractionStatus = response.Status;
            invoice.ExtractionProvider = response.Provider;
            invoice.ExtractedAt = response.CompletedAt;
            invoice.InvoiceType = Enum.TryParse(header.InvoiceType.Value, true, out InvoiceType invoiceType) ? invoiceType : InvoiceType.UNKNOWN;

            // A re-read replaces the current lines; it never adds to them.
            List<InvoiceLine> existingLines = await repository.InvoiceLine.GetTrackedByInvoiceAsync(invoice.Id, cancellationToken);
            repository.InvoiceLine.DeleteRange(existingLines);
            List<InvoiceLine> lines = response.Lines.Select(line => new InvoiceLine
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
                MatchStatus = InvoiceLineMatchStatus.UNMATCHED
            }).ToList();
            repository.InvoiceLine.CreateRange(lines);

            await InvoiceWorkflow.MatchSupplierAsync(repository, invoice, cancellationToken);
            await InvoiceWorkflow.MatchPurchaseOrderAsync(repository, invoice, cancellationToken);
            await InvoiceWorkflow.MatchLinesAsync(repository, invoice, lines, cancellationToken);
            invoice.Status = response.RequiresReview ? InvoiceStatus.REVIEW_REQUIRED : InvoiceWorkflow.ResolveStatus(invoice, lines);
            document.Status = response.RequiresReview ? DocumentStatus.REVIEW_REQUIRED : DocumentStatus.FULL_EXTRACTION_COMPLETE;

            extraction.Status = response.Status == "SUCCESS" ? ProcessingStatus.COMPLETED : ProcessingStatus.REVIEW_REQUIRED;
            extraction.StructuredPayloadJson = JsonSerializer.Serialize(response);
            await InvoiceWorkflow.ReplaceExtDataAsync(repository, invoice, document, lines, response.Confidence, read.Method, response.Provider, cancellationToken);
            AuditTrail.Add(repository, invoice.OrganizationId, invoice.OperatingUnitId, userId,
                trigger == ExtractionTrigger.MANUAL_REREAD ? "INVOICE_EXTRACTION_UPDATED" : "BACKEND_OCR_COMPLETED", "Invoice", invoice.Id, invoice.InvoiceNumber);
        }

        // The active external extraction agent of the organization (or a global one), lowest priority number first.
        private static Task<ExtractionAgentConfig?> ResolveAgentAsync(IRepositoryWrapper repository, Guid organizationId, DocumentType documentType, CancellationToken cancellationToken)
        {
            string type = documentType.ToString();
            return repository.ExtractionAgentConfig
                .FindByCondition(x => x.Enabled && x.IsActive && x.DocumentType == type
                    && (x.OrganizationId == null || x.OrganizationId == organizationId))
                .OrderBy(x => x.Priority)
                .ThenByDescending(x => x.OrganizationId != null)
                .FirstOrDefaultAsync(cancellationToken);
        }

        private static async Task<byte[]> ReadContentAsync(IDocumentStorageService storage, ILoggerManager logger, Document document, CancellationToken cancellationToken)
        {
            try
            {
                return await storage.ReadAsync(document.StorageReference, cancellationToken);
            }
            catch (FileNotFoundException)
            {
                logger.LogError($"Stored document content not found. DocumentId: {document.Id}");
                throw new NotFoundCustomException("Document content not found.", "The stored file of this document could not be found.");
            }
        }

        private static bool Keep(bool hasValue, bool confirmed, HashSet<string> manualFields, string field)
        {
            return (confirmed && hasValue) || manualFields.Contains(field);
        }
    }
}
