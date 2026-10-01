using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Operations.Application.Features.Shared;
using Operations.Application.Services.Integration;
using Operations.Domain.Common;
using Operations.Domain.Dtos;
using Operations.Domain.Entities;
using Operations.Domain.Enums;
using Operations.Infrastructure.Contracts.IRepository;
using SharedKernel.ExceptionHandler;
using SharedKernel.LoggerServices;

namespace Operations.Application.Features.Commands.RetryGoodsReceipt
{
    public class RetryGoodsReceiptCommandHandler : IRequestHandler<RetryGoodsReceiptCommand, GoodsReceiptResponseDto>
    {
        private readonly IRepositoryWrapper _repository;
        private readonly ILoggerManager _logger;
        private readonly IIntegrationHttpExecutor _executor;

        public RetryGoodsReceiptCommandHandler(
            IRepositoryWrapper repository,
            ILoggerManager logger,
            IIntegrationHttpExecutor executor)
        {
            _repository = repository;
            _logger = logger;
            _executor = executor;
        }

        public async Task<GoodsReceiptResponseDto> Handle(RetryGoodsReceiptCommand request, CancellationToken cancellationToken)
        {
            _logger.LogInfo($"Retrying goods receipt. GoodsReceiptId: {request.GoodsReceiptId}, OrganizationId: {request.OrganizationId}, UserId: {request.UserId}");

            GoodsReceipt? receipt = await _repository.GoodsReceipt.FindFirstByConditionAsync(
                x => x.Id == request.GoodsReceiptId && x.OrganizationId == request.OrganizationId);
            if (receipt == null)
            {
                _logger.LogError($"Goods receipt not found. GoodsReceiptId: {request.GoodsReceiptId}, OrganizationId: {request.OrganizationId}");
                throw new NotFoundCustomException("Goods receipt not found.", "The goods receipt does not exist in your organization.");
            }

            if (receipt.Status == GoodsReceiptStatus.POSTED)
            {
                _logger.LogInfo($"Goods receipt is already posted. GoodsReceiptId: {receipt.Id}");
                return await ResponseBuilder.GoodsReceiptAsync(_repository, receipt, cancellationToken);
            }

            PurchaseOrder? order = await _repository.PurchaseOrder.FindFirstByConditionAsync(
                x => x.Id == receipt.PurchaseOrderId && x.OrganizationId == request.OrganizationId);
            if (order == null)
            {
                _logger.LogError($"Purchase order of the goods receipt not found. GoodsReceiptId: {receipt.Id}, PurchaseOrderId: {receipt.PurchaseOrderId}");
                throw new NotFoundCustomException("Purchase order not found.", "The purchase order of this goods receipt no longer exists.");
            }

            await using IDbContextTransaction transaction = await _repository.BeginTransactionAsync(cancellationToken);
            await _repository.PurchaseOrderItem.LockForOrderAsync(order.Id, cancellationToken);
            List<PurchaseOrderItem> items = await _repository.PurchaseOrderItem.GetTrackedByOrderAsync(order.Id, cancellationToken);
            List<GoodsReceiptLine> lines = await _repository.GoodsReceiptLine.GetTrackedByReceiptAsync(receipt.Id, cancellationToken);
            foreach (GoodsReceiptLine line in lines)
            {
                PurchaseOrderItem? item = items.FirstOrDefault(candidate => candidate.Id == line.PurchaseOrderItemId);
                if (item == null)
                {
                    _logger.LogError($"Goods receipt line no longer belongs to the purchase order. GoodsReceiptId: {receipt.Id}, PurchaseOrderItemId: {line.PurchaseOrderItemId}");
                    throw new BadRequestCustomException("Goods receipt cannot be retried.", "A saved receipt line no longer belongs to its purchase order.");
                }

                if (line.ReceivedQuantity > item.OpenQuantity)
                {
                    _logger.LogError($"Open quantity changed since the goods receipt was saved. GoodsReceiptId: {receipt.Id}, PurchaseOrderItemId: {item.Id}");
                    throw new ConflictCustomException("Open quantity changed.", $"The remaining quantity for {item.Description} changed. The goods receipt was kept for review.");
                }
            }

            Invoice? invoice = receipt.InvoiceId == null
                ? null
                : await _repository.Invoice.GetTrackedAsync(receipt.InvoiceId.Value, request.OrganizationId, cancellationToken);
            receipt.Status = GoodsReceiptStatus.POSTING;
            receipt.ErpPostingStatus = Common.ERP_STATUS_RETRYING;
            ErpGoodsReceiptResult erp = await GoodsReceiptWorkflow.PostToErpAsync(_repository, _executor, request.OrganizationId, order.EntityCode, receipt, lines, cancellationToken);
            GoodsReceiptWorkflow.ApplyErpResult(receipt, erp);
            if (!erp.Success)
            {
                AuditTrail.Add(_repository, request.OrganizationId, receipt.OperatingUnitId, request.UserId, "GRN_RETRY_FAILED", "GoodsReceipt", receipt.Id, receipt.GrnNumber, receipt.Status.ToString());
                await _repository.SaveAsync();
                await transaction.CommitAsync(cancellationToken);
                _logger.LogError($"ERP did not accept the goods receipt on retry. GoodsReceiptId: {receipt.Id}, Status: {receipt.Status}, Code: {erp.ErrorCode}");
                return await ResponseBuilder.GoodsReceiptAsync(_repository, receipt, cancellationToken);
            }

            await GoodsReceiptWorkflow.ApplyPostedAsync(_repository, receipt, lines, order, items, invoice, erp, cancellationToken);
            AuditTrail.Add(_repository, request.OrganizationId, receipt.OperatingUnitId, request.UserId, "GRN_RETRY_POSTED", "GoodsReceipt", receipt.Id, receipt.GrnNumber, "SUCCESS");
            await _repository.SaveAsync();
            await transaction.CommitAsync(cancellationToken);

            _logger.LogInfo($"Goods receipt posted on retry. GoodsReceiptId: {receipt.Id}, OrganizationId: {request.OrganizationId}");
            return await ResponseBuilder.GoodsReceiptAsync(_repository, receipt, cancellationToken);
        }
    }
}
