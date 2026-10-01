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

namespace Operations.Application.Features.Commands.PostGoodsReceipt
{
    public class PostGoodsReceiptCommandHandler : IRequestHandler<PostGoodsReceiptCommand, GoodsReceiptResponseDto>
    {
        private readonly IRepositoryWrapper _repository;
        private readonly ILoggerManager _logger;
        private readonly IIntegrationHttpExecutor _executor;

        public PostGoodsReceiptCommandHandler(
            IRepositoryWrapper repository,
            ILoggerManager logger,
            IIntegrationHttpExecutor executor)
        {
            _repository = repository;
            _logger = logger;
            _executor = executor;
        }

        public async Task<GoodsReceiptResponseDto> Handle(PostGoodsReceiptCommand request, CancellationToken cancellationToken)
        {
            _logger.LogInfo($"Posting goods receipt. InvoiceId: {request.Request.InvoiceId}, PurchaseOrderId: {request.Request.PurchaseOrderId}, OrganizationId: {request.OrganizationId}, UserId: {request.UserId}");

            PostGrnRequestDto dto = request.Request;
            if (string.IsNullOrWhiteSpace(request.IdempotencyKey))
            {
                _logger.LogError($"Idempotency key is missing. OrganizationId: {request.OrganizationId}, UserId: {request.UserId}");
                throw new BadRequestCustomException("Idempotency key is required.", $"Send the {Common.IDEMPOTENCY_HEADER} header when posting a goods receipt.");
            }

            string idempotencyKey = request.IdempotencyKey.Trim();
            Invoice? invoice = await _repository.Invoice.GetTrackedAsync(dto.InvoiceId, request.OrganizationId, cancellationToken);
            if (invoice == null)
            {
                _logger.LogError($"Invoice not found. InvoiceId: {dto.InvoiceId}, OrganizationId: {request.OrganizationId}");
                throw new NotFoundCustomException("Invoice not found.", "The invoice does not exist in your organization.");
            }

            await OperationsScope.EnsureUnitAsync(_repository, _logger, request.OrganizationId, dto.OperatingUnitId, cancellationToken);

            IdempotencyRecord? existing = await _repository.IdempotencyRecord
                .FindByCondition(x => x.OrganizationId == request.OrganizationId && x.UserId == request.UserId
                    && x.IdempotencyKey == idempotencyKey && x.Operation == Common.OPERATION_POST_GRN)
                .FirstOrDefaultAsync(cancellationToken);
            if (existing != null)
            {
                // The same request was already handled: its goods receipt is returned, nothing is posted twice.
                GoodsReceipt? previous = Guid.TryParse(existing.ResponseReference, out Guid previousId)
                    ? await _repository.GoodsReceipt.FindByCondition(x => x.Id == previousId && x.OrganizationId == request.OrganizationId).FirstOrDefaultAsync(cancellationToken)
                    : null;
                if (previous == null)
                {
                    _logger.LogError($"Goods receipt posting is already in progress. OrganizationId: {request.OrganizationId}, UserId: {request.UserId}");
                    throw new ConflictCustomException("Posting in progress.", "This posting request is already in progress.");
                }

                _logger.LogInfo($"Goods receipt posting replayed. GoodsReceiptId: {previous.Id}");
                return await ResponseBuilder.GoodsReceiptAsync(_repository, previous, cancellationToken);
            }

            PurchaseOrder? order = await _repository.PurchaseOrder.FindFirstByConditionAsync(
                x => x.Id == dto.PurchaseOrderId && x.OrganizationId == request.OrganizationId);
            List<InvoiceLine> invoiceLines = await _repository.InvoiceLine
                .FindByCondition(x => x.InvoiceId == invoice.Id)
                .ToListAsync(cancellationToken);

            // The purchase order items stay locked from the open-quantity check until the stock is
            // updated, so two receipts can never consume the same open quantity.
            await using IDbContextTransaction transaction = await _repository.BeginTransactionAsync(cancellationToken);
            if (order != null)
            {
                await _repository.PurchaseOrderItem.LockForOrderAsync(order.Id, cancellationToken);
            }

            List<PurchaseOrderItem> items = order == null
                ? new List<PurchaseOrderItem>()
                : await _repository.PurchaseOrderItem.GetTrackedByOrderAsync(order.Id, cancellationToken);
            List<string> errors = GoodsReceiptWorkflow.Validate(invoice, invoiceLines, order, items, dto);
            if (errors.Count > 0 || order == null)
            {
                _logger.LogError($"Goods receipt validation failed. InvoiceId: {invoice.Id}, Errors: {string.Join(" ", errors)}");
                throw new BadRequestCustomException("Goods receipt validation failed.", string.Join(" ", errors));
            }

            GoodsReceipt receipt = new GoodsReceipt
            {
                Id = Guid.NewGuid(),
                OrganizationId = request.OrganizationId,
                OperatingUnitId = dto.OperatingUnitId,
                GrnNumber = $"GRN-{DateTime.UtcNow:yyyyMMdd}-{Guid.NewGuid():N}"[..24].ToUpperInvariant(),
                PurchaseOrderId = order.Id,
                InvoiceId = invoice.Id,
                SupplierId = order.SupplierId,
                ReceiptDate = dto.ReceiptDate ?? DateTime.UtcNow,
                Status = GoodsReceiptStatus.POSTING,
                PostingProvider = Common.GRN_POSTING_PROVIDER,
                BusinessStatus = Common.GRN_BUSINESS_RECEIVING,
                ErpPostingStatus = Common.ERP_STATUS_PENDING
            };
            _repository.GoodsReceipt.Create(receipt);
            List<GoodsReceiptLine> lines = dto.Lines.Select(input =>
            {
                PurchaseOrderItem item = items.First(candidate => candidate.Id == input.PurchaseOrderItemId);
                return new GoodsReceiptLine
                {
                    Id = Guid.NewGuid(),
                    GoodsReceiptId = receipt.Id,
                    PurchaseOrderItemId = item.Id,
                    MaterialId = item.MaterialId,
                    MaterialCode = item.MaterialCode,
                    Description = item.Description,
                    OpenQuantityBefore = item.OpenQuantity,
                    InvoiceQuantity = invoiceLines.FirstOrDefault(line => line.PurchaseOrderItemId == item.Id)?.Quantity,
                    ReceivedQuantity = input.ReceivedQuantity,
                    AcceptedQuantity = input.AcceptedQuantity,
                    DamagedQuantity = input.DamagedQuantity,
                    RejectedQuantity = input.RejectedQuantity,
                    Uom = item.Uom,
                    BatchNumber = input.BatchNumber,
                    ExpiryDate = input.ExpiryDate
                };
            }).ToList();
            _repository.GoodsReceiptLine.CreateRange(lines);
            IdempotencyRecord idempotency = new IdempotencyRecord
            {
                Id = Guid.NewGuid(),
                OrganizationId = request.OrganizationId,
                UserId = request.UserId,
                IdempotencyKey = idempotencyKey,
                Operation = Common.OPERATION_POST_GRN,
                ResponseReference = receipt.Id.ToString(),
                Status = IdempotencyStatus.STARTED
            };
            _repository.IdempotencyRecord.Create(idempotency);

            ErpGoodsReceiptResult erp = await GoodsReceiptWorkflow.PostToErpAsync(_repository, _executor, request.OrganizationId, order.EntityCode, receipt, lines, cancellationToken);
            GoodsReceiptWorkflow.ApplyErpResult(receipt, erp);
            if (!erp.Success)
            {
                // The receipt is kept (FAILED or UNKNOWN) so it can be reviewed and retried; no quantity is booked.
                idempotency.Status = IdempotencyStatus.FAILED;
                AuditTrail.Add(_repository, request.OrganizationId, dto.OperatingUnitId, request.UserId, "GRN_ERP_POST_FAILED", "GoodsReceipt", receipt.Id, receipt.GrnNumber, receipt.Status.ToString());
                await _repository.SaveAsync();
                await transaction.CommitAsync(cancellationToken);
                _logger.LogError($"ERP did not accept the goods receipt. GoodsReceiptId: {receipt.Id}, Status: {receipt.Status}, Code: {erp.ErrorCode}");
                return await ResponseBuilder.GoodsReceiptAsync(_repository, receipt, cancellationToken);
            }

            await GoodsReceiptWorkflow.ApplyPostedAsync(_repository, receipt, lines, order, items, invoice, erp, cancellationToken);
            idempotency.Status = IdempotencyStatus.COMPLETED;
            AuditTrail.Add(_repository, request.OrganizationId, dto.OperatingUnitId, request.UserId, "GRN_POSTED", "GoodsReceipt", receipt.Id, receipt.GrnNumber, "SUCCESS");
            await _repository.SaveAsync();
            await transaction.CommitAsync(cancellationToken);

            _logger.LogInfo($"Goods receipt posted. GoodsReceiptId: {receipt.Id}, GrnNumber: {receipt.GrnNumber}, OrganizationId: {request.OrganizationId}");
            return await ResponseBuilder.GoodsReceiptAsync(_repository, receipt, cancellationToken);
        }
    }
}
