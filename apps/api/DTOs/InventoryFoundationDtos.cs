using SilaMe.Api.Models;

namespace SilaMe.Api.DTOs;

public sealed record InventoryLocationRow(
    Guid Id, string LocationCode, string LocationName, string LocationType, Guid? ParentLocationId, string? ParentLocationName, string? ParentLocationCode,
    Guid? PropertyLocationId, string? PropertyCode, string? PropertyName, string? Description,
    bool InventoryEnabled, bool SalesEnabled, bool ConsumptionEnabled, bool TransferEnabled,
    string? CompanyCode, string? GeneralLedgerNumber, string? CostCenter, string? ProfitCenter,
    string? Currency, string? ManagerGroup, string Status);

public sealed record InventoryCodeName(string Code, string Name);
public sealed record InventoryLocationOptions(
    IReadOnlyList<InventoryCodeName> LocationTypes,
    IReadOnlyList<InventoryLocationRow> Parents,
    IReadOnlyList<InventoryCodeName> Properties,
    IReadOnlyList<InventoryCodeName> CompanyCodes,
    IReadOnlyList<InventoryCodeName> ManagerGroups);

public sealed record LocationUserAssignmentRow(Guid UserId, string DisplayName, string Email, Guid InventoryLocationId, string LocationCode, string LocationName, bool IsDefault);
public sealed record InventoryUserOption(Guid Id, string DisplayName, string Email);
public sealed record AssignLocationUserRequest(Guid UserId, bool IsDefault, bool Remove = false);

public sealed record InventoryLocationUpsertRequest(
    string LocationCode, string LocationName, string LocationType, string? ParentLocationCode, string? PropertyCode,
    string? Description, bool InventoryEnabled, bool SalesEnabled, bool ConsumptionEnabled, bool TransferEnabled,
    string? CompanyCode, string? GeneralLedgerNumber, string? CostCenter, string? ProfitCenter,
    string? Currency, string? ManagerGroup, bool Active);

public sealed record InventoryImportInvalid(int Row, string Code, string Message);
public sealed record InventoryImportSummary(string FileName, int TotalRows, int New, int Changed, int Unchanged, int Invalid, IReadOnlyList<InventoryImportInvalid> Errors);

public sealed record InventoryDashboardKpi(string Key, string Label, decimal? Value, string? Display, string? Note, bool Configured, string? Href);
public sealed record InventoryStockHealth(int Healthy, int LowStock, int OutOfStock, int ExcessStock, string NearExpiryNote, bool NearExpiryConfigured);
public sealed record InventoryActionItem(
    string Severity, string KindLabel, string Title, string? LocationName, string? MaterialCode, string? MaterialName,
    string? Impact, string? Recommendation, string PrimaryAction, string? PrimaryHref, Guid? AlertId, Guid? MaterialId,
    Guid? LocationId, Guid? TransferId, decimal? AvailableQty, decimal? ParLevel, decimal? SourceAvailable, string? SourceLocationName, string? Uom);
public sealed record ReplenishmentRecommendation(
    Guid MaterialId, Guid DestinationLocationId, string Destination, string MaterialCode, string MaterialName, string Uom,
    decimal AvailableQty, decimal? ReorderPoint, decimal? ParLevel, decimal RecommendedQty, Guid? SourceLocationId,
    string? SourceName, decimal? SourceAvailable, string Status, string Action);
public sealed record MovementBucket(string Key, string Label, int Movements, decimal? Value, string? Note, bool Configured);
public sealed record InventoryMovementToday(IReadOnlyList<MovementBucket> Buckets, string? Currency);
public sealed record TransferPipelineCount(string Key, string Label, int Count, string Href);
public sealed record TransferPipeline(IReadOnlyList<TransferPipelineCount> Stages);
public sealed record LocationValueShare(Guid Id, string LocationCode, string LocationName, string LocationType, decimal Value, decimal SharePercent, bool Aggregated);
public sealed record ConsumptionHighlight(Guid MaterialId, string MaterialCode, string Description, decimal Quantity, string Uom, decimal? Value, string? LocationName);
public sealed record InventoryDashboardResponse(
    IReadOnlyList<InventoryDashboardKpi> Kpis,
    IReadOnlyList<InventoryAlertRow> Alerts,
    IReadOnlyList<InventoryLocationRow> Locations,
    InventoryStockHealth Health,
    IReadOnlyList<InventoryActionItem> Actions,
    IReadOnlyList<ReplenishmentRecommendation> Replenishment,
    InventoryMovementToday Movement,
    TransferPipeline Transfers,
    IReadOnlyList<LocationValueShare> LocationValues,
    IReadOnlyList<ConsumptionHighlight> TopConsumption,
    string? Currency);

public sealed record InventoryAlertRow(
    Guid Id, string Kind, string Severity, string Status, string? Title, string? Message,
    Guid? InventoryLocationId, string? LocationName, Guid? MaterialId, string? MaterialCode,
    string? RecommendedAction, DateTime CreatedAt);

public sealed record InventoryAlertActionRequest(string Action, string? Comment);

public sealed record LiveMaterialHit(
    Guid Id, string MaterialCode, string Description, string BaseUom, decimal? UnitCost, string? Currency,
    string? PriceStatus, string? MaterialGroup, string? Category, decimal TotalAvailable);

public sealed record LiveAvailabilityRow(
    Guid InventoryLocationId, string LocationCode, string LocationName, string LocationType, string? PropertyCode,
    decimal OnHandQty, decimal ReservedQty, decimal AvailableQty, decimal InTransitQty, decimal TransferableQty,
    string TransferableBasis, string StockStatus, bool TransferEnabled, string StockingStatus = "NOT_STOCKED",
    string? StockingType = null);

public sealed record LiveInventoryDetail(
    LiveMaterialHit Material,
    IReadOnlyList<LiveAvailabilityRow> Availability,
    decimal RequiredQty,
    decimal LocalAvailable,
    decimal Shortage,
    string Recommendation,
    string RecommendationReason,
    IReadOnlyList<string> NextActions,
    string InventoryType = "NON_STOCK",
    bool InventoryItem = false,
    bool BatchManaged = false,
    bool ExpiryManaged = false,
    bool SerialManaged = false,
    string LocalStockingStatus = "NOT_STOCKED",
    bool NotStockedAtLocation = false);

public sealed record LiveDecisionRequest(decimal RequiredQty, Guid? CurrentLocationId);

public sealed record ItoListRow(
    Guid Id, string ItoNumber, string Mode, string FromLocation, string ToLocation, string TransferRelationship,
    DateTime RequestedAt, DateTime? RequiredBy, decimal TotalValue, string? Currency, string Status, string RequestedByName);

public sealed record ItoLineRow(
    Guid Id, Guid MaterialId, string MaterialCode, string Description, decimal RequestedQty, decimal ApprovedQty,
    decimal DispatchedQty, decimal ReceivedQty, decimal VarianceQty, string Uom, decimal? UnitCost, decimal TransferValue,
    decimal? SourceAvailable, decimal? SourceAfter);

public sealed record ItoApprovalRow(
    Guid Id, string Side, string Status, string? ManagerGroup, decimal? AvailableQty, decimal? RequestedQty,
    decimal? ApprovedQty, decimal? StockAfter, string? Comment, DateTime? ActedAt);

public sealed record ItoEventRow(Guid Id, string Action, string? Comment, DateTime CreatedAt);

public sealed record ItoDetail(
    Guid Id, string ItoNumber, string Status, string Mode, string TransferRelationship, bool AlreadyCollected,
    Guid FromInventoryLocationId, string FromLocation, string FromType, string? FromProperty,
    Guid ToInventoryLocationId, string ToLocation, string ToType, string? ToProperty,
    string? Reason, DateTime? RequiredBy, DateTime BusinessDate, decimal TotalValue, string? Currency,
    IReadOnlyList<ItoLineRow> Lines, IReadOnlyList<ItoApprovalRow> Approvals, IReadOnlyList<ItoEventRow> Events,
    IReadOnlyList<string> AllowedActions);

public sealed record ItoLineInput(Guid MaterialId, decimal Quantity, string? Uom, string? Comment);
public sealed record ItoCreateRequest(
    string Mode, Guid FromInventoryLocationId, Guid ToInventoryLocationId, string? Reason, DateTime? RequiredBy,
    bool AlreadyCollected, IReadOnlyList<ItoLineInput> Lines, bool OneTimeTransfer = false, bool AddToLocation = false);
public sealed record ItoApproveRequest(decimal? ApprovedQty, string? Comment, Guid? LineId);
public sealed record ItoQtyRequest(decimal Quantity, string? Comment, Guid? LineId, bool ConfirmAlreadyCollected = false);

public sealed record QuickTransferPolicyRow(
    bool Enabled, IReadOnlyList<string> AllowedSourceLocationTypes, IReadOnlyList<string> AllowedDestinationLocationTypes,
    decimal? MaximumQuantity, decimal? MaximumValue, bool SamePropertyAllowed, bool CrossPropertyAllowed,
    bool SourceConfirmationRequired, bool DestinationConfirmationRequired, bool ManagerNotification, bool SkipManagerApproval);

public sealed record PhysicalInventoryRequestInput(Guid InventoryLocationId, string Reason, string? Priority, bool SurpriseCount, string? AssignedGroup);
public sealed record InternalPurchaseRequestInput(Guid? InventoryLocationId, Guid MaterialId, decimal Quantity, string? Uom, string? Reason);
public sealed record PhysicalInventoryRequestRow(Guid Id, Guid InventoryLocationId, string LocationName, string Reason, string Priority, bool SurpriseCount, string Status, DateTime CreatedAt);
public sealed record InternalPurchaseRequestRow(Guid Id, Guid? MaterialId, decimal Quantity, string? Reason, string Status, DateTime CreatedAt);

public sealed record MobileInventoryHome(
    Guid? MyLocationId, string? MyLocationName, string? MyLocationType,
    int AvailableItems, int LowStock, int Incoming, int MyActions,
    int IncomingTransfers, int AwaitingReceipt, int Approvals, int CriticalAlerts);

public sealed record MyLocationRow(Guid Id, string LocationCode, string LocationName, string LocationType, bool IsDefault);
public sealed record OpeningStockRequest(Guid MaterialId, Guid InventoryLocationId, decimal Quantity, decimal? UnitCost, string? Comment);
public sealed record MaterialLocationRow(
    Guid Id, Guid MaterialId, string MaterialCode, string Description, Guid InventoryLocationId, string LocationCode, string LocationName,
    string StockingStatus, string StockingType, decimal? MinimumStock, decimal? MaximumStock, decimal? ReorderPoint,
    decimal? SafetyStock, decimal? ParLevel, Guid? PreferredSourceLocationId, string? ReplenishmentMethod, bool Active);
public sealed record MaterialLocationUpsertRequest(
    Guid MaterialId, Guid InventoryLocationId, string? StockingStatus, string? StockingType,
    decimal? MinimumStock, decimal? MaximumStock, decimal? ReorderPoint, decimal? SafetyStock, decimal? ParLevel,
    Guid? PreferredSourceLocationId, string? ReplenishmentMethod, bool? Active);
