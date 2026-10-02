using System.ComponentModel.DataAnnotations;
using SilaMe.Api.Models;

namespace SilaMe.Api.DTOs;

// PROTECTED SILA INVOICE RECEIVING FLOW — See docs/PROTECTED_INVOICE_FLOW.md.
// Document.invoiceId and InvoiceResponse fields are the Mobile Invoice Review contract.
public sealed record DocumentResponse(
    Guid Id,
    string Filename,
    string ContentType,
    long FileSizeBytes,
    int? PageCount,
    DocumentSourceChannel SourceChannel,
    DocumentStatus Status,
    DateTime CreatedAt,
    Guid InvoiceId,
    string SaveStatus = "SAVED",
    string NextStep = "PO_MATCH",
    string? Message = null);
public sealed record DocumentStatusResponse(Guid DocumentId, Guid InvoiceId, DocumentStatus DocumentStatus, InvoiceStatus InvoiceStatus, string? Message);

public sealed record InvoiceLineResponse(
    Guid Id,
    int LineNumber,
    string? SupplierMaterialCode,
    Guid? MaterialId,
    string Description,
    decimal? Quantity,
    string? Uom,
    decimal? UnitPrice,
    decimal? TaxRate,
    decimal? TaxAmount,
    decimal? LineAmount,
    decimal? Confidence,
    InvoiceLineMatchStatus MatchStatus,
    Guid? PurchaseOrderItemId);

public sealed record InvoiceResponse(
    Guid Id,
    Guid DocumentId,
    string InvoiceNumber,
    DateOnly? InvoiceDate,
    string? SupplierName,
    string? SupplierTaxNumber,
    string? PurchaseOrderNumber,
    Guid? PurchaseOrderId,
    string? Currency,
    decimal? NetAmount,
    decimal? TaxAmount,
    decimal? GrossAmount,
    InvoiceType InvoiceType,
    InvoiceStatus Status,
    decimal? OverallConfidence,
    Guid OrganizationId,
    Guid? OperatingUnitId,
    string? OperatingUnitName,
    Guid? SupplierId,
    string? SupplierCode,
    Guid? GoodsReceiptId,
    IReadOnlyList<InvoiceLineResponse> Lines,
    DateTime CreatedAt,
    DateTime UpdatedAt);

public sealed record SupplierResponse(
    Guid Id,
    string SupplierCode,
    string Name,
    string? LegalName,
    string? TaxNumber,
    string? Email,
    string? Phone,
    string EntityCode,
    string? Country,
    string? Currency,
    bool IsBlocked,
    bool IsDeleted,
    StatusKind Status,
    IReadOnlyList<string> Aliases,
    DateTime? LastSyncedAt,
    DateTime UpdatedAt,
    string? SearchName = null,
    string? BusinessPartnerId = null,
    string? Trn = null,
    string? City = null,
    string? PostalCode = null,
    string? Street = null,
    bool IsActive = true,
    string? SourceSystem = null,
    DateTime? SourceLastChangedAt = null);

public sealed record SupplierMatchCandidate(Guid SupplierId, string SupplierCode, string Name, string? TaxNumber, decimal Confidence, string MatchReason);

public sealed record SupplierMatchResponse(
    Guid InvoiceId,
    Guid? SupplierId,
    string? SupplierName,
    IReadOnlyList<SupplierMatchCandidate> Candidates,
    bool RequiresSelection);

public sealed record PurchaseOrderItemResponse(
    Guid Id,
    int LineNumber,
    Guid? MaterialId,
    string MaterialCode,
    string Description,
    decimal OrderedQuantity,
    decimal ReceivedQuantity,
    decimal OpenQuantity,
    string Uom,
    decimal? UnitPrice,
    PurchaseOrderItemStatus Status,
    string? ItemNumber = null,
    decimal? PriceQuantity = null,
    decimal? ItemAmount = null,
    string? TaxCode = null,
    decimal? TaxAmount = null,
    decimal? GrossItemAmount = null,
    string? Currency = null,
    string? MaterialGroup = null,
    string? Plant = null,
    string? StorageLocation = null,
    string? ItemCategory = null,
    string? AccountAssignmentCategory = null,
    bool GoodsReceiptExpected = true,
    bool InvoiceExpected = true,
    bool DeliveryCompleted = false,
    bool DeletionIndicator = false);

public sealed record PurchaseOrderResponse(
    Guid Id,
    string PoNumber,
    DateOnly? PoDate,
    DateOnly? DeliveryDate,
    string Currency,
    PurchaseOrderStatus Status,
    Guid OrganizationId,
    Guid? OperatingUnitId,
    string? OperatingUnitName,
    Guid SupplierId,
    string SupplierName,
    IReadOnlyList<PurchaseOrderItemResponse> Items,
    string EntityCode = "DEFAULT",
    string? PurchaseOrderType = null,
    string? CompanyCode = null,
    string? ErpSupplierId = null,
    string? PurchasingOrganization = null,
    string? PurchasingGroup = null,
    string? PaymentTerms = null,
    string? PoCategory = null,
    decimal? TotalNetAmount = null,
    decimal? TotalTaxAmount = null,
    decimal? TotalAmount = null,
    decimal? TotalOrderedQuantity = null,
    decimal? TotalReceivedQuantity = null,
    string? SourceSystem = null,
    DateTime? SourceLastChangedAt = null,
    DateTime? LastSyncedAt = null);

public sealed record GoodsReceiptLineResponse(
    Guid Id,
    Guid PurchaseOrderItemId,
    int PurchaseOrderLineNumber,
    string MaterialCode,
    string Description,
    decimal OpenQuantityBefore,
    decimal? InvoiceQuantity,
    decimal ReceivedQuantity,
    decimal AcceptedQuantity,
    decimal DamagedQuantity,
    decimal RejectedQuantity,
    string Uom,
    string? BatchNumber,
    DateOnly? ExpiryDate,
    decimal OrderedQuantity = 0);

public sealed record GoodsReceiptResponse(
    Guid Id,
    string GrnNumber,
    GoodsReceiptStatus Status,
    Guid PurchaseOrderId,
    string PurchaseOrderNumber,
    Guid? InvoiceId,
    string? InvoiceNumber,
    Guid SupplierId,
    string SupplierName,
    Guid OrganizationId,
    Guid OperatingUnitId,
    string OperatingUnitName,
    DateTime ReceiptDate,
    DateTime CreatedAt,
    DateTime? PostedAt,
    string? BusinessStatus,
    string? ErpPostingStatus,
    string? ErpMaterialDocument,
    string? ErpDocumentYear,
    string? ErpResponseJson,
    string? FailureCode,
    string? FailureMessage,
    int ErpAttemptCount,
    IReadOnlyList<GoodsReceiptLineResponse> Lines,
    string? CompanyCode = null,
    string? ExternalReference = null,
    string? AsnReference = null,
    string? ExternalSystem = null);

public sealed class PrepareGrnRequest
{
    [Required] public Guid InvoiceId { get; set; }
    public Guid? PurchaseOrderId { get; set; }
    public Guid? OperatingUnitId { get; set; }
}

public sealed class PostGoodsReceiptRequest
{
    public List<GrnLineInput>? Lines { get; set; }
}

public sealed class UpdateInvoiceRequest
{
    [Required, StringLength(150)]
    public string InvoiceNumber { get; set; } = string.Empty;
    public DateOnly? InvoiceDate { get; set; }
    public string? SupplierName { get; set; }
    public Guid? SupplierId { get; set; }
    public string? SupplierTaxNumber { get; set; }
    public string? PoNumber { get; set; }
    public string? Currency { get; set; }
    public decimal? NetAmount { get; set; }
    public decimal? TaxAmount { get; set; }
    public decimal? GrossAmount { get; set; }
    public bool NoPurchaseOrder { get; set; }
}

public sealed class MatchSupplierRequest
{
    public Guid? SupplierId { get; set; }
}

public sealed class MatchPurchaseOrderRequest
{
    public Guid? PurchaseOrderId { get; set; }
    public string? PoNumber { get; set; }
}

public sealed class MatchInvoiceLinesRequest
{
    [Required]
    public List<InvoiceLineMatchInput> Lines { get; set; } = [];
}

public sealed class InvoiceLineMatchInput
{
    [Required]
    public Guid InvoiceLineId { get; set; }
    public Guid? PurchaseOrderItemId { get; set; }
}

public class ValidateGrnRequest
{
    [Required]
    public Guid InvoiceId { get; set; }
    [Required]
    public Guid PurchaseOrderId { get; set; }
    public Guid? OperatingUnitId { get; set; }
    public DateTime? ReceiptDate { get; set; }
    [Required, MinLength(1)]
    public List<GrnLineInput> Lines { get; set; } = [];
}

public sealed class PostGrnRequest : ValidateGrnRequest { }

public sealed class GrnLineInput
{
    [Required]
    public Guid PurchaseOrderItemId { get; set; }
    [Range(0, double.MaxValue)]
    public decimal ReceivedQuantity { get; set; }
    [Range(0, double.MaxValue)]
    public decimal AcceptedQuantity { get; set; }
    [Range(0, double.MaxValue)]
    public decimal DamagedQuantity { get; set; }
    [Range(0, double.MaxValue)]
    public decimal RejectedQuantity { get; set; }
    public string? BatchNumber { get; set; }
    public DateOnly? ExpiryDate { get; set; }
}

public sealed record GrnValidationResponse(bool Valid, IReadOnlyList<string> Errors, IReadOnlyList<string> Warnings);

public sealed class SupplierUpsertRequest
{
    [Required, StringLength(100)] public string SupplierCode { get; set; } = string.Empty;
    [Required, StringLength(250)] public string Name { get; set; } = string.Empty;
    public string? SearchName { get; set; }
    public string? BusinessPartnerId { get; set; }
    public string? LegalName { get; set; }
    public string? TaxNumber { get; set; }
    public string? Trn { get; set; }
    public string? Email { get; set; }
    public string? Phone { get; set; }
    public string EntityCode { get; set; } = "DEFAULT";
    public string? Country { get; set; }
    public string? City { get; set; }
    public string? PostalCode { get; set; }
    public string? Street { get; set; }
    public string? Currency { get; set; }
    public bool IsBlocked { get; set; }
    public bool IsDeleted { get; set; }
    public StatusKind Status { get; set; } = StatusKind.ACTIVE;
    public List<string> Aliases { get; set; } = [];
}

public sealed record CompanyCodeMasterResponse(
    Guid Id, string CompanyCode, string CompanyName, string? Country, string? Currency, string? Address, string? City,
    string? TaxRegistrationNumber, string? SourceSystem, string? ExternalId, StatusKind Status, bool IsDeleted, DateTime UpdatedAt, bool IsActive);
public sealed record PropertyMasterResponse(
    Guid Id, string PropertyCode, string PropertyName, string? Country, string? CompanyCode, StatusKind Status, bool IsDeleted, DateTime UpdatedAt, bool IsActive);
public sealed record PlantMasterResponse(
    Guid Id, string PlantCode, string PlantName, string? CompanyCode, string? PlantType, string? PropertyCode, string? Address, string? City,
    string? Country, string? SourceSystem, string? ExternalId, StatusKind Status, bool IsDeleted, DateTime UpdatedAt, bool IsActive);
public sealed record StorageLocationMasterResponse(
    Guid Id, string StorageLocationCode, string StorageLocationName, string PlantCode, string? CompanyCode, string? LocationType,
    string? Description, string? SourceSystem, string? ExternalId, StatusKind Status, bool IsDeleted, DateTime UpdatedAt, bool IsActive);

public sealed class CompanyCodeUpsertRequest
{
    [Required] public string CompanyCode { get; set; } = string.Empty;
    [Required] public string CompanyName { get; set; } = string.Empty;
    public string? Country { get; set; }
    public string? Currency { get; set; }
    public string? Address { get; set; }
    public string? City { get; set; }
    public string? TaxRegistrationNumber { get; set; }
    public string? SourceSystem { get; set; }
    public string? ExternalId { get; set; }
    public StatusKind Status { get; set; } = StatusKind.ACTIVE;
}

public sealed class PropertyUpsertRequest
{
    [Required] public string PropertyCode { get; set; } = string.Empty;
    [Required] public string PropertyName { get; set; } = string.Empty;
    public string? Country { get; set; }
    public string? CompanyCode { get; set; }
    public StatusKind Status { get; set; } = StatusKind.ACTIVE;
}

public sealed class PlantUpsertRequest
{
    [Required] public string PlantCode { get; set; } = string.Empty;
    [Required] public string PlantName { get; set; } = string.Empty;
    public string? CompanyCode { get; set; }
    public string? PlantType { get; set; }
    public string? PropertyCode { get; set; }
    public string? Address { get; set; }
    public string? City { get; set; }
    public string? Country { get; set; }
    public string? SourceSystem { get; set; }
    public string? ExternalId { get; set; }
    public StatusKind Status { get; set; } = StatusKind.ACTIVE;
}

public sealed class StorageLocationUpsertRequest
{
    [Required] public string StorageLocationCode { get; set; } = string.Empty;
    [Required] public string StorageLocationName { get; set; } = string.Empty;
    [Required] public string PlantCode { get; set; } = string.Empty;
    public string? CompanyCode { get; set; }
    public string? LocationType { get; set; }
    public string? Description { get; set; }
    public string? SourceSystem { get; set; }
    public string? ExternalId { get; set; }
    public StatusKind Status { get; set; } = StatusKind.ACTIVE;
}