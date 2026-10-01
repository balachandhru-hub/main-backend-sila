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

namespace Operations.Application.Features.Queries.ValidateGoodsReceipt
{
    public class ValidateGoodsReceiptQueryHandler : IRequestHandler<ValidateGoodsReceiptQuery, GrnValidationResponseDto>
    {
        private readonly IRepositoryWrapper _repository;
        private readonly ILoggerManager _logger;

        public ValidateGoodsReceiptQueryHandler(IRepositoryWrapper repository, ILoggerManager logger)
        {
            _repository = repository;
            _logger = logger;
        }

        public async Task<GrnValidationResponseDto> Handle(ValidateGoodsReceiptQuery request, CancellationToken cancellationToken)
        {
            _logger.LogInfo($"Validating goods receipt. InvoiceId: {request.Request.InvoiceId}, PurchaseOrderId: {request.Request.PurchaseOrderId}, OrganizationId: {request.OrganizationId}");

            ValidateGrnRequestDto dto = request.Request;
            await OperationsScope.EnsureUnitAsync(_repository, _logger, request.OrganizationId, dto.OperatingUnitId, cancellationToken);
            Invoice? invoice = await _repository.Invoice
                .FindByCondition(x => x.Id == dto.InvoiceId && x.OrganizationId == request.OrganizationId)
                .FirstOrDefaultAsync(cancellationToken);
            PurchaseOrder? order = await _repository.PurchaseOrder
                .FindByCondition(x => x.Id == dto.PurchaseOrderId && x.OrganizationId == request.OrganizationId)
                .FirstOrDefaultAsync(cancellationToken);
            List<InvoiceLine> invoiceLines = invoice == null
                ? new List<InvoiceLine>()
                : await _repository.InvoiceLine.FindByCondition(x => x.InvoiceId == invoice.Id).ToListAsync(cancellationToken);
            List<PurchaseOrderItem> items = order == null
                ? new List<PurchaseOrderItem>()
                : await _repository.PurchaseOrderItem.FindByCondition(x => x.PurchaseOrderId == order.Id).ToListAsync(cancellationToken);

            List<string> errors = GoodsReceiptWorkflow.Validate(invoice, invoiceLines, order, items, dto);
            _logger.LogInfo($"Goods receipt validated. Valid: {errors.Count == 0}, Errors: {errors.Count}");
            return new GrnValidationResponseDto { Valid = errors.Count == 0, Errors = errors };
        }
    }
}
