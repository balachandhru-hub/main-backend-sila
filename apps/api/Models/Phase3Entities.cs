namespace SilaMe.Api.Models;

public enum DocumentType { INVOICE, DELIVERY_NOTE, CREDIT_NOTE, PURCHASE_ORDER, RECEIPT, CONTRACT }
public enum DocumentStatus { UPLOADED, READING, BASIC_EXTRACTION_COMPLETE, FULL_EXTRACTION_PROCESSING, FULL_EXTRACTION_COMPLETE, REVIEW_REQUIRED, PROCESSED, FAILED, GRN_READY, GRN_POSTED }
public enum DocumentSourceChannel { MOBILE_CAMERA, MOBILE_UPLOAD, MOBILE_SCANNER, CLOUD_UPLOAD }
public enum ExtractionType { PDF_TEXT, OCR, BASIC_INVOICE, FULL_INVOICE, INVOICE_STRUCTURED }
public enum ProcessingStatus { QUEUED, PROCESSING, COMPLETED, PARTIAL, REVIEW_REQUIRED, FAILED }
public enum ExtractionMethod { PDF_TEXT, OCR, EXTERNAL_AGENT, USER_CORRECTED }
public enum ExtractionTrigger { LEGACY, AUTO_FALLBACK, MANUAL_REREAD, INITIAL_BACKEND, FULL_PROCESSING }
public enum ExtractionAuthenticationType { NONE, API_KEY, BEARER_TOKEN, BASIC, OAUTH2, CUSTOM }
public enum ExtractionProviderKind { BUILT_IN, AZURE_DOCUMENT_INTELLIGENCE, GOOGLE_DOCUMENT_AI, AWS_TEXTRACT, CUSTOM_REST, AI_AGENT, OTHER }
public enum InvoiceType { MATERIAL, SERVICE, MIXED, UNKNOWN }
public enum InvoiceStatus { UPLOADED, PROCESSING, REVIEW_REQUIRED, PO_MATCHED, READY_FOR_GRN, GRN_POSTED, OCR_FAILED, FAILED }
public enum InvoiceLineMatchStatus { MATCHED, SUGGESTED, UNMATCHED }
public enum PurchaseOrderStatus { OPEN, PARTIALLY_RECEIVED, CLOSED, CANCELLED }
public enum PurchaseOrderItemStatus { OPEN, PARTIALLY_RECEIVED, CLOSED, CANCELLED }
public enum GoodsReceiptStatus { DRAFT, READY_TO_POST, POSTING, POSTED, FAILED, UNKNOWN, CANCELLED }
public enum InventoryTransactionType { GRN_RECEIPT }
public enum IdempotencyStatus { STARTED, COMPLETED, FAILED }

public enum IntegrationProcessType
{
    GET_PO, GET_SUPPLIER, GET_MATERIAL, POST_PO, UPDATE_PO, GET_GRN, POST_GRN, CANCEL_GRN,
    GET_INVOICE, POST_INVOICE, GET_STOCK, UPDATE_STOCK, POST_INVENTORY_ADJUSTMENT, GET_POS_SALE, GET_COMPANY_CODE, GET_PLANT, GET_STORAGE_LOCATION,
    GET_COST_CENTER, GET_GL_ACCOUNT, GET_PROJECT, GET_WBS, GET_PAYMENT_STATUS, CUSTOM
}
public enum IntegrationProtocol { REST, ODATA_V2, ODATA_V4, SOAP }
public enum IntegrationAuthenticationType { NONE, BASIC, API_KEY, BEARER_TOKEN, OAUTH2_CLIENT_CREDENTIALS, CUSTOM_TOKEN_ENDPOINT, CUSTOM_HEADER }
public enum IntegrationConfigurationStatus { DRAFT, TESTING, TEST_FAILED, TESTED, VALIDATED, ACTIVE, INACTIVE, ERROR, DISABLED }
public enum IntegrationExecutionTrigger { TEST, MANUAL, SCHEDULED, RETRY, EXCEL_IMPORT }
public enum IntegrationExecutionStatus { RUNNING, SUCCESS, PARTIAL, FAILED }
public enum IntegrationNullPolicy { IGNORE_NULL, WRITE_NULL, DEFAULT_VALUE }
public enum IntegrationSystemKind { SAP_S4HANA, SAP_ARIBA, ORACLE, ODOO, CUSTOM, OTHER }
public enum IntegrationMappingSourceKind { FIELD, CONSTANT, DEFAULT }
public enum IntegrationExecutionMode { MANUAL, SCHEDULED, EVENT_DRIVEN }
public enum IntegrationRequestBodyMode { FIELD_MAPPING, JSON_TEMPLATE, XML_TEMPLATE }
public enum IntegrationMappingDirection { SILA_TO_EXTERNAL, EXTERNAL_TO_SILA }
public enum OperationalMasterKind { COMPANY_CODES, PROPERTIES, PLANTS, STORAGE_LOCATIONS }

public sealed class CompanyCodeMaster
{
    public Guid Id { get; set; }
    public Guid OrganizationId { get; set; }
    public required string CompanyCode { get; set; }
    public required string CompanyName { get; set; }
    public string? Country { get; set; }
    public string? Currency { get; set; }
    public string? Address { get; set; }
    public string? City { get; set; }
    public string? TaxRegistrationNumber { get; set; }
    public string? SourceSystem { get; set; }
    public string? ExternalId { get; set; }
    public StatusKind Status { get; set; } = StatusKind.ACTIVE;
    public bool IsDeleted { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public Organization Organization { get; set; } = null!;
}

public sealed class PropertyMaster
{
    public Guid Id { get; set; }
    public Guid OrganizationId { get; set; }
    public required string PropertyCode { get; set; }
    public required string PropertyName { get; set; }
    public string? Country { get; set; }
    public string? CompanyCode { get; set; }
    public StatusKind Status { get; set; } = StatusKind.ACTIVE;
    public bool IsDeleted { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public Organization Organization { get; set; } = null!;
}

public sealed class ApiIntegrationConfiguration
{
    public Guid Id { get; set; }
    public Guid OrganizationId { get; set; }
    public Guid? OrganizationUnitId { get; set; }
    public required string EntityCode { get; set; }
    public required string Name { get; set; }
    public string? Description { get; set; }
    public IntegrationSystemKind SystemKind { get; set; } = IntegrationSystemKind.CUSTOM;
    public IntegrationProcessType ProcessType { get; set; }
    public IntegrationProtocol Protocol { get; set; }
    public required string BaseUrl { get; set; }
    public string? ResourcePath { get; set; }
    public string? ServicePath { get; set; }
    public string? EntitySet { get; set; }
    public string HttpMethod { get; set; } = "GET";
    public string? EnvironmentCode { get; set; }
    public int Priority { get; set; } = 100;
    public string? CompanyCode { get; set; }
    public string? Plant { get; set; }
    public string? PropertyCode { get; set; }
    public string? DesignerJson { get; set; }
    public string? ValidationFingerprint { get; set; }
    public DateTime? ValidatedAt { get; set; }
    public string? ValidatedBy { get; set; }
    public string? ValidationStatus { get; set; }
    public string? ConnectionStatus { get; set; }
    public DateTime? LastSuccessfulTestAt { get; set; }
    public IntegrationAuthenticationType AuthenticationType { get; set; }
    public string? Username { get; set; }
    public string? ProtectedPassword { get; set; }
    public string? ProtectedClientId { get; set; }
    public string? ProtectedClientSecret { get; set; }
    public string? ProtectedBearerToken { get; set; }
    public string? TokenEndpoint { get; set; }
    public string? TokenScope { get; set; }
    public string? TokenHeadersJson { get; set; }
    public string? TokenBodyJson { get; set; }
    public int TimeoutSeconds { get; set; } = 30;
    public int RetryCount { get; set; } = 2;
    public int? PageSize { get; set; } = 100;
    public string? WatermarkField { get; set; }
    public DateTime? LastWatermark { get; set; }
    public DateTime? LastAttemptAt { get; set; }
    public DateTime? LastSuccessfulRunAt { get; set; }
    public DateTime? NextRunAt { get; set; }
    public bool IsRunning { get; set; }
    public DateTime? RunningSince { get; set; }
    public string? LastErrorSafe { get; set; }
    public string? ScheduleCron { get; set; }
    public IntegrationConfigurationStatus Status { get; set; } = IntegrationConfigurationStatus.DRAFT;
    public DateTime? TestedAt { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public Organization Organization { get; set; } = null!;
    public OrganizationUnit? OrganizationUnit { get; set; }
    public ICollection<IntegrationSchemaSnapshot> SchemaSnapshots { get; set; } = [];
    public ICollection<ApiFieldMapping> FieldMappings { get; set; } = [];
    public ICollection<ApiIntegrationExecution> Executions { get; set; } = [];
    public ICollection<IntegrationRoute> IntegrationRoutes { get; set; } = [];
}

public sealed class IntegrationConnectionTest
{
    public Guid Id { get; set; }
    public Guid OrganizationId { get; set; }
    public Guid? TenantId { get; set; }
    public Guid? UserId { get; set; }
    public Guid? ConfigurationId { get; set; }
    public required string Name { get; set; }
    public required string Fingerprint { get; set; }
    public required string SystemKind { get; set; }
    public required string ProcessType { get; set; }
    public required string Protocol { get; set; }
    public bool Success { get; set; }
    public required string Status { get; set; }
    public string? ErrorCode { get; set; }
    public string? ErrorMessageSafe { get; set; }
    public string? ChecksJson { get; set; }
    public int? HttpStatus { get; set; }
    public int DurationMs { get; set; }
    public DateTime TestedAt { get; set; }
    public DateTime ExpiresAt { get; set; }
}

public sealed class IntegrationRoute
{
    public Guid Id { get; set; }
    public Guid OrganizationId { get; set; }
    public IntegrationProcessType ProcessType { get; set; }
    public IntegrationSystemKind SystemKind { get; set; }
    public Guid ApiIntegrationConfigurationId { get; set; }
    public bool AppliesToAllCompanyCodes { get; set; }
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
    public Guid? CreatedBy { get; set; }
    public DateTime UpdatedAt { get; set; }
    public Guid? UpdatedBy { get; set; }
    public Organization Organization { get; set; } = null!;
    public ApiIntegrationConfiguration Configuration { get; set; } = null!;
    public ICollection<IntegrationRouteCompanyCode> CompanyCodes { get; set; } = [];
}

public sealed class IntegrationRouteCompanyCode
{
    public Guid Id { get; set; }
    public Guid IntegrationRouteId { get; set; }
    public required string CompanyCode { get; set; }
    public IntegrationRoute IntegrationRoute { get; set; } = null!;
}

public sealed class IntegrationSchemaSnapshot
{
    public Guid Id { get; set; }
    public Guid ConfigurationId { get; set; }
    public required string MetadataUrl { get; set; }
    public required string SchemaJson { get; set; }
    public DateTime DiscoveredAt { get; set; }
    public ApiIntegrationConfiguration Configuration { get; set; } = null!;
}

public sealed class ApiFieldMapping
{
    public Guid Id { get; set; }
    public Guid ConfigurationId { get; set; }
    public required string SourceField { get; set; }
    public required string TargetField { get; set; }
    public IntegrationMappingSourceKind SourceKind { get; set; } = IntegrationMappingSourceKind.FIELD;
    public string? SourceStructure { get; set; }
    public bool IsCollection { get; set; }
    public string? Transformation { get; set; }
    public IntegrationNullPolicy NullPolicy { get; set; } = IntegrationNullPolicy.IGNORE_NULL;
    public string? DefaultValue { get; set; }
    public bool IsValidated { get; set; }
    public DateTime UpdatedAt { get; set; }
    public ApiIntegrationConfiguration Configuration { get; set; } = null!;
}

public sealed class ApiIntegrationExecution
{
    public Guid Id { get; set; }
    public Guid ConfigurationId { get; set; }
    public IntegrationExecutionTrigger Trigger { get; set; }
    public IntegrationExecutionStatus Status { get; set; }
    public DateTime StartedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
    public int RecordsRead { get; set; }
    public int RecordsCreated { get; set; }
    public int RecordsUpdated { get; set; }
    public int RecordsFailed { get; set; }
    public DateTime? WatermarkBefore { get; set; }
    public DateTime? WatermarkAfter { get; set; }
    public string? ErrorCode { get; set; }
    public string? ErrorMessageSafe { get; set; }
    public string? DetailJson { get; set; }
    public ApiIntegrationConfiguration Configuration { get; set; } = null!;
}

public sealed class Supplier
{
    public Guid Id { get; set; }
    public Guid OrganizationId { get; set; }
    public required string SupplierCode { get; set; }
    public required string Name { get; set; }
    public required string NormalizedName { get; set; }
    public string? SearchName { get; set; }
    public string? BusinessPartnerId { get; set; }
    public string? TaxNumber { get; set; }
    public string? Trn { get; set; }
    public string? Email { get; set; }
    public string? Phone { get; set; }
    public string EntityCode { get; set; } = "DEFAULT";
    public string? LegalName { get; set; }
    public string? Address { get; set; }
    public string? Country { get; set; }
    public string? City { get; set; }
    public string? PostalCode { get; set; }
    public string? Street { get; set; }
    public string? CompanyCode { get; set; }
    public string? PurchasingOrganization { get; set; }
    public string? Currency { get; set; }
    public string? PaymentTerms { get; set; }
    public bool IsBlocked { get; set; }
    public bool IsDeleted { get; set; }
    public StatusKind Status { get; set; } = StatusKind.ACTIVE;
    public bool IsActive { get; set; } = true;
    public string? SourceSystem { get; set; }
    public Guid? SourceConfigurationId { get; set; }
    public DateTime? SourceLastChangedAt { get; set; }
    public DateTime? LastSyncedAt { get; set; }
    public string? ExternalId { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public Organization Organization { get; set; } = null!;
    public ICollection<SupplierMaterial> Materials { get; set; } = [];
    public ICollection<PurchaseOrder> PurchaseOrders { get; set; } = [];
    public ICollection<Invoice> Invoices { get; set; } = [];
    public ICollection<GoodsReceipt> GoodsReceipts { get; set; } = [];
    public ICollection<SupplierAlias> Aliases { get; set; } = [];
}

public sealed class SupplierAlias
{
    public Guid Id { get; set; }
    public Guid OrganizationId { get; set; }
    public Guid SupplierId { get; set; }
    public required string Alias { get; set; }
    public required string NormalizedAlias { get; set; }
    public string? SourceSystem { get; set; }
    public string EntityCode { get; set; } = "DEFAULT";
    public decimal? Confidence { get; set; }
    public bool IsConfirmed { get; set; }
    public Guid? CreatedByUserId { get; set; }
    public DateTime CreatedAt { get; set; }
    public Organization Organization { get; set; } = null!;
    public Supplier Supplier { get; set; } = null!;
}

public sealed class Material
{
    public Guid Id { get; set; }
    public Guid OrganizationId { get; set; }
    public required string MaterialCode { get; set; }
    public required string Description { get; set; }
    public required string NormalizedDescription { get; set; }
    public required string BaseUom { get; set; }
    public string? Category { get; set; }
    public string? MaterialGroup { get; set; }
    public string? MaterialType { get; set; }
    public string? AlternateUom { get; set; }
    public decimal? ConvFactor { get; set; }
    public string? ConvUnit { get; set; }
    public decimal? ConvValue { get; set; }
    public decimal? UnitCost { get; set; }
    public string? Currency { get; set; }
    public string? CompanyCode { get; set; }
    public string? ValuationArea { get; set; }
    public string? ValuationClass { get; set; }
    public string? PriceControl { get; set; }
    public decimal? StandardPrice { get; set; }
    public decimal? MovingAveragePrice { get; set; }
    public MaterialAcquisitionSource AcquisitionSource { get; set; } = MaterialAcquisitionSource.MANUAL;
    public MaterialGovernanceStatus GovernanceStatus { get; set; } = MaterialGovernanceStatus.DRAFT;
    public DateTime? ImportedAt { get; set; }
    public DateTime? LastSynchronizedAt { get; set; }
    public DateTime? SourceLastChangedAt { get; set; }
    public Guid? PendingChangeRequestId { get; set; }
    public StatusKind Status { get; set; } = StatusKind.INACTIVE;
    public string? SourceSystem { get; set; }
    public string? ExternalId { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public bool InventoryItem { get; set; }
    public InventoryItemType InventoryType { get; set; } = InventoryItemType.NON_STOCK;
    public bool BatchManaged { get; set; }
    public bool ExpiryManaged { get; set; }
    public int? ShelfLifeDays { get; set; }
    public bool SerialManaged { get; set; }
    public Organization Organization { get; set; } = null!;
}

public sealed class SupplierMaterial
{
    public Guid Id { get; set; }
    public Guid OrganizationId { get; set; }
    public Guid SupplierId { get; set; }
    public Guid MaterialId { get; set; }
    public string? SupplierMaterialCode { get; set; }
    public string? SupplierDescription { get; set; }
    public string? PurchaseUom { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public Organization Organization { get; set; } = null!;
    public Supplier Supplier { get; set; } = null!;
    public Material Material { get; set; } = null!;
}

public sealed class Document
{
    public Guid Id { get; set; }
    public Guid OrganizationId { get; set; }
    public Guid? OperatingUnitId { get; set; }
    public Guid UploadedByUserId { get; set; }
    public DocumentType DocumentType { get; set; }
    public required string OriginalFilename { get; set; }
    public required string ContentType { get; set; }
    public long FileSizeBytes { get; set; }
    public required string StorageProvider { get; set; }
    public required string StorageReference { get; set; }
    public string? ScanSessionId { get; set; }
    public string? OcrRequestId { get; set; }
    public string? ContentHash { get; set; }
    public int? PageCount { get; set; }
    public DocumentStatus Status { get; set; }
    public DocumentSourceChannel SourceChannel { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public Organization Organization { get; set; } = null!;
    public OrganizationUnit? OperatingUnit { get; set; }
    public User UploadedByUser { get; set; } = null!;
    public ICollection<DocumentPage> Pages { get; set; } = [];
    public ICollection<DocumentExtraction> Extractions { get; set; } = [];
    public Invoice? Invoice { get; set; }
    public ICollection<DocumentTransferJob> TransferJobs { get; set; } = [];
}

public sealed class DocumentTransferJob
{
    public Guid Id { get; set; }
    public Guid DocumentId { get; set; }
    public Guid DestinationId { get; set; }
    public Guid UserId { get; set; }
    public Guid OrganizationId { get; set; }
    public Guid? PropertyId { get; set; }
    public Guid? OperatingUnitId { get; set; }
    public DocumentType DocumentType { get; set; }
    public DocumentStorageProvider Provider { get; set; }
    public DocumentTransferStatus Status { get; set; }
    public string? ResolutionSource { get; set; }
    public int AttemptCount { get; set; }
    public DateTime? NextAttemptAt { get; set; }
    public DateTime? LastAttemptAt { get; set; }
    public DateTime? CompletedAt { get; set; }
    public string? ExternalFileId { get; set; }
    public string? ExternalWebUrl { get; set; }
    public string? ExternalFileName { get; set; }
    public string? LastErrorCode { get; set; }
    public string? LastErrorMessageSafe { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public Document Document { get; set; } = null!;
    public DocumentStorageDestination Destination { get; set; } = null!;
    public User User { get; set; } = null!;
    public Organization Organization { get; set; } = null!;
}

public sealed class DocumentPage
{
    public Guid Id { get; set; }
    public Guid DocumentId { get; set; }
    public int PageNumber { get; set; }
    public string? StorageReference { get; set; }
    public int RotationDegrees { get; set; }
    public string? OcrStatus { get; set; }
    public DateTime CreatedAt { get; set; }
    public Document Document { get; set; } = null!;
}

public sealed class DocumentExtraction
{
    public Guid Id { get; set; }
    public Guid DocumentId { get; set; }
    public ExtractionType ExtractionType { get; set; }
    public required string Provider { get; set; }
    public string? RawText { get; set; }
    public string? ProviderReference { get; set; }
    public decimal? Confidence { get; set; }
    public DateTime? ProcessingStartedAt { get; set; }
    public DateTime? ProcessingCompletedAt { get; set; }
    public ProcessingStatus Status { get; set; }
    public string? ErrorCode { get; set; }
    public string? ErrorMessage { get; set; }
    public ExtractionMethod ExtractionMethod { get; set; } = ExtractionMethod.PDF_TEXT;
    public bool FallbackUsed { get; set; }
    public long? ProcessingDurationMs { get; set; }
    public string? ErrorCategory { get; set; }
    public ExtractionTrigger Trigger { get; set; } = ExtractionTrigger.LEGACY;
    public string? OcrRequestId { get; set; }
    public string? ContentHash { get; set; }
    public string? ConfigurationSnapshotJson { get; set; }
    public string? StructuredPayloadJson { get; set; }
    public DateTime CreatedAt { get; set; }
    public Document Document { get; set; } = null!;
}

public sealed class InvoiceExtData
{
    public Guid Id { get; set; }
    public Guid DocumentId { get; set; }
    public Guid OrganizationId { get; set; }
    public Guid? OperatingUnitId { get; set; }
    public string? SupplierName { get; set; }
    public string? SupplierTrn { get; set; }
    public string? SupplierInvoiceNumber { get; set; }
    public DateOnly? InvoiceDate { get; set; }
    public string? PurchaseOrderNumber { get; set; }
    public decimal? InvoiceGross { get; set; }
    public decimal? InvoiceNet { get; set; }
    public string? Currency { get; set; }
    public string? ItemSkuId { get; set; }
    public decimal? ItemAmount { get; set; }
    public decimal? ItemNet { get; set; }
    public string? ItemDescription { get; set; }
    public string? LineItemNumber { get; set; }
    public Guid? PurchaseOrderItemId { get; set; }
    public required string SourceProvider { get; set; }
    public required string ExtractionMethod { get; set; }
    public decimal? ExtractionConfidence { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public Document Document { get; set; } = null!;
    public Organization Organization { get; set; } = null!;
    public OrganizationUnit? OperatingUnit { get; set; }
    public PurchaseOrderItem? PurchaseOrderItem { get; set; }
}

public sealed class ExtractionAgentConfig
{
    public Guid Id { get; set; }
    public Guid? OrganizationId { get; set; }
    public required string Name { get; set; }
    public required string DocumentType { get; set; }
    public required string ProviderType { get; set; }
    public string? EndpointUrl { get; set; }
    public ExtractionAuthenticationType AuthenticationType { get; set; }
    public string? CredentialReference { get; set; }
    public string? CredentialLast4 { get; set; }
    public string? ConfigurationJson { get; set; }
    public int Priority { get; set; } = 100;
    public bool IsActive { get; set; }
    public Guid CreatedByUserId { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public Organization? Organization { get; set; }
    public User CreatedByUser { get; set; } = null!;
}

public sealed class InvoiceOcrConfiguration
{
    public Guid Id { get; set; }
    public Guid OrganizationId { get; set; }
    public bool MobileBasicOcrEnabled { get; set; } = true;
    public bool AutomaticBackendFallbackEnabled { get; set; } = true;
    public decimal MinimumMobileConfidence { get; set; } = 0.75m;
    public bool RequireSupplierName { get; set; } = true;
    public bool RequireInvoiceNumber { get; set; } = true;
    public bool RequirePurchaseOrderNumber { get; set; } = true;
    public bool RequireInvoiceAmount { get; set; } = true;
    public bool RequireInvoiceDate { get; set; }
    public bool RequireCurrency { get; set; }
    public bool RequireSupplierTrn { get; set; }
    public string BackendProvider { get; set; } = "BUILT_IN_ADVANCED";
    public bool AlwaysBackendOnReread { get; set; } = true;
    public bool DetailedLineExtractionEnabled { get; set; } = true;
    public bool SupplierMasterValidationEnabled { get; set; } = true;
    public bool PurchaseOrderValidationEnabled { get; set; } = true;
    public bool FinancialReconciliationEnabled { get; set; } = true;
    public decimal AmountTolerance { get; set; } = 0.05m;
    public int BackendTimeoutSeconds { get; set; } = 60;
    public int BackendRetryCount { get; set; } = 1;
    public bool ReuseCachedOcr { get; set; } = true;
    public int Version { get; set; } = 1;
    public Guid CreatedByUserId { get; set; }
    public Guid UpdatedByUserId { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public Organization Organization { get; set; } = null!;
    public User CreatedByUser { get; set; } = null!;
    public User UpdatedByUser { get; set; } = null!;
}

public sealed class Invoice
{
    public Guid Id { get; set; }
    public Guid OrganizationId { get; set; }
    public Guid? OperatingUnitId { get; set; }
    public Guid DocumentId { get; set; }
    public Guid? SupplierId { get; set; }
    public required string InvoiceNumber { get; set; }
    public DateOnly? InvoiceDate { get; set; }
    public string? SupplierNameRaw { get; set; }
    public string? SupplierTaxNumberRaw { get; set; }
    public string? PoNumberRaw { get; set; }
    public Guid? PurchaseOrderId { get; set; }
    public string? Currency { get; set; }
    public decimal? NetAmount { get; set; }
    public decimal? TaxAmount { get; set; }
    public decimal? GrossAmount { get; set; }
    public string? SupplierLegalName { get; set; }
    public string? SupplierAddress { get; set; }
    public string? SupplierEmail { get; set; }
    public string? SupplierPhone { get; set; }
    public decimal? DiscountAmount { get; set; }
    public decimal? FreightAmount { get; set; }
    public decimal? OtherCharges { get; set; }
    public decimal? TaxableAmount { get; set; }
    public decimal? AmountDue { get; set; }
    public string? PaymentTerms { get; set; }
    public DateOnly? DueDate { get; set; }
    public string? ExtractionStatus { get; set; }
    public string? ExtractionProvider { get; set; }
    public DateTime? ExtractedAt { get; set; }
    public DateTime? ReviewedAt { get; set; }
    public Guid? ReviewedByUserId { get; set; }
    public string? ManualEditedFieldsJson { get; set; }
    public InvoiceType InvoiceType { get; set; }
    public InvoiceStatus Status { get; set; }
    public decimal? OverallConfidence { get; set; }
    public Guid CreatedByUserId { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public Organization Organization { get; set; } = null!;
    public OrganizationUnit? OperatingUnit { get; set; }
    public Document Document { get; set; } = null!;
    public Supplier? Supplier { get; set; }
    public PurchaseOrder? PurchaseOrder { get; set; }
    public User CreatedByUser { get; set; } = null!;
    public ICollection<InvoiceLine> Lines { get; set; } = [];
    public ICollection<GoodsReceipt> GoodsReceipts { get; set; } = [];
}

public sealed class InvoiceLine
{
    public Guid Id { get; set; }
    public Guid InvoiceId { get; set; }
    public int LineNumber { get; set; }
    public string? SupplierMaterialCode { get; set; }
    public Guid? MaterialId { get; set; }
    public string? MaterialCodeRaw { get; set; }
    public required string DescriptionRaw { get; set; }
    public decimal? Quantity { get; set; }
    public string? Uom { get; set; }
    public decimal? UnitPrice { get; set; }
    public decimal? TaxRate { get; set; }
    public decimal? TaxAmount { get; set; }
    public decimal? LineAmount { get; set; }
    public decimal? DiscountAmount { get; set; }
    public decimal? GrossAmount { get; set; }
    public string? PoItemNumber { get; set; }
    public string? BatchNumber { get; set; }
    public DateOnly? ExpiryDate { get; set; }
    public decimal? Confidence { get; set; }
    public InvoiceLineMatchStatus MatchStatus { get; set; }
    public Guid? PurchaseOrderItemId { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public Invoice Invoice { get; set; } = null!;
    public Material? Material { get; set; }
    public PurchaseOrderItem? PurchaseOrderItem { get; set; }
}

public sealed class PurchaseOrder
{
    public Guid Id { get; set; }
    public Guid OrganizationId { get; set; }
    public Guid? OperatingUnitId { get; set; }
    public Guid SupplierId { get; set; }
    public required string PoNumber { get; set; }
    public string? PurchaseOrderType { get; set; }
    public string? CompanyCode { get; set; }
    public string? ErpSupplierId { get; set; }
    public string? SupplierName { get; set; }
    public string? PurchasingOrganization { get; set; }
    public string? PurchasingGroup { get; set; }
    public string? PaymentTerms { get; set; }
    public string? PoCategory { get; set; }
    public DateOnly? PoDate { get; set; }
    public DateOnly? DeliveryDate { get; set; }
    public required string Currency { get; set; }
    public decimal? TotalNetAmount { get; set; }
    public decimal? TotalTaxAmount { get; set; }
    public decimal? TotalAmount { get; set; }
    public decimal? TotalOrderedQuantity { get; set; }
    public decimal? TotalReceivedQuantity { get; set; }
    public PurchaseOrderStatus Status { get; set; }
    public required string SourceSystem { get; set; }
    public string? ExternalId { get; set; }
    public string EntityCode { get; set; } = "DEFAULT";
    public Guid? SourceConfigurationId { get; set; }
    public DateTime? SourceLastChangedAt { get; set; }
    public string? SourceLastChangedAtRaw { get; set; }
    public DateTime? LastSyncedAt { get; set; }
    public string? SourceHash { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public Organization Organization { get; set; } = null!;
    public OrganizationUnit? OperatingUnit { get; set; }
    public Supplier Supplier { get; set; } = null!;
    public ICollection<PurchaseOrderItem> Items { get; set; } = [];
    public ICollection<Invoice> Invoices { get; set; } = [];
    public ICollection<GoodsReceipt> GoodsReceipts { get; set; } = [];
}

public sealed class PurchaseOrderItem
{
    public Guid Id { get; set; }
    public Guid PurchaseOrderId { get; set; }
    public int LineNumber { get; set; }
    public string? ItemNumber { get; set; }
    public Guid? MaterialId { get; set; }
    public required string MaterialCode { get; set; }
    public required string Description { get; set; }
    public decimal OrderedQuantity { get; set; }
    public decimal ReceivedQuantity { get; set; }
    public decimal OpenQuantity { get; set; }
    public required string Uom { get; set; }
    public decimal? UnitPrice { get; set; }
    public decimal? PriceQuantity { get; set; }
    public decimal? ItemAmount { get; set; }
    public string? TaxCode { get; set; }
    public decimal? TaxAmount { get; set; }
    public decimal? GrossItemAmount { get; set; }
    public string? Currency { get; set; }
    public string? MaterialGroup { get; set; }
    public string? Plant { get; set; }
    public string? StorageLocation { get; set; }
    public string? ItemCategory { get; set; }
    public string? AccountAssignmentCategory { get; set; }
    public bool GoodsReceiptExpected { get; set; } = true;
    public bool InvoiceExpected { get; set; } = true;
    public bool DeliveryCompleted { get; set; }
    public bool DeletionIndicator { get; set; }
    public PurchaseOrderItemStatus Status { get; set; }
    public string? ExternalId { get; set; }
    public DateTime? SourceLastChangedAt { get; set; }
    public DateTime? LastSyncedAt { get; set; }
    public string? SourceHash { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public PurchaseOrder PurchaseOrder { get; set; } = null!;
    public Material? Material { get; set; }
    public ICollection<InvoiceLine> InvoiceLines { get; set; } = [];
    public ICollection<GoodsReceiptLine> GoodsReceiptLines { get; set; } = [];
}

public sealed class GoodsReceipt
{
    public Guid Id { get; set; }
    public Guid OrganizationId { get; set; }
    public Guid? OperatingUnitId { get; set; }
    public required string GrnNumber { get; set; }
    public Guid PurchaseOrderId { get; set; }
    public Guid? InvoiceId { get; set; }
    public Guid SupplierId { get; set; }
    public DateTime ReceiptDate { get; set; }
    public GoodsReceiptStatus Status { get; set; }
    public required string PostingProvider { get; set; }
    public string? ExternalReference { get; set; }
    public Guid CreatedByUserId { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? PostedAt { get; set; }
    public string? FailureCode { get; set; }
    public string? FailureMessage { get; set; }
    public string? BusinessStatus { get; set; }
    public string? ErpPostingStatus { get; set; }
    public string? ErpMaterialDocument { get; set; }
    public string? ErpDocumentYear { get; set; }
    public string? ErpResponseJson { get; set; }
    public DateTime? ErpPostedAt { get; set; }
    public int ErpAttemptCount { get; set; }
    public DateTime? LastErpAttemptAt { get; set; }
    public string? AsnReference { get; set; }
    public Guid? IntegrationRouteId { get; set; }
    public Guid? IntegrationConfigurationId { get; set; }
    public string? ExternalSystem { get; set; }
    public Organization Organization { get; set; } = null!;
    public OrganizationUnit? OperatingUnit { get; set; }
    public PurchaseOrder PurchaseOrder { get; set; } = null!;
    public Invoice? Invoice { get; set; }
    public Supplier Supplier { get; set; } = null!;
    public User CreatedByUser { get; set; } = null!;
    public ICollection<GoodsReceiptLine> Lines { get; set; } = [];
}

public sealed class GoodsReceiptLine
{
    public Guid Id { get; set; }
    public Guid GoodsReceiptId { get; set; }
    public Guid PurchaseOrderItemId { get; set; }
    public Guid? MaterialId { get; set; }
    public required string MaterialCode { get; set; }
    public required string Description { get; set; }
    public decimal OpenQuantityBefore { get; set; }
    public decimal? InvoiceQuantity { get; set; }
    public decimal ReceivedQuantity { get; set; }
    public decimal AcceptedQuantity { get; set; }
    public decimal DamagedQuantity { get; set; }
    public decimal RejectedQuantity { get; set; }
    public required string Uom { get; set; }
    public string? BatchNumber { get; set; }
    public DateOnly? ExpiryDate { get; set; }
    public DateTime CreatedAt { get; set; }
    public GoodsReceipt GoodsReceipt { get; set; } = null!;
    public PurchaseOrderItem PurchaseOrderItem { get; set; } = null!;
}

public sealed class StockBalance
{
    public Guid Id { get; set; }
    public Guid OrganizationId { get; set; }
    public Guid OperatingUnitId { get; set; }
    public Guid? MaterialId { get; set; }
    public required string MaterialCode { get; set; }
    public decimal Quantity { get; set; }
    public required string Uom { get; set; }
    public DateTime UpdatedAt { get; set; }
    public Organization Organization { get; set; } = null!;
    public OrganizationUnit OperatingUnit { get; set; } = null!;
}

public sealed class InventoryTransaction
{
    public Guid Id { get; set; }
    public Guid OrganizationId { get; set; }
    public Guid OperatingUnitId { get; set; }
    public Guid? MaterialId { get; set; }
    public required string MaterialCode { get; set; }
    public InventoryTransactionType TransactionType { get; set; }
    public required string ReferenceType { get; set; }
    public Guid ReferenceId { get; set; }
    public decimal Quantity { get; set; }
    public required string Uom { get; set; }
    public Guid CreatedByUserId { get; set; }
    public DateTime CreatedAt { get; set; }
    public Organization Organization { get; set; } = null!;
    public OrganizationUnit OperatingUnit { get; set; } = null!;
    public User CreatedByUser { get; set; } = null!;
}

public sealed class IdempotencyRecord
{
    public Guid Id { get; set; }
    public Guid OrganizationId { get; set; }
    public Guid UserId { get; set; }
    public required string IdempotencyKey { get; set; }
    public required string Operation { get; set; }
    public string? RequestHash { get; set; }
    public string? ResponseReference { get; set; }
    public IdempotencyStatus Status { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? ExpiresAt { get; set; }
    public Organization Organization { get; set; } = null!;
    public User User { get; set; } = null!;
}

public sealed class AuditEvent
{
    public Guid Id { get; set; }
    public Guid? OrganizationId { get; set; }
    public Guid? OperatingUnitId { get; set; }
    public Guid? UserId { get; set; }
    public required string EventType { get; set; }
    public required string EntityType { get; set; }
    public Guid? EntityId { get; set; }
    public string? Reference { get; set; }
    public string? MetadataJson { get; set; }
    public string? OldStateJson { get; set; }
    public string? NewStateJson { get; set; }
    public Guid? ChangedByUserId { get; set; }
    public string? Reason { get; set; }
    public string? Result { get; set; }
    public DateTime CreatedAt { get; set; }
}