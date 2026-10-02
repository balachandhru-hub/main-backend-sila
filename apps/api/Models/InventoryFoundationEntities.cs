namespace SilaMe.Api.Models;

public enum InventoryLocationType { PROPERTY, VENUE, STORE, OUTLET }
public enum InventoryItemType { STOCK, NON_STOCK, SERVICE }
public enum MaterialStockingStatus { ACTIVE, INACTIVE }
public enum MaterialStockingType { REGULAR, SEASONAL, TEMPORARY }
public enum InventoryTxnType
{
    OPENING_STOCK, GOODS_RECEIPT, TRANSFER_OUT, TRANSFER_IN, QUICK_TRANSFER_OUT, QUICK_TRANSFER_IN,
    GOODS_ISSUE, RECIPE_CONSUMPTION, DIRECT_CONSUMPTION, WASTE, DAMAGE, BREAKAGE, SPOILAGE, EXPIRED,
    STOCK_COUNT_ADJUSTMENT, MANUAL_ADJUSTMENT, RETURN_TO_SUPPLIER, RETURN_TO_STORE
}
public enum InventoryDirection { IN, OUT }
public enum InventoryTxnStatus { POSTED, REVERSED }
public enum ItoMode { STANDARD, QUICK }
public enum ItoStatus
{
    DRAFT, SUBMITTED, PENDING_APPROVAL, APPROVED, READY_TO_DISPATCH, DISPATCHED, IN_TRANSIT, RECEIVED, COMPLETED,
    REJECTED, CANCELLED, DISCREPANCY
}
public enum ItoApprovalSide { SOURCE, DESTINATION }
public enum ItoApprovalStatus { PENDING, APPROVED, REJECTED }
public enum InventoryAlertSeverity { CRITICAL, HIGH, MEDIUM, INFO }
public enum InventoryAlertStatus { NEW, ACKNOWLEDGED, ACTION_INITIATED, IN_PROGRESS, RESOLVED, CLOSED, DISMISSED }
public enum InventoryAlertKind
{
    LOW_STOCK, STOCKOUT_RISK, NEGATIVE_STOCK, INVENTORY_VARIANCE, TRANSFER_DISCREPANCY, EXPIRY_RISK,
    EXCESS_STOCK, REPEATED_QUICK_TRANSFER, HIGH_WASTE, HIGH_BREAKAGE
}
public enum InventoryNextActionType
{
    REQUEST_TRANSFER, QUICK_TRANSFER, CREATE_PR, REQUEST_PHYSICAL_INVENTORY, INVESTIGATE, MANAGER_REVIEW,
    REVIEW_TRANSFER, ACKNOWLEDGE, DISMISS
}
public enum PhysicalInventoryRequestStatus { REQUESTED, ASSIGNED, SCHEDULED, IN_PROGRESS, COMPLETED, CANCELLED }
public enum InternalPurchaseRequestStatus { DRAFT, SUBMITTED, READY_FOR_INTEGRATION, CANCELLED }

public sealed class InventoryLocation
{
    public Guid Id { get; set; }
    public Guid OrganizationId { get; set; }
    public required string LocationCode { get; set; }
    public required string LocationName { get; set; }
    public InventoryLocationType LocationType { get; set; }
    public Guid? ParentLocationId { get; set; }
    public Guid? PropertyLocationId { get; set; }
    public Guid? PropertyMasterId { get; set; }
    public Guid? CompanyCodeMasterId { get; set; }
    public string? Description { get; set; }
    public bool InventoryEnabled { get; set; }
    public bool SalesEnabled { get; set; }
    public bool ConsumptionEnabled { get; set; }
    public bool TransferEnabled { get; set; }
    public string? CompanyCode { get; set; }
    public string? GeneralLedgerNumber { get; set; }
    public string? CostCenter { get; set; }
    public string? ProfitCenter { get; set; }
    public string? Currency { get; set; }
    public string? ManagerGroup { get; set; }
    public StatusKind Status { get; set; } = StatusKind.ACTIVE;
    public DateTime CreatedAt { get; set; }
    public Guid? CreatedBy { get; set; }
    public DateTime UpdatedAt { get; set; }
    public Guid? UpdatedBy { get; set; }
    public Organization Organization { get; set; } = null!;
    public InventoryLocation? ParentLocation { get; set; }
    public InventoryLocation? PropertyLocation { get; set; }
}

public sealed class InventoryBalance
{
    public Guid Id { get; set; }
    public Guid OrganizationId { get; set; }
    public Guid MaterialId { get; set; }
    public Guid InventoryLocationId { get; set; }
    public Guid? BatchId { get; set; }
    public decimal OnHandQty { get; set; }
    public decimal ReservedQty { get; set; }
    public decimal AvailableQty { get; set; }
    public decimal InTransitQty { get; set; }
    public required string BaseUom { get; set; }
    public decimal InventoryValue { get; set; }
    public string? Currency { get; set; }
    public DateTime? LastMovementAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public Material Material { get; set; } = null!;
    public InventoryLocation InventoryLocation { get; set; } = null!;
}

public sealed class InventoryStockTransaction
{
    public Guid Id { get; set; }
    public Guid OrganizationId { get; set; }
    public required string TransactionId { get; set; }
    public InventoryTxnType TransactionType { get; set; }
    public Guid MaterialId { get; set; }
    public Guid InventoryLocationId { get; set; }
    public decimal Quantity { get; set; }
    public required string Uom { get; set; }
    public decimal BaseQuantity { get; set; }
    public required string BaseUom { get; set; }
    public InventoryDirection Direction { get; set; }
    public decimal? UnitCost { get; set; }
    public decimal? TransactionValue { get; set; }
    public string? Currency { get; set; }
    public Guid? BatchId { get; set; }
    public DateTime? ExpiryDate { get; set; }
    public string? ReferenceType { get; set; }
    public Guid? ReferenceId { get; set; }
    public DateTime BusinessDate { get; set; }
    public DateTime PostingDate { get; set; }
    public required string Source { get; set; }
    public InventoryTxnStatus Status { get; set; } = InventoryTxnStatus.POSTED;
    public Guid CreatedBy { get; set; }
    public DateTime CreatedAt { get; set; }
}

public sealed class InternalTransferOrder
{
    public Guid Id { get; set; }
    public Guid OrganizationId { get; set; }
    public required string ItoNumber { get; set; }
    public ItoMode Mode { get; set; }
    public Guid FromInventoryLocationId { get; set; }
    public Guid ToInventoryLocationId { get; set; }
    public Guid? FromPropertyId { get; set; }
    public Guid? ToPropertyId { get; set; }
    public string? Reason { get; set; }
    public DateTime? RequiredBy { get; set; }
    public DateTime BusinessDate { get; set; }
    public ItoStatus Status { get; set; } = ItoStatus.DRAFT;
    public Guid RequestedBy { get; set; }
    public DateTime RequestedAt { get; set; }
    public DateTime? ApprovedAt { get; set; }
    public Guid? DispatchedBy { get; set; }
    public DateTime? DispatchedAt { get; set; }
    public Guid? ReceivedBy { get; set; }
    public DateTime? ReceivedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
    public decimal TotalValue { get; set; }
    public string? Currency { get; set; }
    public bool AlreadyCollected { get; set; }
    public bool OneTimeTransfer { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public InventoryLocation FromLocation { get; set; } = null!;
    public InventoryLocation ToLocation { get; set; } = null!;
    public ICollection<InternalTransferLine> Lines { get; set; } = [];
    public ICollection<InternalTransferApproval> Approvals { get; set; } = [];
}

public sealed class InternalTransferLine
{
    public Guid Id { get; set; }
    public Guid ItoId { get; set; }
    public Guid MaterialId { get; set; }
    public decimal RequestedQty { get; set; }
    public decimal ApprovedQty { get; set; }
    public decimal DispatchedQty { get; set; }
    public decimal ReceivedQty { get; set; }
    public required string Uom { get; set; }
    public decimal BaseQty { get; set; }
    public required string BaseUom { get; set; }
    public decimal? UnitCost { get; set; }
    public decimal TransferValue { get; set; }
    public Guid? BatchId { get; set; }
    public DateTime? ExpiryDate { get; set; }
    public string? Comment { get; set; }
    public InternalTransferOrder Ito { get; set; } = null!;
    public Material Material { get; set; } = null!;
}

public sealed class InternalTransferApproval
{
    public Guid Id { get; set; }
    public Guid ItoId { get; set; }
    public ItoApprovalSide Side { get; set; }
    public string? ManagerGroup { get; set; }
    public ItoApprovalStatus Status { get; set; } = ItoApprovalStatus.PENDING;
    public decimal? AvailableQty { get; set; }
    public decimal? RequestedQty { get; set; }
    public decimal? ApprovedQty { get; set; }
    public decimal? StockAfter { get; set; }
    public string? Comment { get; set; }
    public Guid? ActorUserId { get; set; }
    public DateTime? ActedAt { get; set; }
    public InternalTransferOrder Ito { get; set; } = null!;
}

public sealed class InventoryWorkflowEvent
{
    public Guid Id { get; set; }
    public Guid OrganizationId { get; set; }
    public string ReferenceType { get; set; } = "ITO";
    public Guid ReferenceId { get; set; }
    public required string Action { get; set; }
    public string? Comment { get; set; }
    public Guid ActorUserId { get; set; }
    public DateTime CreatedAt { get; set; }
}

public sealed class InventoryAlert
{
    public Guid Id { get; set; }
    public Guid OrganizationId { get; set; }
    public InventoryAlertKind Kind { get; set; }
    public InventoryAlertSeverity Severity { get; set; }
    public InventoryAlertStatus Status { get; set; } = InventoryAlertStatus.NEW;
    public string? Title { get; set; }
    public string? Message { get; set; }
    public Guid? InventoryLocationId { get; set; }
    public Guid? MaterialId { get; set; }
    public Guid? ReferenceId { get; set; }
    public string? ReferenceType { get; set; }
    public InventoryNextActionType? RecommendedAction { get; set; }
    public Guid CreatedBy { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}

public sealed class PhysicalInventoryRequest
{
    public Guid Id { get; set; }
    public Guid OrganizationId { get; set; }
    public Guid InventoryLocationId { get; set; }
    public required string Reason { get; set; }
    public string Priority { get; set; } = "MEDIUM";
    public Guid RequestedBy { get; set; }
    public string? AssignedGroup { get; set; }
    public bool ManagerVisibility { get; set; } = true;
    public bool SurpriseCount { get; set; }
    public PhysicalInventoryRequestStatus Status { get; set; } = PhysicalInventoryRequestStatus.REQUESTED;
    public DateTime CreatedAt { get; set; }
}

public sealed class InternalPurchaseRequest
{
    public Guid Id { get; set; }
    public Guid OrganizationId { get; set; }
    public Guid? InventoryLocationId { get; set; }
    public Guid? MaterialId { get; set; }
    public decimal Quantity { get; set; }
    public string? Uom { get; set; }
    public string? Reason { get; set; }
    public InternalPurchaseRequestStatus Status { get; set; } = InternalPurchaseRequestStatus.SUBMITTED;
    public Guid RequestedBy { get; set; }
    public DateTime CreatedAt { get; set; }
}

public sealed class QuickTransferPolicy
{
    public Guid Id { get; set; }
    public Guid OrganizationId { get; set; }
    public bool Enabled { get; set; } = true;
    public string AllowedSourceLocationTypes { get; set; } = "PROPERTY,VENUE,STORE,OUTLET";
    public string AllowedDestinationLocationTypes { get; set; } = "PROPERTY,VENUE,STORE,OUTLET";
    public decimal? MaximumQuantity { get; set; }
    public decimal? MaximumValue { get; set; }
    public bool SamePropertyAllowed { get; set; } = true;
    public bool CrossPropertyAllowed { get; set; }
    public bool SourceConfirmationRequired { get; set; } = true;
    public bool DestinationConfirmationRequired { get; set; } = true;
    public bool ManagerNotification { get; set; } = true;
    public bool SkipManagerApproval { get; set; } = true;
    public DateTime UpdatedAt { get; set; }
}

public sealed class UserInventoryLocation
{
    public Guid Id { get; set; }
    public Guid OrganizationId { get; set; }
    public Guid UserId { get; set; }
    public Guid InventoryLocationId { get; set; }
    public bool IsDefault { get; set; }
    public DateTime CreatedAt { get; set; }
    public InventoryLocation InventoryLocation { get; set; } = null!;
}

public sealed class MaterialLocation
{
    public Guid Id { get; set; }
    public Guid OrganizationId { get; set; }
    public Guid MaterialId { get; set; }
    public Guid InventoryLocationId { get; set; }
    public MaterialStockingStatus StockingStatus { get; set; } = MaterialStockingStatus.ACTIVE;
    public MaterialStockingType StockingType { get; set; } = MaterialStockingType.REGULAR;
    public decimal? MinimumStock { get; set; }
    public decimal? MaximumStock { get; set; }
    public decimal? ReorderPoint { get; set; }
    public decimal? SafetyStock { get; set; }
    public decimal? ParLevel { get; set; }
    public Guid? PreferredSourceLocationId { get; set; }
    public string? ReplenishmentMethod { get; set; }
    public DateTime? EffectiveFrom { get; set; }
    public DateTime? EffectiveTo { get; set; }
    public bool Active { get; set; } = true;
    public Guid? CreatedBy { get; set; }
    public DateTime CreatedAt { get; set; }
    public Guid? UpdatedBy { get; set; }
    public DateTime UpdatedAt { get; set; }
    public Material Material { get; set; } = null!;
    public InventoryLocation InventoryLocation { get; set; } = null!;
    public InventoryLocation? PreferredSourceLocation { get; set; }
}
