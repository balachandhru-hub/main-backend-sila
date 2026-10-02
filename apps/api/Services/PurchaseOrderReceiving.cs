using SilaMe.Api.Models;

namespace SilaMe.Api.Services;

public static class PurchaseOrderReceiving
{
    public static bool IsUnscopedEntity(string? entityCode) =>
        string.IsNullOrWhiteSpace(entityCode) ||
        string.Equals(entityCode, "ALL", StringComparison.OrdinalIgnoreCase) ||
        string.Equals(entityCode, "DEFAULT", StringComparison.OrdinalIgnoreCase);

    public static bool MatchesRequestedEntity(string? requestedEntityCode, string poEntityCode) =>
        IsUnscopedEntity(requestedEntityCode) ||
        string.Equals(poEntityCode, requestedEntityCode, StringComparison.OrdinalIgnoreCase) ||
        string.Equals(poEntityCode, "ALL", StringComparison.OrdinalIgnoreCase);

    public static bool IsHeaderOpen(PurchaseOrder po) =>
        po.Status is not PurchaseOrderStatus.CLOSED and not PurchaseOrderStatus.CANCELLED;

    public static decimal RemainingQuantity(PurchaseOrderItem item, decimal postedAcceptedQuantity)
    {
        var remaining = item.OrderedQuantity - Math.Max(0, postedAcceptedQuantity);
        return remaining < 0 ? 0 : remaining;
    }

    public static bool IsGoodsReceiptEligible(PurchaseOrderItem item) =>
        item.GoodsReceiptExpected &&
        !item.DeletionIndicator &&
        !item.DeliveryCompleted &&
        item.OpenQuantity > 0 &&
        item.Status is not PurchaseOrderItemStatus.CLOSED and not PurchaseOrderItemStatus.CANCELLED;

    public static bool IsOpenForReceiving(PurchaseOrder po) =>
        IsHeaderOpen(po) && po.Items.Any(IsGoodsReceiptEligible);

    public static bool IsServiceOnly(PurchaseOrder po)
    {
        var live = po.Items.Where(item => !item.DeletionIndicator).ToList();
        return live.Count > 0 && live.All(item => !item.GoodsReceiptExpected);
    }

    public static string? IneligibilityReason(PurchaseOrder po)
    {
        if (po.Status == PurchaseOrderStatus.CLOSED) return "PO CLOSED";
        if (po.Status == PurchaseOrderStatus.CANCELLED) return "PO CANCELLED";
        if (string.Equals(po.PoCategory, "SERVICE", StringComparison.OrdinalIgnoreCase) || IsServiceOnly(po))
            return "SERVICE PO";
        var live = po.Items.Where(item => !item.DeletionIndicator).ToList();
        if (live.Count > 0 && live.All(item => !item.GoodsReceiptExpected))
            return "GR NOT EXPECTED";
        if (live.Count > 0 && live.All(item => item.DeliveryCompleted || item.OpenQuantity <= 0 || item.Status == PurchaseOrderItemStatus.CLOSED))
            return "PO FULLY RECEIVED";
        if (!IsOpenForReceiving(po)) return "GR NOT EXPECTED";
        return null;
    }
}
