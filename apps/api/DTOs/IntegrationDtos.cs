using System.ComponentModel.DataAnnotations;
using SilaMe.Api.Models;

namespace SilaMe.Api.DTOs;

public sealed class IntegrationConfigurationInput
{
    [Required] public string Name { get; set; } = string.Empty;
    [Required] public string EntityCode { get; set; } = "ALL";
    public Guid? OrganizationUnitId { get; set; }
    public IntegrationProcessType ProcessType { get; set; } = IntegrationProcessType.GET_PO;
    public IntegrationProtocol Protocol { get; set; } = IntegrationProtocol.ODATA_V4;
    [Required, Url] public string BaseUrl { get; set; } = string.Empty;
    public string? ResourcePath { get; set; } = "API_PURCHASEORDER_PROCESS_SRV/A_PurchaseOrder";
    public IntegrationAuthenticationType AuthenticationType { get; set; } = IntegrationAuthenticationType.NONE;
    public string? Username { get; set; }
    public string? Password { get; set; }
    public string? ClientId { get; set; }
    public string? ClientSecret { get; set; }
    public string? BearerToken { get; set; }
    public string? TokenEndpoint { get; set; }
    public string? TokenScope { get; set; }
    public Dictionary<string, string>? TokenHeaders { get; set; }
    public Dictionary<string, string>? TokenBody { get; set; }
    [Range(5, 300)] public int TimeoutSeconds { get; set; } = 30;
    [Range(0, 5)] public int RetryCount { get; set; } = 2;
    [Range(1, 1000)] public int? PageSize { get; set; } = 100;
    public string? WatermarkField { get; set; }
    public string? ScheduleCron { get; set; }
}

public sealed class FieldMappingInput
{
    [Required] public string SourceField { get; set; } = string.Empty;
    [Required] public string TargetField { get; set; } = string.Empty;
    public string? Transformation { get; set; }
    public IntegrationNullPolicy NullPolicy { get; set; } = IntegrationNullPolicy.IGNORE_NULL;
    public string? DefaultValue { get; set; }
    public IntegrationMappingSourceKind SourceKind { get; set; } = IntegrationMappingSourceKind.FIELD;
    public string? SourceStructure { get; set; }
    public bool IsCollection { get; set; }
}

public sealed record IntegrationConfigurationResponse(
    Guid Id, Guid OrganizationId, Guid? OrganizationUnitId, string EntityCode, string Name,
    IntegrationProcessType ProcessType, IntegrationProtocol Protocol, string BaseUrl, string? ResourcePath,
    IntegrationAuthenticationType AuthenticationType, string? Username, string CredentialStatus,
    int TimeoutSeconds, int RetryCount, int? PageSize, string? WatermarkField, DateTime? LastWatermark,
    DateTime? LastAttemptAt, DateTime? LastSuccessfulRunAt, DateTime? NextRunAt, bool IsRunning,
    string? LastErrorSafe, string? ScheduleCron, IntegrationConfigurationStatus Status, DateTime? TestedAt,
    DateTime CreatedAt, DateTime UpdatedAt,
    IntegrationSystemKind SystemKind = IntegrationSystemKind.CUSTOM, string? Description = null, string? HttpMethod = null,
    string? ServicePath = null, string? EntitySet = null, int Priority = 100, string? CompanyCode = null, string? Plant = null,
    string? PropertyCode = null, string? EnvironmentCode = null, string? ValidationFingerprint = null, DateTime? ValidatedAt = null,
    string? ValidatedBy = null, string? ValidationStatus = null, DateTime? LastSuccessfulTestAt = null,
    IntegrationDesignerDocument? Designer = null, string? ConnectionStatus = null);

public sealed record IntegrationSchemaResponse(Guid ConfigurationId, string MetadataUrl, DateTime DiscoveredAt, IReadOnlyList<IntegrationSchemaEntity> Entities);
public sealed record IntegrationSchemaEntity(string Name, string? EntitySet, IReadOnlyList<IntegrationSchemaProperty> Properties, IReadOnlyList<string> Keys);
public sealed record IntegrationSchemaProperty(string Name, string Type, bool Nullable);
public sealed record IntegrationTestResponse(bool Success, string Message, int? HttpStatus, DateTime TestedAt);
public sealed record IntegrationMappingResponse(Guid Id, Guid ConfigurationId, string SourceField, string TargetField, string? Transformation, IntegrationNullPolicy NullPolicy, string? DefaultValue, bool IsValidated, DateTime UpdatedAt, IntegrationMappingSourceKind SourceKind = IntegrationMappingSourceKind.FIELD, string? SourceStructure = null, bool IsCollection = false);
public sealed record IntegrationTargetFieldResponse(string TargetField, string Area, string DataType, bool Required, IReadOnlyList<string> AllowedTransformations);
public sealed record IntegrationExecutionResponse(
    Guid Id, Guid ConfigurationId, IntegrationExecutionTrigger Trigger, IntegrationExecutionStatus Status,
    DateTime StartedAt, DateTime? CompletedAt, int RecordsRead, int RecordsCreated, int RecordsUpdated,
    int RecordsFailed, DateTime? WatermarkBefore, DateTime? WatermarkAfter, string? ErrorCode, string? ErrorMessageSafe);
public sealed record IntegrationPurchaseOrderRow(Guid Id, string PoNumber, string SupplierName, PurchaseOrderStatus Status, string Currency, DateOnly? PoDate, DateTime? LastSyncedAt);
public sealed record IntegrationSupplierRow(Guid Id, string SupplierCode, string Name, string? TaxNumber, StatusKind Status, DateTime UpdatedAt);
public sealed record IntegrationDataUpdateResponse(
    Guid ConfigurationId,
    int PurchaseOrders,
    int Suppliers,
    DateTime? LastSyncedAt,
    DateTime? LastWatermark,
    bool IsRunning,
    string? LastErrorSafe,
    IReadOnlyList<IntegrationPurchaseOrderRow> PurchaseOrderRows,
    IReadOnlyList<IntegrationSupplierRow> SupplierRows,
    string Kind = "PURCHASE_ORDERS",
    int TotalRows = 0,
    int Page = 1,
    int PageSize = 25);

public sealed record IntegrationExecutionRequest(bool FullSync = false);
public enum IntegrationImportKind { PURCHASE_ORDERS, SUPPLIERS }
public sealed record IntegrationImportRowResponse(
    int RowNumber,
    bool IsValid,
    IReadOnlyDictionary<string, string?> Values,
    IReadOnlyList<string> Errors,
    IReadOnlyList<string>? ErrorCodes = null,
    string? Action = null);
public sealed record IntegrationImportPreviewResponse(
    Guid ConfigurationId,
    string Kind,
    string FileName,
    int TotalRows,
    int ValidRows,
    int InvalidRows,
    IReadOnlyList<string> Columns,
    IReadOnlyList<IntegrationImportRowResponse> Rows,
    int NewRows = 0,
    int UpdateRows = 0,
    int UnchangedRows = 0,
    string? EntityCode = null);
public sealed record IntegrationImportCorrectionReportInput(
    IntegrationImportKind Kind,
    IReadOnlyList<IntegrationImportRowResponse> Rows);
public sealed record IntegrationImportCommitInput(
    IntegrationImportKind Kind,
    IReadOnlyList<Dictionary<string, string?>> Rows);
public sealed record IntegrationImportCommitResponse(
    IntegrationExecutionResponse Execution,
    int RecordsCommitted);
public sealed record OperationalMasterCommitInput(IReadOnlyList<Dictionary<string, string?>> Rows);

public sealed class IntegrationRouteInput
{
    public IntegrationProcessType ProcessType { get; set; }
    public IntegrationSystemKind SystemKind { get; set; }
    public Guid ApiIntegrationConfigurationId { get; set; }
    public List<string>? CompanyCodes { get; set; }
    public bool AppliesToAllCompanyCodes { get; set; }
    public bool IsActive { get; set; } = true;
}

public sealed record IntegrationRouteResponse(
    Guid Id,
    IntegrationProcessType ProcessType,
    IntegrationSystemKind SystemKind,
    Guid ApiIntegrationConfigurationId,
    string ConfigurationName,
    bool AppliesToAllCompanyCodes,
    IReadOnlyList<string> CompanyCodes,
    bool IsActive,
    DateTime UpdatedAt);

public sealed record IntegrationRouteMatchResponse(
    Guid Id,
    string Name,
    IntegrationProcessType ProcessType,
    IntegrationSystemKind SystemKind,
    string Status);

public sealed record ResolvedIntegrationRoute(
    Guid RouteId,
    IntegrationProcessType ProcessType,
    IntegrationSystemKind SystemKind,
    Guid ApiIntegrationConfigurationId,
    string ConfigurationName,
    bool AppliesToAllCompanyCodes,
    string CompanyCode,
    ApiIntegrationConfiguration Configuration);

public sealed record IntegrationDeleteResponse(bool Deleted, Guid Id);