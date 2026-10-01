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

namespace Operations.Application.Features.Queries.GetBasicExtraction
{
    public class GetBasicExtractionQueryHandler : IRequestHandler<GetBasicExtractionQuery, BasicInvoiceExtractionResponseDto>
    {
        private readonly IRepositoryWrapper _repository;
        private readonly ILoggerManager _logger;

        public GetBasicExtractionQueryHandler(IRepositoryWrapper repository, ILoggerManager logger)
        {
            _repository = repository;
            _logger = logger;
        }

        public async Task<BasicInvoiceExtractionResponseDto> Handle(GetBasicExtractionQuery request, CancellationToken cancellationToken)
        {
            _logger.LogInfo($"Fetching basic extraction. InvoiceId: {request.InvoiceId}, OrganizationId: {request.OrganizationId}");

            Invoice? invoice = await _repository.Invoice
                .FindByCondition(x => x.Id == request.InvoiceId && x.OrganizationId == request.OrganizationId)
                .FirstOrDefaultAsync(cancellationToken);
            if (invoice == null)
            {
                _logger.LogError($"Invoice not found. InvoiceId: {request.InvoiceId}, OrganizationId: {request.OrganizationId}");
                throw new NotFoundCustomException("Invoice not found.", "The invoice does not exist in your organization.");
            }

            return new BasicInvoiceExtractionResponseDto
            {
                DocumentId = invoice.DocumentId,
                SupplierName = invoice.SupplierNameRaw,
                SupplierTrn = invoice.SupplierTaxNumberRaw,
                SupplierInvoiceNumber = InvoiceWorkflow.IsPendingNumber(invoice.InvoiceNumber) ? null : invoice.InvoiceNumber,
                InvoiceDate = invoice.InvoiceDate,
                PurchaseOrderNumber = invoice.PoNumberRaw,
                InvoiceGross = invoice.GrossAmount,
                Currency = invoice.Currency,
                Status = invoice.Status
            };
        }
    }
}
