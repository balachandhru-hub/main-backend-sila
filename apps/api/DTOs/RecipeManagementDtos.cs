using SilaMe.Api.Models;

namespace SilaMe.Api.DTOs;

public sealed record MaterialValuationRecord(
    string? CompanyCode, string ValuationArea, string? ValuationClass, string? PriceControl,
    decimal? StandardPrice, decimal? MovingAveragePrice, string? Currency,
    string? Plant = null, string? PriceUom = null, DateTime? EffectiveFrom = null, DateTime? EffectiveTo = null);

public sealed record MaterialNormalizedRecord(
    string MaterialCode,
    string Name,
    string? Description,
    string? MaterialType,
    string? MaterialGroup,
    string? Category,
    string BaseUom,
    string? AlternateUom,
    string? CompanyCode,
    string? ValuationArea,
    string? ValuationClass,
    string? PriceControl,
    decimal? StandardPrice,
    decimal? MovingAveragePrice,
    string? Currency,
    decimal? UnitCost,
    MaterialAcquisitionSource Source,
    string? SourceSystem,
    DateTime? SourceLastChangedAt,
    bool? Active = true,
    IReadOnlyList<MaterialValuationRecord>? Valuations = null,
    decimal? ConvFactor = null,
    string? ConvUnit = null,
    decimal? ConvValue = null,
    bool InventoryItem = false,
    string InventoryType = "NON_STOCK",
    bool BatchManaged = false,
    bool ExpiryManaged = false,
    int? ShelfLifeDays = null,
    bool SerialManaged = false);

public sealed record MaterialMasterRow(
    Guid Id, string MaterialCode, string Name, string Description, string? MaterialGroup, string? MaterialType,
    string? Category, string BaseUom, string? AlternateUom, string? CompanyCode, string? ValuationArea,
    string? ValuationClass, string? PriceControl, decimal? StandardPrice, decimal? MovingAveragePrice,
    decimal? UnitCost, string? Currency, string Source, string ApprovalStatus, string ActiveStatus,
    string? SourceSystem, DateTime UpdatedAt, DateTime? LastSynchronizedAt, Guid? PendingChangeRequestId,
    IReadOnlyList<MaterialValuationRecord> Valuations, bool HasValidUnitPrice,
    string PriceStatus, string? PackSummary, IReadOnlyList<MaterialUomConversionRow> Conversions,
    decimal? ProposedUnitPrice, string PriceStatusLabel, string? PriceUom, DateTime? PriceEffectiveFrom,
    string? Plant, bool HasPendingPriceChange, decimal? ApprovedUnitPrice, string? SupplierSummary,
    decimal? ConvFactor, string? ConvUnit, decimal? ConvValue, string? ConversionText,
    bool InventoryItem = false, string InventoryType = "NON_STOCK", bool BatchManaged = false,
    bool ExpiryManaged = false, int? ShelfLifeDays = null, bool SerialManaged = false);

public sealed record MaterialSupplierFacet(Guid Id, string Code, string Name);
public sealed record MaterialSearchFacets(
    IReadOnlyList<string> Categories, IReadOnlyList<string> MaterialGroups, IReadOnlyList<string> MaterialTypes,
    IReadOnlyList<MaterialSupplierFacet> Suppliers);

public sealed record MaterialPriceUpdateRequest(
    decimal UnitPrice, string? Currency, string? PriceUom, string? CompanyCode, string? ValuationArea,
    string? PriceControl, string? Comment, DateTime? EffectiveFrom, string? Plant);

public sealed record MaterialUomConversionRow(
    Guid Id, Guid MaterialId, string MaterialCode, string FromUom, string ToUom, decimal Numerator, decimal Denominator,
    decimal? PackSize, string? PackUom, string? Source, bool IsActive);

public sealed record MaterialUomConversionUpsertRequest(
    Guid MaterialId, string FromUom, string ToUom, decimal Numerator, decimal Denominator,
    decimal? PackSize, string? PackUom, bool? IsActive);

public sealed record UomMasterRow(Guid Id, string Code, string Name, string Dimension, string Status);
public sealed record UomMasterUpsertRequest(string Code, string Name, string? Dimension, string? Status);

public sealed record PageResult<T>(IReadOnlyList<T> Items, string? NextCursor, int PageSize, int ReturnedCount);

public sealed record MaterialUpsertRequest(
    string MaterialCode, string Name, string? Description, string? MaterialGroup, string? MaterialType,
    string? Category, string BaseUom, string? AlternateUom, string? CompanyCode, string? ValuationArea,
    string? ValuationClass, string? PriceControl, decimal? StandardPrice, decimal? MovingAveragePrice,
    decimal? UnitCost, string? Currency, bool? Active, decimal? ConvFactor, string? ConvUnit, decimal? ConvValue,
    bool? InventoryItem = null, string? InventoryType = null, bool? BatchManaged = null, bool? ExpiryManaged = null,
    int? ShelfLifeDays = null, bool? SerialManaged = null);

public sealed record MaterialImportPreviewResponse(
    string FileName, int TotalRows, int ValidRows, int InvalidRows, int NewRows, int ChangedRows, int UnchangedRows,
    IReadOnlyList<MaterialImportRowResponse> Rows);

public sealed record MaterialImportRowResponse(
    int RowNumber, bool IsValid, string Action, IReadOnlyDictionary<string, string?> Values, IReadOnlyList<string> Errors);

public sealed record MaterialImportCommitRequest(IReadOnlyList<int>? RowNumbers);

public sealed record MaterialErpRouteResponse(
    string CompanyCode, string SystemKind, string ConfigurationName, Guid ConfigurationId, Guid RouteId,
    DateTime? LastSyncAt, string? LastStatus);

public sealed record MaterialErpPullRequest(string CompanyCode);

public sealed record MaterialErpPullResult(
    string CompanyCode, string SystemKind, string ConfigurationName, int RecordsRead, int NewCount, int ChangedCount,
    int UnchangedCount, int FailedCount, IReadOnlyList<string> Failures, DateTime SyncedAt);

public sealed record MaterialApprovalTaskRow(
    Guid Id, Guid ChangeRequestId, Guid? MaterialId, string MaterialCode, string Event, string Source, int Level,
    string? RoleKey, string Status, string? Comment, DateTime SubmittedAt, DateTime? ActionAt);

public sealed record RecipeCategoryRow(Guid Id, string Code, string Name, string? Description, string Status);
public sealed record RecipeCategoryUpsertRequest(string Code, string Name, string? Description, string? Status);
public sealed record RecipeFamilyRow(Guid Id, string Code, string Name, string? Description, string Status);
public sealed record RecipeFamilyUpsertRequest(string Code, string Name, string? Description, string? Status);
public sealed record RecipeLocationRow(Guid Id, string Kind, string Code, string Name, string? Description, string Status);
public sealed record RecipeLocationUpsertRequest(string Kind, string Code, string Name, string? Description, string? Status);
public sealed record RecipeLocationAssignmentRow(Guid LocationId, string Kind, string Code, string Name);

public sealed record RecipeListRow(
    Guid Id, string RecipeCode, string Title, string? PosCode, string? PosItem, string ItemMode,
    decimal ServingQty, string ServingUom, decimal YieldQty, string YieldUom, string? Family, string? Category,
    decimal? MenuPrice, decimal? RecipeCost, decimal? CostPerServing, decimal? CostPercent, decimal? MarginAmount,
    decimal? MarginPercent, string Status, DateTime UpdatedAt, string? Currency, int CurrentVersionNumber,
    Guid? ActiveVersionId, string Readiness, int ReadinessIssueCount);

public sealed record RecipeIngredientInput(
    Guid? MaterialId, string? ErpMaterialId, string? UnmappedIngredientName, string? MaterialDescription, decimal Quantity, string Uom,
    decimal WastagePercent, decimal YieldPercent, decimal? UnitCost, int Sequence, string? PreparationNotes);

public sealed record RecipeIngredientRow(
    Guid Id, string? RecipeIngredientId, Guid? MaterialId, string? ErpMaterialId, string? MaterialCode, string? MaterialName,
    string? UnmappedIngredientName, string? MaterialDescription, string? MaterialGroup, decimal Quantity, string Uom,
    decimal WastagePercent, decimal YieldPercent, decimal EffectiveQuantity, decimal? UnitCost, bool PriceUnavailable,
    decimal? IngredientCost, decimal? PercentageOfTotalCost, int Sequence, string? PreparationNotes,
    string? BaseUom, string? PackSummary, decimal? ConsumptionQuantity, string? ConsumptionUom, bool ConversionMissing,
    string PriceStatus, string PriceStatusLabel, decimal? ProposedUnitPrice, bool CanUpdatePrice,
    IReadOnlyList<MaterialUomConversionRow> Conversions);

public sealed record RecipeCostingSummary(
    decimal? RecipeCost, decimal? CostPerServing, decimal? CostPercent, decimal? MarginAmount, decimal? MarginPercent,
    decimal ServingQty, string ServingUom, decimal YieldQty, string YieldUom, string Currency,
    decimal IngredientCost, decimal TotalPosItemCost, decimal? PercentageOfTotalCost, decimal? PosItemCostPercentage,
    bool CostingComplete, int MissingPriceCount, int PendingPriceCount, string? CostingMessage);

public sealed record RecipeReadinessIssue(string MaterialCode, string Description, string Code, string Message);
public sealed record RecipeReadinessInfo(string Status, int IssueCount, IReadOnlyList<RecipeReadinessIssue> Issues, bool ReadyForApproval);

public sealed record RecipeVersionDetail(
    Guid Id, Guid RecipeId, int VersionNumber, string RecipeCode, string Title, string? Description, Guid? CategoryId, string? Category,
    Guid? FamilyId, string? Family, string ItemMode, string? Cuisine, string? RecipeType, decimal ServingUnit, string ServingUom,
    decimal YieldQuantity, string YieldUom, decimal? PortionSize, string? PortionUom, string? PosCode, string? PosItem,
    decimal? PosItemMenuPrice, string? Currency, int? PreparationMinutes, int? CookingMinutes, string? ImageUrl,
    string? PreparationInstructions, string? ChefNotes, string Status, DateTime? EffectiveFrom, DateTime? EffectiveTo,
    Guid? ApprovedByUserId, DateTime? ApprovedAt, IReadOnlyList<RecipeIngredientRow> Ingredients, RecipeCostingSummary Costing,
    RecipeReadinessInfo Readiness, DateOnly? LastSaleDate, IReadOnlyList<RecipeLocationAssignmentRow> Locations);

public sealed record RecipeUpsertRequest(
    string? RecipeCode, string? Title, string? Name, string? Description, Guid? CategoryId, string? Category, Guid? FamilyId, string? Family,
    string? ItemMode, string? Cuisine, string? RecipeType, decimal? ServingUnit, decimal? ServingSize, string? ServingUom,
    decimal? YieldQuantity, string? YieldUom, decimal? PortionSize, string? PortionUom, string? PosCode, string? PosItem,
    decimal? PosItemMenuPrice, DateOnly? LastSaleDate, int? PreparationMinutes, int? CookingMinutes, string? ImageUrl,
    string? PreparationInstructions, string? ChefNotes, IReadOnlyList<RecipeIngredientInput> Ingredients,
    IReadOnlyList<Guid>? LocationIds = null);

public sealed record RecipeImportPreviewResponse(
    string FileName, int TotalRows, int ValidRows, int InvalidRows, int RecipeCount,
    IReadOnlyList<MaterialImportRowResponse> Rows);

public sealed record RecipeApprovalLevelInput(int Level, string RoleKey, string Label);
public sealed record RecipeApprovalWorkflowRow(
    Guid Id, string Event, int LevelCount, IReadOnlyList<RecipeApprovalLevelInput> Levels);
public sealed record RecipeApprovalWorkflowUpsertRequest(string Event, IReadOnlyList<RecipeApprovalLevelInput> Levels);
public sealed record RecipeApprovalActionRow(
    Guid Id, Guid RecipeId, Guid RecipeVersionId, int VersionNumber, string RecipeName, string Event, int Level,
    string? RoleKey, string Status, string? Comment, DateTime SubmittedAt, DateTime? ActionAt);
public sealed record RecipeApprovalDecisionRequest(string Comment);

public sealed record PosSourceRow(
    Guid Id, string Name, string PosSystem, string IntegrationKind, string Status, Guid? ApiIntegrationConfigurationId,
    string? DatabaseType, string? Host, int? Port, string? DatabaseName, string? SchemaName, string? TableOrView,
    string? CredentialReference, bool HasPassword, string? TransactionIdField, string? BusinessDateField,
    string? OutletField, string? ItemCodeField, string? QuantityField, string? StatusField);
public sealed record PosSourceUpsertRequest(
    string Name, string PosSystem, string IntegrationKind, string? Status, Guid? ApiIntegrationConfigurationId,
    string? DatabaseType, string? Host, int? Port, string? DatabaseName, string? SchemaName, string? TableOrView,
    string? CredentialReference, string? Password, string? TransactionIdField, string? BusinessDateField,
    string? OutletField, string? ItemCodeField, string? QuantityField, string? StatusField);
public sealed record PosOutletMappingRow(
    Guid Id, Guid PosSourceId, string PosOutletCode, string? PosOutletName, string PropertyCode, string OutletCode,
    string PlantCode, string StorageLocationCode, string? CompanyCode,
    string? LocationType = null, string? LocationName = null);
public sealed record PosOutletMappingUpsertRequest(
    string PosOutletCode, string? PosOutletName, string? PropertyCode, string OutletCode, string? PlantCode = null,
    string? StorageLocationCode = null, string? CompanyCode = null);
public sealed record PosItemRecipeMappingRow(
    Guid Id, Guid PosSourceId, string PosItemCode, string? PosItemDescription, Guid RecipeId, string RecipeCode, string RecipeName);
public sealed record PosItemRecipeMappingUpsertRequest(string PosItemCode, string? PosItemDescription, Guid RecipeId);

public sealed record OutletMenuItemRow(
    Guid Id, Guid OutletMappingId, string OutletCode, string? OutletName, Guid RecipeId, string RecipeCode, string Title,
    string? PosCode, string? PosItem, decimal? PosItemMenuPrice);
public sealed record OutletMenuItemUpsertRequest(Guid RecipeId);

public sealed record PosSaleIntakeRequest(
    Guid PosSourceId, string SourceTransactionId, int? SourceLineNumber, DateOnly? BusinessDate, DateTime? TransactionAt,
    string PosOutletCode, string PosItemCode, string? PosItemDescription, decimal QuantitySold, decimal? Amount, string? RawReference,
    string? SaleUom = null, string? Currency = null);

public sealed record PosSalesImportRow(
    int RowNumber, bool IsValid, DateOnly? BusinessDate, string? TransactionId, int? LineId, string? PosCode, decimal? Qty, string? Uom, string? OutletId, string? Currency,
    IReadOnlyList<string> Errors, string Status = "READY", string? ErrorCode = null, string? Message = null);
public sealed record PosSalesImportPreview(
    string FileName, int TotalRows, int ValidRows, int InvalidRows, IReadOnlyList<PosSalesImportRow> Rows,
    Guid? BatchId = null, int DuplicateRows = 0, int UnmappedPosCodes = 0, int InvalidOutlets = 0, int InvalidUom = 0,
    int RecipeNotReady = 0, int ReadyToProcess = 0);
public sealed record PosSalesImportResult(
    int Processed, int Posted, int Failed, int AlreadyPosted, IReadOnlyList<string> Failures,
    Guid? BatchId = null, int Duplicates = 0, int PostingUnknown = 0);
public sealed record PosSalesUploadListRow(
    Guid Id, string FileName, string? BusinessDateRange, Guid? UploadedByUserId, DateTime UploadedAt,
    int Rows, int Valid, int Processed, int Duplicates, int Failed, int PostingUnknown, string Status);
public sealed record PosSalesUploadDetail(
    PosSalesUploadListRow Header,
    IReadOnlyList<PosSalesUploadLineRow> Lines);
public sealed record PosSalesUploadLineRow(
    Guid Id, int RowNumber, DateOnly? BusinessDate, string? TransactionId, int? LineId, string? PosCode,
    string? MenuItem, decimal? Qty, string? Uom, string? OutletId, int? RecipeVersion, string Status,
    string? Message, Guid? ConsumptionTransactionId);

public sealed record RecipeTransactionListRow(
    Guid Id, string SourceSystem, string SourceTransactionId, DateOnly? BusinessDate, string? PropertyCode, string Outlet,
    string? RecipeCode, int? RecipeVersionNumber, decimal QuantitySold, int IngredientLines, string? CompanyCode,
    string? PlantCode, string? StorageLocationCode, string? IntegrationSystem, string Status, string? ExternalReference,
    DateTime CreatedAt, DateTime? ProcessedAt, bool CanReprocess,
    string? LocationType = null, string? LocationName = null, string? LocationCode = null,
    string? PosItemCode = null, int SourceLineNumber = 1, string? SapMovementType = null, string? MaterialDocument = null,
    string? Step1Status = null, string? Step1Message = null, string? Step2Status = null, string? Step2Message = null,
    int? ErpHttpStatus = null, string? ErpResponse = null, string? FailureMessage = null);

public sealed record RecipeTransactionDetail(
    RecipeTransactionListRow Header,
    IReadOnlyList<RecipeConsumptionLineRow> Lines,
    IReadOnlyList<RecipeTransactionEventRow> Timeline,
    string? FailureCode, string? FailureMessage, string? FailedStep, DateTime? FailureAt,
    string? ErpRequest = null, string? ErpResponse = null, int? ErpHttpStatus = null);

public sealed record RecipeConsumptionLineRow(
    Guid Id, string? MaterialCode, string? Description, decimal ConsumedQuantity, string Uom, decimal? UnitCost);
public sealed record RecipeTransactionEventRow(Guid Id, string Step, string Status, string? Detail, DateTime CreatedAt);

public sealed record RecipeDashboardResponse(
    int RecipeCount, int ActiveVersionCount, int PendingApprovalCount, int MaterialCount, int FailedTransactionCount,
    int PostedConsumptionCount = 0);
