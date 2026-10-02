namespace SilaMe.Api.Models;

public enum RecipeStatus { DRAFT, SUBMITTED, PENDING_APPROVAL, APPROVED, ACTIVE, REJECTED, INACTIVE }
public enum RecipeApprovalEvent { CREATE_RECIPE, CHANGE_RECIPE, CREATE_MATERIAL, CHANGE_MATERIAL }
public enum RecipeApprovalActionStatus { PENDING, APPROVED, REJECTED }
public enum PosIntegrationKind { API, DATABASE }
public enum PosSourceStatus { DRAFT, ACTIVE, INACTIVE }
public enum RecipeTransactionStatus
{
    RECEIVED, VALIDATING, RECIPE_MATCHED, RECIPE_EXPLODED, READY_TO_POST, POSTING, POSTED, FAILED, POSTING_UNKNOWN, REPROCESSING
}
public enum MaterialAcquisitionSource { ERP, MANUAL, EXCEL }
public enum MaterialGovernanceStatus { DRAFT, PENDING_APPROVAL, APPROVED, REJECTED, ACTIVE, INACTIVE }
public enum RecipeItemMode { DIRECT, RECIPE, BATCH_RECIPE }
public enum RecipeLocationKind { OUTLET, STORE, VENUE }
public enum UomDimension { MASS, VOLUME, COUNT, PACK, SERVING, OTHER }

public sealed class UomMaster
{
    public Guid Id { get; set; }
    public Guid OrganizationId { get; set; }
    public required string Code { get; set; }
    public required string Name { get; set; }
    public UomDimension Dimension { get; set; } = UomDimension.OTHER;
    public StatusKind Status { get; set; } = StatusKind.ACTIVE;
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public Organization Organization { get; set; } = null!;
}

public sealed class MaterialUomConversion
{
    public Guid Id { get; set; }
    public Guid MaterialId { get; set; }
    public required string FromUom { get; set; }
    public required string ToUom { get; set; }
    public decimal Numerator { get; set; } = 1;
    public decimal Denominator { get; set; } = 1;
    public decimal? PackSize { get; set; }
    public string? PackUom { get; set; }
    public string? Source { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public Material Material { get; set; } = null!;
}

public sealed class MaterialValuation
{
    public Guid Id { get; set; }
    public Guid MaterialId { get; set; }
    public string? CompanyCode { get; set; }
    public required string ValuationArea { get; set; }
    public string? Plant { get; set; }
    public string? ValuationClass { get; set; }
    public string? PriceControl { get; set; }
    public decimal? StandardPrice { get; set; }
    public decimal? MovingAveragePrice { get; set; }
    public string? Currency { get; set; }
    public string? PriceUom { get; set; }
    public DateTime? EffectiveFrom { get; set; }
    public DateTime? EffectiveTo { get; set; }
    public string? Source { get; set; }
    public DateTime UpdatedAt { get; set; }
    public Material Material { get; set; } = null!;
}

public sealed class MaterialChangeRequest
{
    public Guid Id { get; set; }
    public Guid OrganizationId { get; set; }
    public Guid? MaterialId { get; set; }
    public required string MaterialCode { get; set; }
    public RecipeApprovalEvent Event { get; set; }
    public MaterialAcquisitionSource Source { get; set; }
    public required string ProposedJson { get; set; }
    public MaterialGovernanceStatus Status { get; set; } = MaterialGovernanceStatus.PENDING_APPROVAL;
    public Guid? SubmittedByUserId { get; set; }
    public DateTime SubmittedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
    public string? Reason { get; set; }
    public decimal? CurrentUnitPrice { get; set; }
    public decimal? ProposedUnitPrice { get; set; }
    public string? PriceUom { get; set; }
    public string? Currency { get; set; }
    public DateTime? EffectiveFrom { get; set; }
    public Material? Material { get; set; }
    public ICollection<MaterialApprovalAction> Actions { get; set; } = [];
}

public sealed class MaterialApprovalAction
{
    public Guid Id { get; set; }
    public Guid ChangeRequestId { get; set; }
    public Guid? MaterialId { get; set; }
    public RecipeApprovalEvent Event { get; set; }
    public int Level { get; set; }
    public string? RoleKey { get; set; }
    public Guid? ApproverUserId { get; set; }
    public RecipeApprovalActionStatus Status { get; set; } = RecipeApprovalActionStatus.PENDING;
    public string? Comment { get; set; }
    public DateTime SubmittedAt { get; set; }
    public DateTime? ActionAt { get; set; }
    public MaterialChangeRequest ChangeRequest { get; set; } = null!;
}

public sealed class MaterialErpSyncState
{
    public Guid Id { get; set; }
    public Guid OrganizationId { get; set; }
    public required string CompanyCode { get; set; }
    public Guid? IntegrationRouteId { get; set; }
    public Guid? IntegrationConfigurationId { get; set; }
    public string? LastStatus { get; set; }
    public DateTime? LastSyncAt { get; set; }
    public int LastReadCount { get; set; }
    public int LastNewCount { get; set; }
    public int LastChangedCount { get; set; }
    public int LastUnchangedCount { get; set; }
    public int LastFailedCount { get; set; }
    public string? LastError { get; set; }
}

public sealed class RecipeFamily
{
    public Guid Id { get; set; }
    public Guid OrganizationId { get; set; }
    public required string Code { get; set; }
    public required string Name { get; set; }
    public string? Description { get; set; }
    public StatusKind Status { get; set; } = StatusKind.ACTIVE;
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public Organization Organization { get; set; } = null!;
}

public sealed class RecipeCategory
{
    public Guid Id { get; set; }
    public Guid OrganizationId { get; set; }
    public required string Code { get; set; }
    public required string Name { get; set; }
    public string? Description { get; set; }
    public StatusKind Status { get; set; } = StatusKind.ACTIVE;
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public Organization Organization { get; set; } = null!;
}

public sealed class Recipe
{
    public Guid Id { get; set; }
    public Guid OrganizationId { get; set; }
    public required string RecipeCode { get; set; }
    public required string Name { get; set; }
    public string? Description { get; set; }
    public Guid? CategoryId { get; set; }
    public Guid? FamilyId { get; set; }
    public string? Cuisine { get; set; }
    public string? RecipeType { get; set; }
    public string? Family { get; set; }
    public decimal? ServingSize { get; set; }
    public string? ServingUom { get; set; }
    public decimal? TotalServingQty { get; set; }
    public string? TotalServingUom { get; set; }
    public RecipeItemMode ItemMode { get; set; } = RecipeItemMode.RECIPE;
    public decimal YieldQty { get; set; } = 1;
    public string YieldUom { get; set; } = "EA";
    public string? Currency { get; set; }
    public string? PosCode { get; set; }
    public string? PosItem { get; set; }
    public decimal? PosItemMenuPrice { get; set; }
    public DateOnly? LastSaleDate { get; set; }
    public RecipeStatus Status { get; set; } = RecipeStatus.DRAFT;
    public int CurrentVersionNumber { get; set; } = 1;
    public Guid? ActiveVersionId { get; set; }
    public Guid CreatedByUserId { get; set; }
    public Guid UpdatedByUserId { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public Organization Organization { get; set; } = null!;
    public RecipeCategory? Category { get; set; }
    public RecipeFamily? RecipeFamily { get; set; }
    public ICollection<RecipeVersion> Versions { get; set; } = [];
    public ICollection<RecipeLocationAssignment> LocationAssignments { get; set; } = [];
}

public sealed class RecipeLocation
{
    public Guid Id { get; set; }
    public Guid OrganizationId { get; set; }
    public RecipeLocationKind Kind { get; set; } = RecipeLocationKind.OUTLET;
    public required string Code { get; set; }
    public required string Name { get; set; }
    public string? Description { get; set; }
    public StatusKind Status { get; set; } = StatusKind.ACTIVE;
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public Organization Organization { get; set; } = null!;
    public ICollection<RecipeLocationAssignment> Assignments { get; set; } = [];
}

public sealed class RecipeLocationAssignment
{
    public Guid Id { get; set; }
    public Guid RecipeId { get; set; }
    public Guid LocationId { get; set; }
    public DateTime CreatedAt { get; set; }
    public Recipe Recipe { get; set; } = null!;
    public RecipeLocation Location { get; set; } = null!;
}

public sealed class RecipeVersion
{
    public Guid Id { get; set; }
    public Guid OrganizationId { get; set; }
    public Guid RecipeId { get; set; }
    public int VersionNumber { get; set; }
    public required string Name { get; set; }
    public string? Description { get; set; }
    public Guid? CategoryId { get; set; }
    public string? Cuisine { get; set; }
    public string? RecipeType { get; set; }
    public decimal YieldQuantity { get; set; } = 1;
    public required string YieldUom { get; set; }
    public decimal? PortionSize { get; set; }
    public string? PortionUom { get; set; }
    public int? PreparationMinutes { get; set; }
    public int? CookingMinutes { get; set; }
    public string? ImageUrl { get; set; }
    public string? PreparationInstructions { get; set; }
    public string? ChefNotes { get; set; }
    public RecipeStatus Status { get; set; } = RecipeStatus.DRAFT;
    public DateTime? EffectiveFrom { get; set; }
    public DateTime? EffectiveTo { get; set; }
    public Guid CreatedByUserId { get; set; }
    public Guid? ApprovedByUserId { get; set; }
    public DateTime? ApprovedAt { get; set; }
    public decimal? SnapshotMenuPrice { get; set; }
    public decimal? SnapshotTotalRecipeCost { get; set; }
    public decimal? SnapshotCostPerServing { get; set; }
    public decimal? SnapshotCostPercent { get; set; }
    public decimal? SnapshotMarginAmount { get; set; }
    public decimal? SnapshotMarginPercent { get; set; }
    public DateTime? SnapshotCalculatedAt { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public Recipe Recipe { get; set; } = null!;
    public ICollection<RecipeIngredient> Ingredients { get; set; } = [];
    public ICollection<RecipeApprovalAction> ApprovalActions { get; set; } = [];
}

public sealed class RecipeIngredient
{
    public Guid Id { get; set; }
    public Guid RecipeVersionId { get; set; }
    public Guid? MaterialId { get; set; }
    public string? UnmappedIngredientName { get; set; }
    public string? MaterialDescription { get; set; }
    public decimal Quantity { get; set; }
    public required string Uom { get; set; }
    public decimal WastagePercent { get; set; }
    public decimal YieldPercent { get; set; } = 100;
    public decimal? UnitCost { get; set; }
    public string? ErpMaterialId { get; set; }
    public string? MaterialGroup { get; set; }
    public decimal? IngredientCost { get; set; }
    public decimal? PercentageOfTotalCost { get; set; }
    public string? RecipeIngredientId { get; set; }
    public decimal? ConsumptionQuantity { get; set; }
    public string? ConsumptionUom { get; set; }
    public int Sequence { get; set; }
    public string? PreparationNotes { get; set; }
    public RecipeVersion RecipeVersion { get; set; } = null!;
    public Material? Material { get; set; }
}

public sealed class RecipeApprovalWorkflow
{
    public Guid Id { get; set; }
    public Guid OrganizationId { get; set; }
    public RecipeApprovalEvent Event { get; set; }
    public int LevelCount { get; set; } = 1;
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public Organization Organization { get; set; } = null!;
    public ICollection<RecipeApprovalLevel> Levels { get; set; } = [];
}

public sealed class RecipeApprovalLevel
{
    public Guid Id { get; set; }
    public Guid WorkflowId { get; set; }
    public int Level { get; set; }
    public required string RoleKey { get; set; }
    public required string Label { get; set; }
    public RecipeApprovalWorkflow Workflow { get; set; } = null!;
}

public sealed class RecipeApprovalAction
{
    public Guid Id { get; set; }
    public Guid RecipeId { get; set; }
    public Guid RecipeVersionId { get; set; }
    public RecipeApprovalEvent Event { get; set; }
    public int Level { get; set; }
    public string? RoleKey { get; set; }
    public Guid? ApproverUserId { get; set; }
    public RecipeApprovalActionStatus Status { get; set; } = RecipeApprovalActionStatus.PENDING;
    public string? Comment { get; set; }
    public DateTime SubmittedAt { get; set; }
    public DateTime? ActionAt { get; set; }
    public Recipe Recipe { get; set; } = null!;
    public RecipeVersion RecipeVersion { get; set; } = null!;
}

public sealed class PosSource
{
    public Guid Id { get; set; }
    public Guid OrganizationId { get; set; }
    public required string Name { get; set; }
    public required string PosSystem { get; set; }
    public PosIntegrationKind IntegrationKind { get; set; }
    public PosSourceStatus Status { get; set; } = PosSourceStatus.DRAFT;
    public Guid? ApiIntegrationConfigurationId { get; set; }
    public string? DatabaseType { get; set; }
    public string? Host { get; set; }
    public int? Port { get; set; }
    public string? DatabaseName { get; set; }
    public string? SchemaName { get; set; }
    public string? TableOrView { get; set; }
    public string? CredentialReference { get; set; }
    public string? ProtectedPassword { get; set; }
    public string? TransactionIdField { get; set; }
    public string? BusinessDateField { get; set; }
    public string? OutletField { get; set; }
    public string? ItemCodeField { get; set; }
    public string? QuantityField { get; set; }
    public string? StatusField { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public Organization Organization { get; set; } = null!;
    public ApiIntegrationConfiguration? ApiIntegrationConfiguration { get; set; }
}

public sealed class PosOutletMapping
{
    public Guid Id { get; set; }
    public Guid OrganizationId { get; set; }
    public Guid PosSourceId { get; set; }
    public required string PosOutletCode { get; set; }
    public string? PosOutletName { get; set; }
    public required string PropertyCode { get; set; }
    public required string OutletCode { get; set; }
    public required string PlantCode { get; set; }
    public required string StorageLocationCode { get; set; }
    public string? CompanyCode { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public PosSource PosSource { get; set; } = null!;
    public ICollection<OutletMenuItem> MenuItems { get; set; } = [];
}

public sealed class OutletMenuItem
{
    public Guid Id { get; set; }
    public Guid OrganizationId { get; set; }
    public Guid OutletMappingId { get; set; }
    public Guid RecipeId { get; set; }
    public string? PosCode { get; set; }
    public string? PosItem { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public PosOutletMapping Outlet { get; set; } = null!;
    public Recipe Recipe { get; set; } = null!;
}

public sealed class PosItemRecipeMapping
{
    public Guid Id { get; set; }
    public Guid OrganizationId { get; set; }
    public Guid PosSourceId { get; set; }
    public required string PosItemCode { get; set; }
    public string? PosItemDescription { get; set; }
    public Guid RecipeId { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public PosSource PosSource { get; set; } = null!;
    public Recipe Recipe { get; set; } = null!;
}

public sealed class RecipeConsumptionTransaction
{
    public Guid Id { get; set; }
    public Guid OrganizationId { get; set; }
    public Guid PosSourceId { get; set; }
    public required string SourceSystem { get; set; }
    public required string SourceTransactionId { get; set; }
    public int SourceLineNumber { get; set; } = 1;
    public DateOnly? BusinessDate { get; set; }
    public DateTime? TransactionAt { get; set; }
    public required string PosOutletCode { get; set; }
    public required string PosItemCode { get; set; }
    public string? PosItemDescription { get; set; }
    public decimal QuantitySold { get; set; }
    public decimal? Amount { get; set; }
    public string? RawReference { get; set; }
    public RecipeTransactionStatus Status { get; set; } = RecipeTransactionStatus.RECEIVED;
    public Guid? RecipeId { get; set; }
    public Guid? RecipeVersionId { get; set; }
    public int? RecipeVersionNumber { get; set; }
    public string? PropertyCode { get; set; }
    public string? OutletCode { get; set; }
    public string? CompanyCode { get; set; }
    public string? PlantCode { get; set; }
    public string? StorageLocationCode { get; set; }
    public Guid? UploadBatchId { get; set; }
    public Guid? IntegrationRouteId { get; set; }
    public string? IntegrationSystem { get; set; }
    public string? ExternalReference { get; set; }
    public string? FailureCode { get; set; }
    public string? FailureMessage { get; set; }
    public string? FailedStep { get; set; }
    public DateTime? FailureAt { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? ProcessedAt { get; set; }
    public ICollection<RecipeConsumptionLine> Lines { get; set; } = [];
    public ICollection<RecipeTransactionEvent> Events { get; set; } = [];
}

public sealed class RecipeConsumptionLine
{
    public Guid Id { get; set; }
    public Guid TransactionId { get; set; }
    public Guid? MaterialId { get; set; }
    public string? MaterialCode { get; set; }
    public string? Description { get; set; }
    public decimal ConsumedQuantity { get; set; }
    public required string Uom { get; set; }
    public decimal? UnitCost { get; set; }
    public int Sequence { get; set; }
    public RecipeConsumptionTransaction Transaction { get; set; } = null!;
}

public sealed class RecipeTransactionEvent
{
    public Guid Id { get; set; }
    public Guid TransactionId { get; set; }
    public required string Step { get; set; }
    public required string Status { get; set; }
    public string? Detail { get; set; }
    public DateTime CreatedAt { get; set; }
    public RecipeConsumptionTransaction Transaction { get; set; } = null!;
}

public sealed class RecipeInventoryPosting
{
    public Guid Id { get; set; }
    public Guid TransactionId { get; set; }
    public Guid OrganizationId { get; set; }
    public RecipeTransactionStatus Status { get; set; } = RecipeTransactionStatus.READY_TO_POST;
    public Guid? IntegrationRouteId { get; set; }
    public Guid? IntegrationConfigurationId { get; set; }
    public Guid? SapPostingId { get; set; }
    public string? ExternalReference { get; set; }
    public string? RequestJson { get; set; }
    public string? ResponseJson { get; set; }
    public int? HttpStatus { get; set; }
    public string? MaterialDocument { get; set; }
    public string? DocumentYear { get; set; }
    public string? ErrorCode { get; set; }
    public string? ErrorMessage { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
    public RecipeConsumptionTransaction Transaction { get; set; } = null!;
}

public sealed class PosSalesUploadBatch
{
    public Guid Id { get; set; }
    public Guid OrganizationId { get; set; }
    public Guid PosSourceId { get; set; }
    public required string FileName { get; set; }
    public Guid? UploadedByUserId { get; set; }
    public DateTime UploadedAt { get; set; }
    public DateOnly? BusinessDateFrom { get; set; }
    public DateOnly? BusinessDateTo { get; set; }
    public int Rows { get; set; }
    public int Valid { get; set; }
    public int Duplicates { get; set; }
    public int Invalid { get; set; }
    public int UnmappedPosCodes { get; set; }
    public int InvalidOutlets { get; set; }
    public int InvalidUom { get; set; }
    public int RecipeNotReady { get; set; }
    public int ReadyToProcess { get; set; }
    public int Processed { get; set; }
    public int Failed { get; set; }
    public int PostingUnknown { get; set; }
    public required string Status { get; set; }
    public ICollection<PosSalesUploadLine> Lines { get; set; } = [];
}

public sealed class PosSalesUploadLine
{
    public Guid Id { get; set; }
    public Guid BatchId { get; set; }
    public int RowNumber { get; set; }
    public DateOnly? BusinessDate { get; set; }
    public string? TransactionId { get; set; }
    public int? LineId { get; set; }
    public string? PosCode { get; set; }
    public decimal? Qty { get; set; }
    public string? Uom { get; set; }
    public string? OutletId { get; set; }
    public string? Currency { get; set; }
    public required string Status { get; set; }
    public string? ErrorCode { get; set; }
    public string? ErrorMessage { get; set; }
    public Guid? ConsumptionTransactionId { get; set; }
    public PosSalesUploadBatch Batch { get; set; } = null!;
}

public sealed class RecipeSapPosting
{
    public Guid Id { get; set; }
    public Guid OrganizationId { get; set; }
    public Guid? UploadBatchId { get; set; }
    public DateOnly BusinessDate { get; set; }
    public required string Plant { get; set; }
    public required string StorageLocation { get; set; }
    public string? CompanyCode { get; set; }
    public RecipeTransactionStatus Status { get; set; } = RecipeTransactionStatus.READY_TO_POST;
    public Guid? IntegrationRouteId { get; set; }
    public Guid? IntegrationConfigurationId { get; set; }
    public string? RequestJson { get; set; }
    public string? ResponseJson { get; set; }
    public int? HttpStatus { get; set; }
    public string? MaterialDocument { get; set; }
    public string? DocumentYear { get; set; }
    public string? ErrorCode { get; set; }
    public string? ErrorMessage { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? PostedAt { get; set; }
}
