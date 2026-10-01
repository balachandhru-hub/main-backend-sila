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

namespace Operations.Application.Features.Commands.MatchInvoicePurchaseOrder
{
    public class MatchInvoicePurchaseOrderCommandHandler : IRequestHandler<MatchInvoicePurchaseOrderCommand, InvoiceResponseDto>
    {
        private readonly IRepositoryWrapper _repository;
        private readonly ILoggerManager _logger;

        public MatchInvoicePurchaseOrderCommandHandler(IRepositoryWrapper repository, ILoggerManager logger)
        {
            _repository = repository;
            _logger = logger;
        }

        public async Task<InvoiceResponseDto> Handle(MatchInvoicePurchaseOrderCommand request, CancellationToken cancellationToken)
        {
            _logger.LogInfo($"Matching invoice purchase order. InvoiceId: {request.InvoiceId}, PurchaseOrderId: {request.Request.PurchaseOrderId}, OrganizationId: {request.OrganizationId}");

            Invoice? invoice = await _repository.Invoice.GetTrackedAsync(request.InvoiceId, request.OrganizationId, cancellationToken);
            if (invoice == null)
            {
                _logger.LogError($"Invoice not found. InvoiceId: {request.InvoiceId}, OrganizationId: {request.OrganizationId}");
                throw new NotFoundCustomException("Invoice not found.", "The invoice does not exist in your organization.");
            }

            // Matching a purchase order ends the "no purchase order" state of the invoice.
            invoice.NoPurchaseOrder = false;
            if (!string.IsNullOrWhiteSpace(request.Request.PoNumber))
            {
                invoice.PoNumberRaw = request.Request.PoNumber.Trim();
            }

            if (request.Request.PurchaseOrderId != null)
            {
                Guid purchaseOrderId = request.Request.PurchaseOrderId.Value;
                PurchaseOrder? order = await _repository.PurchaseOrder
                    .FindByCondition(x => x.Id == purchaseOrderId && x.OrganizationId == request.OrganizationId)
                    .FirstOrDefaultAsync(cancellationToken);
                if (order == null)
                {
                    _logger.LogError($"Purchase order not found. PurchaseOrderId: {purchaseOrderId}, OrganizationId: {request.OrganizationId}");
                    throw new NotFoundCustomException("Purchase order not found.", "The purchase order does not exist in your organization.");
                }

                invoice.PurchaseOrderId = order.Id;
                invoice.PoNumberRaw = order.PoNumber;
            }
            else
            {
                await InvoiceWorkflow.MatchPurchaseOrderAsync(_repository, invoice, cancellationToken);
            }

            List<InvoiceLine> lines = await _repository.InvoiceLine.GetTrackedByInvoiceAsync(invoice.Id, cancellationToken);
            await InvoiceWorkflow.MatchLinesAsync(_repository, invoice, lines, cancellationToken);
            invoice.Status = InvoiceWorkflow.ResolveStatus(invoice, lines);
            AuditTrail.Add(_repository, invoice.OrganizationId, invoice.OperatingUnitId, request.UserId, "INVOICE_PO_MATCHED", "Invoice", invoice.Id, invoice.PoNumberRaw,
                invoice.PurchaseOrderId == null ? "NO_MATCH" : "SUCCESS");
            await _repository.SaveAsync();

            InvoiceResponseDto result = await ResponseBuilder.InvoiceAsync(_repository, invoice, cancellationToken);
            _logger.LogInfo($"Invoice purchase order matched. InvoiceId: {invoice.Id}, PurchaseOrderId: {invoice.PurchaseOrderId}, Status: {invoice.Status}");
            return result;
        }
    }
}
