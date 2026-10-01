using Microsoft.EntityFrameworkCore;
using Operations.Application.Services.Integration;
using Operations.Domain.Common;
using Operations.Domain.Dtos;
using Operations.Domain.Entities;
using Operations.Domain.Enums;
using Operations.Infrastructure.Contracts.IRepository;

namespace Operations.Application.Features.Shared
{
    /// <summary>
    /// Rules shared by validating, posting and retrying a goods receipt: the validation errors,
    /// the ERP post, and the purchase order / stock update of a posted receipt.
    /// </summary>
    internal static class GoodsReceiptWorkflow
    {
        public static List<string> Validate(Invoice? invoice, List<InvoiceLine> invoiceLines, PurchaseOrder? order, List<PurchaseOrderItem> items, ValidateGrnRequestDto request)
        {
            List<string> errors = new List<string>();
            if (invoice == null)
            {
                errors.Add("The invoice was not found.");
            }

            if (order == null)
            {
                errors.Add("The purchase order was not found.");
            }

            if (invoice == null || order == null)
            {
                return errors;
            }

            if (invoice.PurchaseOrderId != order.Id)
            {
                errors.Add("The invoice and purchase order do not belong together.");
            }

            if (order.Status is PurchaseOrderStatus.CLOSED or PurchaseOrderStatus.CANCELLED)
            {
                errors.Add("The purchase order is not open for receiving.");
            }

            if (invoice.InvoiceType == InvoiceType.SERVICE)
            {
                errors.Add("Service invoices cannot create a goods receipt.");
            }

            if (invoiceLines.Any(line => line.MatchStatus == InvoiceLineMatchStatus.UNMATCHED))
            {
                errors.Add("Every material invoice line must be matched before posting.");
            }

            foreach (GrnLineInputDto input in request.Lines)
            {
                PurchaseOrderItem? item = items.FirstOrDefault(line => line.Id == input.PurchaseOrderItemId);
                if (item == null)
                {
                    errors.Add("A receipt line does not belong to the purchase order.");
                    continue;
                }

                if (input.ReceivedQuantity < 0 || input.AcceptedQuantity < 0 || input.DamagedQuantity < 0 || input.RejectedQuantity < 0)
                {
                    errors.Add($"Quantities for {item.Description} cannot be negative.");
                }

                if (input.AcceptedQuantity + input.DamagedQuantity + input.RejectedQuantity > input.ReceivedQuantity)
                {
                    errors.Add($"Accepted, damaged, and rejected quantities cannot exceed received quantity for {item.Description}.");
                }

                if (input.ReceivedQuantity > item.OpenQuantity)
                {
                    errors.Add($"Received quantity exceeds the remaining PO quantity for {item.Description}.");
                }
            }

            if (request.Lines.GroupBy(line => line.PurchaseOrderItemId).Any(group => group.Count() > 1))
            {
                errors.Add("A purchase order item can appear only once in a goods receipt.");
            }

            return errors;
        }

        /// <summary>
        /// Posts the receipt to the ERP configured for the organization and entity (process POST_GRN).
        /// Without an active configuration the receipt is posted locally only.
        /// </summary>
        public static async Task<ErpGoodsReceiptResult> PostToErpAsync(
            IRepositoryWrapper repository,
            IIntegrationHttpExecutor executor,
            Guid organizationId,
            string entityCode,
            GoodsReceipt receipt,
            List<GoodsReceiptLine> lines,
            CancellationToken cancellationToken)
        {
            ApiIntegrationConfiguration? configuration = await repository.ApiIntegrationConfiguration
                .FindByCondition(x => x.OrganizationId == organizationId
                    && x.ProcessType == IntegrationProcessType.POST_GRN
                    && (x.EntityCode == entityCode || x.EntityCode == Common.ENTITY_CODE_ALL)
                    && x.Status == IntegrationConfigurationStatus.ACTIVE)
                .OrderBy(x => x.EntityCode == Common.ENTITY_CODE_ALL)
                .FirstOrDefaultAsync(cancellationToken);
            if (configuration == null)
            {
                return new ErpGoodsReceiptResult { Configured = false, Success = true };
            }

            return await executor.PostGoodsReceiptAsync(configuration, receipt, lines, cancellationToken);
        }

        public static void ApplyErpResult(GoodsReceipt receipt, ErpGoodsReceiptResult erp)
        {
            receipt.ErpAttemptCount++;
            receipt.LastErpAttemptAt = DateTime.UtcNow;
            receipt.ErpResponseJson = erp.ResponseJson;
            receipt.ErpMaterialDocument = erp.MaterialDocument;
            receipt.ErpDocumentYear = erp.DocumentYear;
            if (erp.Success)
            {
                return;
            }

            // Unknown = the ERP may have booked it (timeout / network): the receipt needs reconciliation.
            receipt.Status = erp.Unknown ? GoodsReceiptStatus.UNKNOWN : GoodsReceiptStatus.FAILED;
            receipt.BusinessStatus = erp.Unknown ? Common.GRN_BUSINESS_RECONCILIATION : Common.GRN_BUSINESS_ERP_FAILED;
            receipt.ErpPostingStatus = erp.Unknown ? Common.ERP_STATUS_UNKNOWN : Common.ERP_STATUS_FAILED;
            receipt.FailureCode = erp.ErrorCode;
            receipt.FailureMessage = erp.ErrorMessage;
        }

        /// <summary>
        /// Books a receipt the ERP accepted: purchase order quantities and status, stock balances,
        /// inventory transactions, the invoice status and the receipt itself. The handler saves.
        /// </summary>
        public static async Task ApplyPostedAsync(
            IRepositoryWrapper repository,
            GoodsReceipt receipt,
            List<GoodsReceiptLine> lines,
            PurchaseOrder order,
            List<PurchaseOrderItem> items,
            Invoice? invoice,
            ErpGoodsReceiptResult erp,
            CancellationToken cancellationToken)
        {
            foreach (GoodsReceiptLine line in lines)
            {
                PurchaseOrderItem item = items.First(candidate => candidate.Id == line.PurchaseOrderItemId);
                item.ReceivedQuantity += line.ReceivedQuantity;
                item.OpenQuantity -= line.ReceivedQuantity;
                item.Status = item.OpenQuantity <= 0 ? PurchaseOrderItemStatus.CLOSED : PurchaseOrderItemStatus.PARTIALLY_RECEIVED;
                if (line.AcceptedQuantity <= 0)
                {
                    continue;
                }

                StockBalance? stock = await repository.StockBalance.FindFirstByConditionAsync(x =>
                    x.OrganizationId == receipt.OrganizationId && x.OperatingUnitId == receipt.OperatingUnitId
                    && x.MaterialCode == item.MaterialCode && x.Uom == item.Uom);
                if (stock == null)
                {
                    stock = new StockBalance
                    {
                        Id = Guid.NewGuid(),
                        OrganizationId = receipt.OrganizationId,
                        OperatingUnitId = receipt.OperatingUnitId,
                        MaterialId = item.MaterialId,
                        MaterialCode = item.MaterialCode,
                        Uom = item.Uom
                    };
                    repository.StockBalance.Create(stock);
                }

                stock.Quantity += line.AcceptedQuantity;
                repository.InventoryTransaction.Create(new InventoryTransaction
                {
                    Id = Guid.NewGuid(),
                    OrganizationId = receipt.OrganizationId,
                    OperatingUnitId = receipt.OperatingUnitId,
                    MaterialId = item.MaterialId,
                    MaterialCode = item.MaterialCode,
                    TransactionType = InventoryTransactionType.GRN_RECEIPT,
                    ReferenceType = Common.GRN_REFERENCE_TYPE,
                    ReferenceId = receipt.Id,
                    Quantity = line.AcceptedQuantity,
                    Uom = item.Uom
                });
            }

            order.Status = items.All(item => item.Status == PurchaseOrderItemStatus.CLOSED)
                ? PurchaseOrderStatus.CLOSED
                : PurchaseOrderStatus.PARTIALLY_RECEIVED;
            order.TotalReceivedQuantity = items.Sum(item => item.ReceivedQuantity);
            if (invoice != null)
            {
                invoice.Status = InvoiceStatus.GRN_POSTED;
            }

            receipt.Status = GoodsReceiptStatus.POSTED;
            receipt.BusinessStatus = Common.GRN_BUSINESS_POSTED;
            receipt.ErpPostingStatus = erp.Configured ? Common.ERP_STATUS_POSTED : Common.ERP_STATUS_NOT_CONFIGURED;
            receipt.PostedAt = DateTime.UtcNow;
            receipt.ErpPostedAt = erp.Configured ? DateTime.UtcNow : null;
            receipt.FailureCode = null;
            receipt.FailureMessage = null;
        }
    }
}
