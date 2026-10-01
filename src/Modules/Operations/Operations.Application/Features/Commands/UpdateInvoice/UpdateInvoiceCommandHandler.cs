using System.Text.Json;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Operations.Application.Features.Shared;
using Operations.Domain.Common;
using Operations.Domain.Dtos;
using Operations.Domain.Entities;
using Operations.Domain.Enums;
using Operations.Infrastructure.Contracts.IRepository;
using SharedKernel.ExceptionHandler;
using SharedKernel.LoggerServices;

namespace Operations.Application.Features.Commands.UpdateInvoice
{
    public class UpdateInvoiceCommandHandler : IRequestHandler<UpdateInvoiceCommand, InvoiceResponseDto>
    {
        private readonly IRepositoryWrapper _repository;
        private readonly ILoggerManager _logger;

        public UpdateInvoiceCommandHandler(IRepositoryWrapper repository, ILoggerManager logger)
        {
            _repository = repository;
            _logger = logger;
        }

        public async Task<InvoiceResponseDto> Handle(UpdateInvoiceCommand request, CancellationToken cancellationToken)
        {
            _logger.LogInfo($"Updating invoice. InvoiceId: {request.InvoiceId}, OrganizationId: {request.OrganizationId}, UserId: {request.UserId}");

            UpdateInvoiceRequestDto dto = request.Request;
            Invoice? invoice = await _repository.Invoice.GetTrackedAsync(request.InvoiceId, request.OrganizationId, cancellationToken);
            if (invoice == null)
            {
                _logger.LogError($"Invoice not found. InvoiceId: {request.InvoiceId}, OrganizationId: {request.OrganizationId}");
                throw new NotFoundCustomException("Invoice not found.", "The invoice does not exist in your organization.");
            }

            List<string> missingFields = await InvoiceWorkflow.MissingReviewFieldsAsync(
                _repository, request.OrganizationId, dto.SupplierName, dto.InvoiceNumber, dto.PoNumber,
                dto.GrossAmount, dto.InvoiceDate, dto.Currency, dto.SupplierTaxNumber, dto.NoPurchaseOrder, cancellationToken);
            if (missingFields.Count > 0)
            {
                _logger.LogError($"Invoice is missing required fields. InvoiceId: {invoice.Id}, Fields: {string.Join(", ", missingFields)}");
                throw new BadRequestCustomException("Invoice review fields are required.", $"Complete the required invoice fields: {string.Join(", ", missingFields)}.");
            }

            Guid? duplicateId = await InvoiceWorkflow.FindProbableDuplicateAsync(
                _repository, request.OrganizationId, invoice.Id, dto.InvoiceNumber, dto.SupplierTaxNumber, dto.SupplierName, cancellationToken);
            if (duplicateId != null)
            {
                _logger.LogError($"Invoice with the same supplier and number exists. InvoiceId: {invoice.Id}, ExistingInvoiceId: {duplicateId}");
                throw new ConflictCustomException("Duplicate invoice.", $"An invoice with the same supplier and invoice number already exists. ExistingInvoiceId: {duplicateId}; DuplicateType: PROBABLE");
            }

            if (dto.SupplierId != null)
            {
                bool supplierExists = await _repository.SupplierMaster
                    .FindByCondition(x => x.Id == dto.SupplierId && x.OrganizationId == request.OrganizationId
                        && x.Status == StatusKind.ACTIVE && !x.IsBlocked && !x.IsDeleted)
                    .AnyAsync(cancellationToken);
                if (!supplierExists)
                {
                    _logger.LogError($"Supplier not found. SupplierId: {dto.SupplierId}, OrganizationId: {request.OrganizationId}");
                    throw new NotFoundCustomException("Supplier not found.", "The selected supplier was not found in this organization.");
                }

                invoice.SupplierId = dto.SupplierId;
            }

            invoice.InvoiceNumber = dto.InvoiceNumber.Trim();
            invoice.InvoiceDate = dto.InvoiceDate;
            invoice.SupplierNameRaw = dto.SupplierName?.Trim();
            invoice.SupplierTaxNumberRaw = dto.SupplierTaxNumber?.Trim();
            invoice.PoNumberRaw = dto.NoPurchaseOrder ? null : dto.PoNumber?.Trim();
            invoice.NoPurchaseOrder = dto.NoPurchaseOrder;
            if (dto.NoPurchaseOrder)
            {
                invoice.PurchaseOrderId = null;
            }

            invoice.Currency = dto.Currency?.Trim().ToUpperInvariant();
            invoice.NetAmount = dto.NetAmount;
            invoice.TaxAmount = dto.TaxAmount;
            invoice.GrossAmount = dto.GrossAmount;

            HashSet<string> manualFields = InvoiceWorkflow.ParseManualFields(invoice.ManualEditedFieldsJson);
            foreach (string field in new[] { "InvoiceNumber", "InvoiceDate", "SupplierName", "SupplierTaxNumber", "PoNumber", "Currency", "NetAmount", "TaxAmount", "GrossAmount" })
            {
                manualFields.Add(field);
            }

            invoice.ManualEditedFieldsJson = JsonSerializer.Serialize(manualFields);
            invoice.ReviewedAt = DateTime.UtcNow;
            invoice.ReviewedByUserId = request.UserId;

            // The current extraction rows mirror the reviewed header.
            List<InvoiceExtData> extractionRows = await _repository.InvoiceExtData.GetTrackedByDocumentAsync(invoice.DocumentId, cancellationToken);
            if (extractionRows.Count == 0)
            {
                InvoiceExtData row = new InvoiceExtData
                {
                    Id = Guid.NewGuid(),
                    DocumentId = invoice.DocumentId,
                    OrganizationId = invoice.OrganizationId,
                    OperatingUnitId = invoice.OperatingUnitId,
                    SourceProvider = "REVIEW"
                };
                _repository.InvoiceExtData.Create(row);
                extractionRows.Add(row);
            }

            foreach (InvoiceExtData row in extractionRows)
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
            }

            AuditTrail.Add(_repository, invoice.OrganizationId, invoice.OperatingUnitId, request.UserId, "INVOICE_REVIEW_SAVED", "Invoice", invoice.Id, invoice.InvoiceNumber);
            await _repository.SaveAsync();

            InvoiceResponseDto result = await ResponseBuilder.InvoiceAsync(_repository, invoice, cancellationToken);
            _logger.LogInfo($"Invoice updated. InvoiceId: {invoice.Id}, OrganizationId: {request.OrganizationId}");
            return result;
        }
    }
}
