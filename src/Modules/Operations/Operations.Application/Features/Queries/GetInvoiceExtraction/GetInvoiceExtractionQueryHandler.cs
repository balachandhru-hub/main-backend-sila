using System.Globalization;
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

namespace Operations.Application.Features.Queries.GetInvoiceExtraction
{
    public class GetInvoiceExtractionQueryHandler : IRequestHandler<GetInvoiceExtractionQuery, InvoiceExtractionResponseDto>
    {
        private readonly IRepositoryWrapper _repository;
        private readonly ILoggerManager _logger;

        public GetInvoiceExtractionQueryHandler(IRepositoryWrapper repository, ILoggerManager logger)
        {
            _repository = repository;
            _logger = logger;
        }

        public async Task<InvoiceExtractionResponseDto> Handle(GetInvoiceExtractionQuery request, CancellationToken cancellationToken)
        {
            _logger.LogInfo($"Fetching invoice extraction. InvoiceId: {request.InvoiceId}, OrganizationId: {request.OrganizationId}");

            Invoice? invoice = await _repository.Invoice
                .FindByCondition(x => x.Id == request.InvoiceId && x.OrganizationId == request.OrganizationId)
                .FirstOrDefaultAsync(cancellationToken);
            if (invoice == null)
            {
                _logger.LogError($"Invoice not found. InvoiceId: {request.InvoiceId}, OrganizationId: {request.OrganizationId}");
                throw new NotFoundCustomException("Invoice not found.", "The invoice does not exist in your organization.");
            }

            DocumentExtraction? extraction = await _repository.DocumentExtraction
                .FindByCondition(x => x.DocumentId == invoice.DocumentId && x.ExtractionType == ExtractionType.FULL_INVOICE)
                .OrderByDescending(x => x.DateCreated)
                .FirstOrDefaultAsync(cancellationToken);
            List<InvoiceExtData> rows = await _repository.InvoiceExtData
                .FindByCondition(x => x.DocumentId == invoice.DocumentId)
                .ToListAsync(cancellationToken);

            List<InvoiceExtractionLineResponseDto> lines;
            if (rows.Count > 0)
            {
                lines = rows
                    .OrderBy(row => int.TryParse(row.LineItemNumber, NumberStyles.Integer, CultureInfo.InvariantCulture, out int number) ? number : 0)
                    .Select(row => new InvoiceExtractionLineResponseDto
                    {
                        ItemSkuId = row.ItemSkuId,
                        ItemAmount = row.ItemAmount,
                        ItemNet = row.ItemNet,
                        ItemDescription = row.ItemDescription,
                        LineItemNumber = row.LineItemNumber,
                        PurchaseOrderItemId = row.PurchaseOrderItemId
                    }).ToList();
            }
            else
            {
                List<InvoiceLine> invoiceLines = await _repository.InvoiceLine
                    .FindByCondition(x => x.InvoiceId == invoice.Id)
                    .OrderBy(x => x.LineNumber)
                    .ToListAsync(cancellationToken);
                lines = invoiceLines.Select(line => new InvoiceExtractionLineResponseDto
                {
                    ItemSkuId = line.MaterialCodeRaw,
                    ItemAmount = line.LineAmount,
                    ItemNet = line.LineAmount,
                    ItemDescription = line.DescriptionRaw,
                    LineItemNumber = line.LineNumber.ToString(CultureInfo.InvariantCulture),
                    PurchaseOrderItemId = line.PurchaseOrderItemId
                }).ToList();
            }

            _logger.LogInfo($"Invoice extraction fetched. InvoiceId: {invoice.Id}, Lines: {lines.Count}");
            return new InvoiceExtractionResponseDto
            {
                Header = new InvoiceExtractionHeaderResponseDto
                {
                    DocumentId = invoice.DocumentId,
                    SupplierName = invoice.SupplierNameRaw,
                    SupplierTrn = invoice.SupplierTaxNumberRaw,
                    SupplierInvoiceNumber = InvoiceWorkflow.IsPendingNumber(invoice.InvoiceNumber) ? null : invoice.InvoiceNumber,
                    InvoiceDate = invoice.InvoiceDate,
                    PurchaseOrderNumber = invoice.PoNumberRaw,
                    InvoiceGross = invoice.GrossAmount,
                    InvoiceNet = invoice.NetAmount,
                    Currency = invoice.Currency
                },
                Lines = lines,
                Provider = extraction?.Provider ?? Common.OCR_PROVIDER_BUILT_IN,
                ExtractionMethod = (extraction?.ExtractionMethod ?? ExtractionMethod.PDF_TEXT).ToString(),
                Confidence = extraction?.Confidence ?? invoice.OverallConfidence,
                FallbackUsed = extraction?.FallbackUsed ?? false,
                Status = (extraction?.Status ?? ProcessingStatus.FAILED).ToString(),
                CompletedAt = extraction?.ProcessingCompletedAt
            };
        }
    }
}
