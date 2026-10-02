using System.ComponentModel.DataAnnotations;
using SilaMe.Api.Models;

namespace SilaMe.Api.DTOs;

// PROTECTED SILA INVOICE RECEIVING FLOW — See docs/PROTECTED_INVOICE_FLOW.md.
// Do not modify this contract/behavior from unrelated feature work.

public sealed record BasicInvoiceExtractionResponse(
    Guid DocumentId,
    string? SupplierName,
    string? SupplierTrn,
    string? SupplierInvoiceNumber,
    DateOnly? InvoiceDate,
    string? PurchaseOrderNumber,
    decimal? InvoiceGross,
    string? Currency,
    InvoiceStatus Status);

public sealed record BasicOcrResponse(
    string? SupplierName,
    string? SupplierTrn,
    string? SupplierInvoiceNumber,
    DateOnly? InvoiceDate,
    string? PurchaseOrderNumber,
    decimal? InvoiceGross,
    string? Currency,
    string Provider,
    bool Available,
    string Status,
    decimal? Confidence,
    string? RawText);

public sealed record AdvancedExtractedField<T>(
    T? Value,
    decimal? Confidence,
    string ConfidenceBand,
    int? PageNumber,
    string? SourceLabel,
    string? ExtractionMethod);

public sealed record AdvancedInvoiceHeaderResponse(
    AdvancedExtractedField<string> SupplierName,
    AdvancedExtractedField<string> SupplierLegalName,
    AdvancedExtractedField<string> SupplierTrn,
    AdvancedExtractedField<string> SupplierAddress,
    AdvancedExtractedField<string> SupplierEmail,
    AdvancedExtractedField<string> SupplierPhone,
    AdvancedExtractedField<string> InvoiceNumber,
    AdvancedExtractedField<DateOnly?> InvoiceDate,
    AdvancedExtractedField<string> InvoiceType,
    AdvancedExtractedField<string> PurchaseOrderNumber,
    AdvancedExtractedField<string> Currency,
    AdvancedExtractedField<decimal?> NetAmount,
    AdvancedExtractedField<decimal?> DiscountAmount,
    AdvancedExtractedField<decimal?> FreightAmount,
    AdvancedExtractedField<decimal?> OtherCharges,
    AdvancedExtractedField<decimal?> TaxableAmount,
    AdvancedExtractedField<decimal?> TaxAmount,
    AdvancedExtractedField<decimal?> GrossAmount,
    AdvancedExtractedField<decimal?> AmountDue,
    AdvancedExtractedField<string> PaymentTerms,
    AdvancedExtractedField<DateOnly?> DueDate);

public sealed record AdvancedInvoiceLineResponse(
    int LineNumber,
    AdvancedExtractedField<string> SupplierItemCode,
    AdvancedExtractedField<string> Description,
    AdvancedExtractedField<string> PoItemNumber,
    AdvancedExtractedField<decimal?> Quantity,
    AdvancedExtractedField<string> Uom,
    AdvancedExtractedField<decimal?> UnitPrice,
    AdvancedExtractedField<decimal?> DiscountAmount,
    AdvancedExtractedField<decimal?> NetAmount,
    AdvancedExtractedField<decimal?> TaxRate,
    AdvancedExtractedField<decimal?> TaxAmount,
    AdvancedExtractedField<decimal?> GrossAmount,
    AdvancedExtractedField<string> BatchNumber,
    AdvancedExtractedField<DateOnly?> ExpiryDate);

public sealed record AdvancedInvoiceValidationResponse(
    bool AmountsReconciled,
    bool LineTotalMatchesNet,
    bool TaxReconciled,
    decimal? Difference,
    bool SupplierMatched,
    bool PurchaseOrderMatched);

public sealed record AdvancedInvoiceExtractionResponse(
    Guid? DocumentId,
    string Provider,
    string Status,
    string Trigger,
    decimal? Confidence,
    AdvancedInvoiceHeaderResponse Header,
    IReadOnlyList<AdvancedInvoiceLineResponse> Lines,
    AdvancedInvoiceValidationResponse Validation,
    bool RequiresReview,
    bool FallbackUsed,
    DateTime? StartedAt,
    DateTime? CompletedAt,
    int PageCount,
    string? ErrorMessage)
{
    public string? OcrRequestId { get; init; }
    public string? RawText { get; init; }
    public AdvancedOcrConfigurationSnapshot? EffectiveConfiguration { get; init; }
}

public sealed record AdvancedOcrConfigurationSnapshot(
    bool MobileBasicOcrEnabled,
    bool AutomaticBackendFallbackEnabled,
    decimal MinimumMobileConfidence,
    bool RequireSupplierName,
    bool RequireInvoiceNumber,
    bool RequirePurchaseOrderNumber,
    bool RequireInvoiceAmount,
    bool RequireInvoiceDate,
    bool RequireCurrency,
    bool RequireSupplierTrn,
    bool AlwaysBackendOnReread,
    bool DetailedLineExtractionEnabled,
    bool SupplierMasterValidationEnabled,
    bool PurchaseOrderValidationEnabled,
    bool FinancialReconciliationEnabled,
    int Version);

public sealed class AdvancedInvoiceExtractionRequest
{
    [Required]
    public Guid OrganizationId { get; set; }
    public Guid? OperatingUnitId { get; set; }
    public string? Trigger { get; set; }
    public string? MobileSupplierName { get; set; }
    public string? MobileSupplierInvoiceNumber { get; set; }
    public string? MobilePurchaseOrderNumber { get; set; }
    public decimal? MobileInvoiceGross { get; set; }
    public decimal? MobileConfidence { get; set; }
    public string? OcrRequestId { get; set; }
}

public sealed record InvoiceOcrConfigurationResponse(
    Guid Id,
    Guid OrganizationId,
    bool MobileBasicOcrEnabled,
    bool AutomaticBackendFallbackEnabled,
    decimal MinimumMobileConfidence,
    bool RequireSupplierName,
    bool RequireInvoiceNumber,
    bool RequirePurchaseOrderNumber,
    bool RequireInvoiceAmount,
    bool RequireInvoiceDate,
    bool RequireCurrency,
    bool RequireSupplierTrn,
    string BackendProvider,
    bool AlwaysBackendOnReread,
    bool DetailedLineExtractionEnabled,
    bool SupplierMasterValidationEnabled,
    bool PurchaseOrderValidationEnabled,
    bool FinancialReconciliationEnabled,
    decimal AmountTolerance,
    int BackendTimeoutSeconds,
    int BackendRetryCount,
    bool ReuseCachedOcr,
    int Version,
    DateTime UpdatedAt);

public sealed class UpsertInvoiceOcrConfigurationRequest
{
    public bool MobileBasicOcrEnabled { get; set; } = true;
    public bool AutomaticBackendFallbackEnabled { get; set; } = true;
    [Range(0, 1)]
    public decimal MinimumMobileConfidence { get; set; } = 0.75m;
    public bool RequireSupplierName { get; set; } = true;
    public bool RequireInvoiceNumber { get; set; } = true;
    public bool RequirePurchaseOrderNumber { get; set; } = true;
    public bool RequireInvoiceAmount { get; set; } = true;
    public bool RequireInvoiceDate { get; set; }
    public bool RequireCurrency { get; set; }
    public bool RequireSupplierTrn { get; set; }
    [StringLength(100)]
    public string BackendProvider { get; set; } = "BUILT_IN_ADVANCED";
    public bool AlwaysBackendOnReread { get; set; } = true;
    public bool DetailedLineExtractionEnabled { get; set; } = true;
    public bool SupplierMasterValidationEnabled { get; set; } = true;
    public bool PurchaseOrderValidationEnabled { get; set; } = true;
    public bool FinancialReconciliationEnabled { get; set; } = true;
    [Range(0, 1000)]
    public decimal AmountTolerance { get; set; } = 0.05m;
    [Range(1, 600)]
    public int BackendTimeoutSeconds { get; set; } = 60;
    [Range(0, 10)]
    public int BackendRetryCount { get; set; } = 1;
    public bool ReuseCachedOcr { get; set; } = true;
}

public sealed record InvoiceExtractionHeaderResponse(
    Guid DocumentId,
    string? SupplierName,
    string? SupplierTrn,
    string? SupplierInvoiceNumber,
    DateOnly? InvoiceDate,
    string? PurchaseOrderNumber,
    decimal? InvoiceGross,
    decimal? InvoiceNet,
    string? Currency);

public sealed record InvoiceExtractionLineResponse(
    string? ItemSkuId,
    decimal? ItemAmount,
    decimal? ItemNet,
    string? ItemDescription,
    string? LineItemNumber,
    Guid? PurchaseOrderItemId);

public sealed record InvoiceExtractionResponse(
    InvoiceExtractionHeaderResponse Header,
    IReadOnlyList<InvoiceExtractionLineResponse> Lines,
    string Provider,
    string ExtractionMethod,
    decimal? Confidence,
    bool FallbackUsed,
    string Status,
    DateTime? CompletedAt);

public sealed record ExtractionAgentConfigResponse(
    Guid Id,
    Guid? OrganizationId,
    string Name,
    string DocumentType,
    string ProviderType,
    string? EndpointUrl,
    ExtractionAuthenticationType AuthenticationType,
    string? CredentialMask,
    int Priority,
    bool IsActive,
    DateTime CreatedAt,
    DateTime UpdatedAt);

public sealed class UpsertExtractionAgentConfigRequest
{
    [Required, StringLength(200)]
    public string Name { get; set; } = string.Empty;
    [Required, StringLength(50)]
    public string DocumentType { get; set; } = "INVOICE";
    [Required, StringLength(100)]
    public string ProviderType { get; set; } = "CUSTOM_REST";
    [Url, StringLength(2000)]
    public string? EndpointUrl { get; set; }
    public ExtractionAuthenticationType AuthenticationType { get; set; } = ExtractionAuthenticationType.NONE;
    [StringLength(500)]
    public string? CredentialReference { get; set; }
    [Range(1, 100000)]
    public int Priority { get; set; } = 100;
    public bool IsActive { get; set; }
    public string? ConfigurationJson { get; set; }
}

public sealed record ExtractionAgentTestResponse(bool Success, string Message);