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

namespace Operations.Application.Features.Commands.MatchInvoiceLines
{
    public class MatchInvoiceLinesCommandHandler : IRequestHandler<MatchInvoiceLinesCommand, InvoiceResponseDto>
    {
        private readonly IRepositoryWrapper _repository;
        private readonly ILoggerManager _logger;

        public MatchInvoiceLinesCommandHandler(IRepositoryWrapper repository, ILoggerManager logger)
        {
            _repository = repository;
            _logger = logger;
        }

        public async Task<InvoiceResponseDto> Handle(MatchInvoiceLinesCommand request, CancellationToken cancellationToken)
        {
            _logger.LogInfo($"Matching invoice lines. InvoiceId: {request.InvoiceId}, Lines: {request.Request.Lines.Count}, OrganizationId: {request.OrganizationId}");

            Invoice? invoice = await _repository.Invoice.GetTrackedAsync(request.InvoiceId, request.OrganizationId, cancellationToken);
            if (invoice == null)
            {
                _logger.LogError($"Invoice not found. InvoiceId: {request.InvoiceId}, OrganizationId: {request.OrganizationId}");
                throw new NotFoundCustomException("Invoice not found.", "The invoice does not exist in your organization.");
            }

            if (invoice.PurchaseOrderId == null)
            {
                _logger.LogError($"Invoice has no purchase order. InvoiceId: {invoice.Id}");
                throw new BadRequestCustomException("Purchase order is required.", "Select a purchase order before matching lines.");
            }

            List<Guid> itemIds = await _repository.PurchaseOrderItem
                .FindByCondition(x => x.PurchaseOrderId == invoice.PurchaseOrderId)
                .Select(x => x.Id)
                .ToListAsync(cancellationToken);
            List<InvoiceLine> lines = await _repository.InvoiceLine.GetTrackedByInvoiceAsync(invoice.Id, cancellationToken);
            foreach (InvoiceLineMatchInputDto match in request.Request.Lines)
            {
                InvoiceLine? line = lines.FirstOrDefault(item => item.Id == match.InvoiceLineId);
                if (line == null)
                {
                    _logger.LogError($"Invoice line not found. InvoiceLineId: {match.InvoiceLineId}, InvoiceId: {invoice.Id}");
                    throw new NotFoundCustomException("Invoice line not found.", "The invoice line does not belong to this invoice.");
                }

                bool matched = match.PurchaseOrderItemId != null && itemIds.Contains(match.PurchaseOrderItemId.Value);
                line.PurchaseOrderItemId = matched ? match.PurchaseOrderItemId : null;
                line.MatchStatus = matched ? InvoiceLineMatchStatus.MATCHED : InvoiceLineMatchStatus.UNMATCHED;
            }

            invoice.Status = InvoiceWorkflow.ResolveStatus(invoice, lines);
            await _repository.SaveAsync();

            InvoiceResponseDto result = await ResponseBuilder.InvoiceAsync(_repository, invoice, cancellationToken);
            _logger.LogInfo($"Invoice lines matched. InvoiceId: {invoice.Id}, Status: {invoice.Status}");
            return result;
        }
    }
}
