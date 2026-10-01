using System.Security.Cryptography;
using System.Text.Json;
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Operations.Application.Features.Shared;
using Operations.Application.Services.Ocr;
using Operations.Application.Services.Storage;
using Operations.Application.Services.Workers;
using Operations.Domain.Common;
using Operations.Domain.Dtos;
using Operations.Domain.Entities;
using Operations.Domain.Enums;
using Operations.Infrastructure.Contracts.IRepository;
using SharedKernel.ExceptionHandler;
using SharedKernel.LoggerServices;

namespace Operations.Application.Features.Commands.UploadInvoice
{
    public class UploadInvoiceCommandHandler : IRequestHandler<UploadInvoiceCommand, DocumentResponseDto>
    {
        private readonly IRepositoryWrapper _repository;
        private readonly ILoggerManager _logger;
        private readonly IDocumentStorageService _storage;
        private readonly IInvoiceOcrPipeline _pipeline;
        private readonly IInvoiceProcessingQueue _queue;

        public UploadInvoiceCommandHandler(
            IRepositoryWrapper repository,
            ILoggerManager logger,
            IDocumentStorageService storage,
            IInvoiceOcrPipeline pipeline,
            IInvoiceProcessingQueue queue)
        {
            _repository = repository;
            _logger = logger;
            _storage = storage;
            _pipeline = pipeline;
            _queue = queue;
        }

        public async Task<DocumentResponseDto> Handle(UploadInvoiceCommand request, CancellationToken cancellationToken)
        {
            _logger.LogInfo($"Uploading invoice document. OrganizationId: {request.OrganizationId}, UserId: {request.UserId}");

            UploadInvoiceRequestDto dto = request.Request;
            IFormFile? file = dto.File;
            if (file == null || file.Length <= 0 || file.Length > Common.MAX_DOCUMENT_SIZE)
            {
                _logger.LogError($"Invoice file is missing or too large. OrganizationId: {request.OrganizationId}");
                throw new BadRequestCustomException("Invalid document.", "Attach an invoice file smaller than 20 MB.");
            }

            if (!HasSupportedIdentity(file))
            {
                _logger.LogError($"Unsupported invoice file. FileName: {file.FileName}, ContentType: {file.ContentType}");
                throw new BadRequestCustomException("Unsupported file.", "Only PDF, PNG, JPG, and JPEG invoices are supported.");
            }

            await OperationsScope.EnsureUnitAsync(_repository, _logger, request.OrganizationId, dto.OperatingUnitId, cancellationToken);

            // A scanner upload was reviewed on the device: its fields are final and required.
            bool reviewed = dto.SourceChannel == DocumentSourceChannel.MOBILE_SCANNER;
            string? scanSessionId = string.IsNullOrWhiteSpace(dto.ScanSessionId) ? null : dto.ScanSessionId.Trim();
            if (reviewed)
            {
                if (scanSessionId == null)
                {
                    throw new BadRequestCustomException("Scan session is required.", "A scan session ID is required for scanner uploads.");
                }

                if (dto.PageCount is < 1 or > 20)
                {
                    throw new BadRequestCustomException("Invalid page count.", "A scan must contain between 1 and 20 pages.");
                }

                Document? scanned = await _repository.Document
                    .FindByCondition(x => x.OrganizationId == request.OrganizationId && x.ScanSessionId == scanSessionId)
                    .FirstOrDefaultAsync(cancellationToken);
                if (scanned != null)
                {
                    Invoice? scannedInvoice = await _repository.Invoice
                        .FindByCondition(x => x.DocumentId == scanned.Id)
                        .FirstOrDefaultAsync(cancellationToken);
                    if (scannedInvoice != null)
                    {
                        _logger.LogInfo($"Scan was already saved. DocumentId: {scanned.Id}, InvoiceId: {scannedInvoice.Id}");
                        return ResponseBuilder.DocumentResponse(scanned, scannedInvoice.Id, "IDEMPOTENT_REPLAY", "This scan was already saved.");
                    }
                }

                List<string> missingFields = await InvoiceWorkflow.MissingReviewFieldsAsync(
                    _repository, request.OrganizationId, dto.SupplierName, dto.SupplierInvoiceNumber, dto.PurchaseOrderNumber,
                    dto.InvoiceGross, dto.InvoiceDate, dto.Currency, dto.SupplierTrn, dto.NoPurchaseOrder, cancellationToken);
                if (missingFields.Count > 0)
                {
                    _logger.LogError($"Reviewed invoice is missing required fields. Fields: {string.Join(", ", missingFields)}");
                    throw new BadRequestCustomException("Invoice review fields are required.", $"Complete the required invoice fields: {string.Join(", ", missingFields)}.");
                }
            }

            byte[] content;
            await using (MemoryStream memory = new MemoryStream())
            {
                await file.CopyToAsync(memory, cancellationToken);
                content = memory.ToArray();
            }

            bool isPdf = Path.GetExtension(file.FileName).Equals(".pdf", StringComparison.OrdinalIgnoreCase)
                || string.Equals(file.ContentType?.Split(';', 2)[0].Trim(), "application/pdf", StringComparison.OrdinalIgnoreCase);
            if (isPdf && !(content.Length >= 4 && content.AsSpan(0, 4).SequenceEqual("%PDF"u8)))
            {
                throw new BadRequestCustomException("Invalid document.", "The uploaded PDF is empty or invalid.");
            }

            string contentHash = Convert.ToHexString(SHA256.HashData(content));
            string? idempotencyKey = string.IsNullOrWhiteSpace(request.IdempotencyKey) ? null : request.IdempotencyKey.Trim();
            if (idempotencyKey != null)
            {
                IdempotencyRecord? existingKey = await _repository.IdempotencyRecord
                    .FindByCondition(x => x.OrganizationId == request.OrganizationId && x.UserId == request.UserId
                        && x.IdempotencyKey == idempotencyKey && x.Operation == Common.OPERATION_SAVE_INVOICE)
                    .FirstOrDefaultAsync(cancellationToken);
                if (existingKey != null)
                {
                    if (!string.Equals(existingKey.RequestHash, contentHash, StringComparison.Ordinal))
                    {
                        _logger.LogError($"Idempotency key reused for different content. OrganizationId: {request.OrganizationId}, UserId: {request.UserId}");
                        throw new ConflictCustomException("Idempotency key was already used.", "The save request key was already used for different invoice content.");
                    }

                    if (Guid.TryParse(existingKey.ResponseReference, out Guid savedInvoiceId))
                    {
                        Invoice? savedInvoice = await _repository.Invoice.GetTrackedAsync(savedInvoiceId, request.OrganizationId, cancellationToken);
                        if (savedInvoice != null)
                        {
                            _logger.LogInfo($"Invoice save replayed. InvoiceId: {savedInvoice.Id}");
                            return ResponseBuilder.DocumentResponse(savedInvoice.Document, savedInvoice.Id, "IDEMPOTENT_REPLAY", "This invoice was already saved.");
                        }
                    }

                    throw new ConflictCustomException("Save in progress.", "This invoice save is already being processed. Retry shortly.");
                }
            }

            List<Guid> sameContentDocumentIds = await _repository.Document
                .FindByCondition(x => x.OrganizationId == request.OrganizationId && x.ContentHash == contentHash)
                .Select(x => x.Id)
                .ToListAsync(cancellationToken);
            if (sameContentDocumentIds.Count > 0)
            {
                Invoice? exactDuplicate = await _repository.Invoice
                    .FindByCondition(x => sameContentDocumentIds.Contains(x.DocumentId))
                    .FirstOrDefaultAsync(cancellationToken);
                if (exactDuplicate != null)
                {
                    _logger.LogError($"Document was already saved as an invoice. ExistingInvoiceId: {exactDuplicate.Id}");
                    throw new ConflictCustomException("Duplicate invoice.", $"This document was already saved as an invoice. ExistingInvoiceId: {exactDuplicate.Id}; DuplicateType: EXACT_DOCUMENT");
                }
            }

            Guid? matchedSupplierId = null;
            if (reviewed)
            {
                Guid? probableDuplicateId = await InvoiceWorkflow.FindProbableDuplicateAsync(
                    _repository, request.OrganizationId, null, dto.SupplierInvoiceNumber!, dto.SupplierTrn, dto.SupplierName, cancellationToken);
                if (probableDuplicateId != null)
                {
                    _logger.LogError($"Invoice with the same supplier and number exists. ExistingInvoiceId: {probableDuplicateId}");
                    throw new ConflictCustomException("Duplicate invoice.", $"An invoice with the same supplier and invoice number already exists. ExistingInvoiceId: {probableDuplicateId}; DuplicateType: PROBABLE");
                }
            }

            if (dto.SupplierId != null)
            {
                SupplierMaster? selectedSupplier = await _repository.SupplierMaster
                    .FindByCondition(x => x.Id == dto.SupplierId && x.OrganizationId == request.OrganizationId
                        && x.Status == StatusKind.ACTIVE && !x.IsBlocked && !x.IsDeleted)
                    .FirstOrDefaultAsync(cancellationToken);
                if (selectedSupplier == null)
                {
                    _logger.LogError($"Supplier not found. SupplierId: {dto.SupplierId}, OrganizationId: {request.OrganizationId}");
                    throw new NotFoundCustomException("Supplier not found.", "The selected supplier was not found in this organization.");
                }

                matchedSupplierId = selectedSupplier.Id;
            }
            else if (!string.IsNullOrWhiteSpace(dto.SupplierName) || !string.IsNullOrWhiteSpace(dto.SupplierTrn))
            {
                // Only an unambiguous match is taken; otherwise the supplier is picked on the review screen.
                List<SupplierMaster> candidates = await _repository.SupplierMaster.FindCandidatesAsync(
                    request.OrganizationId,
                    string.IsNullOrWhiteSpace(dto.SupplierTrn) ? null : dto.SupplierTrn.Trim(),
                    InvoiceWorkflow.Normalize(dto.SupplierName),
                    2,
                    cancellationToken);
                if (candidates.Count == 1)
                {
                    matchedSupplierId = candidates[0].Id;
                }
            }

            string storageReference = await _storage.StoreAsync(request.OrganizationId, content, file.FileName, cancellationToken);
            DateTime now = DateTime.UtcNow;
            Document document = new Document
            {
                Id = Guid.NewGuid(),
                OrganizationId = request.OrganizationId,
                OperatingUnitId = dto.OperatingUnitId,
                DocumentType = DocumentType.INVOICE,
                OriginalFilename = Path.GetFileName(file.FileName),
                ContentType = string.IsNullOrWhiteSpace(file.ContentType) ? "application/octet-stream" : file.ContentType,
                FileSizeBytes = content.LongLength,
                StorageProvider = Common.STORAGE_PROVIDER_LOCAL,
                StorageReference = storageReference,
                ScanSessionId = scanSessionId,
                OcrRequestId = string.IsNullOrWhiteSpace(dto.OcrRequestId) ? null : dto.OcrRequestId.Trim(),
                ContentHash = contentHash,
                PageCount = dto.PageCount,
                Status = dto.DeferFullExtraction ? DocumentStatus.UPLOADED : DocumentStatus.READING,
                SourceChannel = dto.SourceChannel,
                DateCreated = now
            };
            Invoice invoice = new Invoice
            {
                Id = Guid.NewGuid(),
                OrganizationId = request.OrganizationId,
                OperatingUnitId = dto.OperatingUnitId,
                DocumentId = document.Id,
                SupplierId = matchedSupplierId,
                InvoiceNumber = string.IsNullOrWhiteSpace(dto.SupplierInvoiceNumber)
                    ? $"{Common.PENDING_INVOICE_PREFIX}{document.Id:N}"[..24]
                    : dto.SupplierInvoiceNumber.Trim(),
                InvoiceDate = dto.InvoiceDate,
                SupplierNameRaw = string.IsNullOrWhiteSpace(dto.SupplierName) ? null : dto.SupplierName.Trim(),
                SupplierTaxNumberRaw = string.IsNullOrWhiteSpace(dto.SupplierTrn) ? null : dto.SupplierTrn.Trim(),
                PoNumberRaw = dto.NoPurchaseOrder || string.IsNullOrWhiteSpace(dto.PurchaseOrderNumber) ? null : dto.PurchaseOrderNumber.Trim(),
                NoPurchaseOrder = dto.NoPurchaseOrder,
                Currency = string.IsNullOrWhiteSpace(dto.Currency) ? null : dto.Currency.Trim().ToUpperInvariant(),
                GrossAmount = dto.InvoiceGross,
                ManualEditedFieldsJson = string.IsNullOrWhiteSpace(dto.ManualEditedFieldsJson) ? EnteredFieldsJson(dto) : dto.ManualEditedFieldsJson,
                InvoiceType = InvoiceType.UNKNOWN,
                Status = InvoiceStatus.PROCESSING,
                ReviewedAt = reviewed ? now : null,
                ReviewedByUserId = reviewed ? request.UserId : null
            };
            _repository.Document.Create(document);
            _repository.Invoice.Create(invoice);
            AuditTrail.Add(_repository, request.OrganizationId, dto.OperatingUnitId, request.UserId, "INVOICE_UPLOADED", "Invoice", invoice.Id, invoice.InvoiceNumber);

            if (reviewed)
            {
                _repository.DocumentExtraction.Create(new DocumentExtraction
                {
                    Id = Guid.NewGuid(),
                    DocumentId = document.Id,
                    ExtractionType = ExtractionType.BASIC_INVOICE,
                    Provider = Common.OCR_PROVIDER_MOBILE,
                    ExtractionMethod = string.IsNullOrWhiteSpace(dto.ManualEditedFieldsJson) ? ExtractionMethod.OCR : ExtractionMethod.USER_CORRECTED,
                    OcrRequestId = document.OcrRequestId,
                    ContentHash = contentHash,
                    Status = ProcessingStatus.COMPLETED,
                    ProcessingStartedAt = now,
                    ProcessingCompletedAt = now
                });
                _repository.InvoiceExtData.Create(new InvoiceExtData
                {
                    Id = Guid.NewGuid(),
                    DocumentId = document.Id,
                    OrganizationId = request.OrganizationId,
                    OperatingUnitId = dto.OperatingUnitId,
                    SupplierName = invoice.SupplierNameRaw,
                    SupplierTrn = invoice.SupplierTaxNumberRaw,
                    SupplierInvoiceNumber = invoice.InvoiceNumber,
                    InvoiceDate = invoice.InvoiceDate,
                    PurchaseOrderNumber = invoice.PoNumberRaw,
                    InvoiceGross = invoice.GrossAmount,
                    Currency = invoice.Currency,
                    SourceProvider = Common.OCR_PROVIDER_MOBILE,
                    ExtractionMethod = string.IsNullOrWhiteSpace(dto.ManualEditedFieldsJson) ? ExtractionMethod.OCR.ToString() : ExtractionMethod.USER_CORRECTED.ToString()
                });
                AuditTrail.Add(_repository, request.OrganizationId, dto.OperatingUnitId, request.UserId, "INVOICE_REVIEW_SAVED", "Invoice", invoice.Id, invoice.InvoiceNumber);
            }

            if (idempotencyKey != null)
            {
                _repository.IdempotencyRecord.Create(new IdempotencyRecord
                {
                    Id = Guid.NewGuid(),
                    OrganizationId = request.OrganizationId,
                    UserId = request.UserId,
                    IdempotencyKey = idempotencyKey,
                    Operation = Common.OPERATION_SAVE_INVOICE,
                    RequestHash = contentHash,
                    ResponseReference = invoice.Id.ToString(),
                    Status = IdempotencyStatus.COMPLETED,
                    ExpiresAt = now.AddHours(24)
                });
            }

            await DocumentRouting.QueueTransferAsync(_repository, document, request.UserId, cancellationToken);

            // Document, invoice, idempotency record and transfer job are stored together.
            await _repository.SaveAsync();
            _logger.LogInfo($"Invoice document saved. DocumentId: {document.Id}, InvoiceId: {invoice.Id}, OrganizationId: {request.OrganizationId}");

            if (dto.DeferFullExtraction)
            {
                await _queue.EnqueueAsync(new InvoiceProcessingJob
                {
                    OrganizationId = request.OrganizationId,
                    UserId = request.UserId,
                    DocumentId = document.Id
                }, cancellationToken);
            }
            else
            {
                await InvoiceExtractionWorkflow.RunBasicAsync(_repository, _storage, _pipeline, _logger, invoice.Id, request.OrganizationId, request.UserId, cancellationToken);
            }

            _logger.LogInfo($"Invoice upload completed. InvoiceId: {invoice.Id}, Status: {invoice.Status}, Deferred: {dto.DeferFullExtraction}");
            return ResponseBuilder.DocumentResponse(document, invoice.Id, "SAVED",
                invoice.ExtractionStatus == Common.MANUAL_ENTRY_REQUIRED
                    ? Common.OCR_UNAVAILABLE_MESSAGE
                    : "Invoice saved. Continue with purchase-order matching.");
        }

        private static readonly HashSet<string> AllowedExtensions = new HashSet<string> { ".pdf", ".png", ".jpg", ".jpeg" };
        private static readonly HashSet<string> AllowedContentTypes = new HashSet<string> { "application/pdf", "image/png", "image/jpeg" };

        // A browser "Blob" upload has no useful file name, so the MIME type is accepted as well.
        private static bool HasSupportedIdentity(IFormFile file)
        {
            string extension = Path.GetExtension(file.FileName).ToLowerInvariant();
            string? contentType = file.ContentType?.Split(';', 2)[0].Trim().ToLowerInvariant();
            return AllowedExtensions.Contains(extension) || (contentType != null && AllowedContentTypes.Contains(contentType));
        }

        // Header values typed on the upload form are the user's: extraction must not overwrite them.
        private static string? EnteredFieldsJson(UploadInvoiceRequestDto dto)
        {
            List<string> fields = new List<string>();
            if (!string.IsNullOrWhiteSpace(dto.SupplierName))
            {
                fields.Add("SupplierName");
            }

            if (!string.IsNullOrWhiteSpace(dto.SupplierTrn))
            {
                fields.Add("SupplierTaxNumber");
            }

            if (!string.IsNullOrWhiteSpace(dto.SupplierInvoiceNumber))
            {
                fields.Add("InvoiceNumber");
            }

            if (dto.InvoiceDate != null)
            {
                fields.Add("InvoiceDate");
            }

            if (!string.IsNullOrWhiteSpace(dto.PurchaseOrderNumber))
            {
                fields.Add("PoNumber");
            }

            if (dto.InvoiceGross != null)
            {
                fields.Add("GrossAmount");
            }

            if (!string.IsNullOrWhiteSpace(dto.Currency))
            {
                fields.Add("Currency");
            }

            return fields.Count == 0 ? null : JsonSerializer.Serialize(fields);
        }
    }
}
