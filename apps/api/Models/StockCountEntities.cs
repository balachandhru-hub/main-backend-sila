namespace SilaMe.Api.Models;

public enum StockCountType { MONTHLY, PERIODIC, SURPRISE, ADHOC }

public enum StockCountStatus
{
    DRAFT, PLANNED, IN_PROGRESS, SUBMITTED, UNDER_REVIEW, ENQUIRY_PENDING,
    ADJUSTMENT_PENDING, POSTING, COMPLETED, CANCELLED
}

public enum StockCountLineStatus
{
    NOT_COUNTED, MATCHED, SHORTAGE, SURPLUS, RECOUNT_REQUIRED, ENQUIRY_REQUIRED,
    UNDER_REVIEW, APPROVED, REJECTED, POSTED
}

public enum StockCountMethod { BARCODE, PHOTO, SEARCH, MANUAL }

public enum ShortageEnquiryStatus
{
    CREATED, SENT, AWAITING_RESPONSE, RESPONDED, MORE_INFORMATION_REQUIRED,
    UNDER_REVIEW, ACCEPTED, REJECTED, CLOSED
}

public enum JustificationCategory
{
    BREAKAGE, SPILLAGE, UNRECORDED_CONSUMPTION, UNRECORDED_TRANSFER, COMPLIMENTARY_GUEST_RECOVERY,
    INCORRECT_PREVIOUS_COUNT, POS_RECIPE_MAPPING_ISSUE, UOM_PACK_CONVERSION_ISSUE,
    EXPIRED_OR_SPOILED, THEFT_SUSPECTED_LOSS, OTHER
}

public enum StockSapStatus { NONE, ADJUSTMENT_PENDING, POSTING, POSTED, FAILED, POSTING_UNKNOWN }

public sealed class MaterialBarcode
{
    public Guid Id { get; set; }
    public Guid OrganizationId { get; set; }
    public Guid MaterialId { get; set; }
    public required string Barcode { get; set; }
    public string BarcodeType { get; set; } = "CODE128";
    public string? PackUom { get; set; }
    public decimal? PackQuantity { get; set; }
    public bool Active { get; set; } = true;
    public DateTime CreatedAt { get; set; }
    public Material Material { get; set; } = null!;
}

public sealed class StockCountSession
{
    public Guid Id { get; set; }
    public Guid OrganizationId { get; set; }
    public required string CountNumber { get; set; }
    public StockCountType CountType { get; set; }
    public Guid? PropertyId { get; set; }
    public Guid InventoryLocationId { get; set; }
    public DateTime BusinessDate { get; set; }
    public bool BlindCount { get; set; }
    public StockCountStatus Status { get; set; } = StockCountStatus.PLANNED;
    public Guid CreatedBy { get; set; }
    public DateTime CreatedAt { get; set; }
    public Guid? StartedBy { get; set; }
    public DateTime? StartedAt { get; set; }
    public Guid? SubmittedBy { get; set; }
    public DateTime? SubmittedAt { get; set; }
    public Guid? ReviewedBy { get; set; }
    public DateTime? ReviewedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
    public string? Notes { get; set; }
    public InventoryLocation InventoryLocation { get; set; } = null!;
    public InventoryLocation? PropertyLocation { get; set; }
    public ICollection<StockCountLine> Lines { get; set; } = [];
}

public sealed class StockCountLine
{
    public Guid Id { get; set; }
    public Guid StockCountSessionId { get; set; }
    public Guid MaterialId { get; set; }
    public Guid InventoryLocationId { get; set; }
    public decimal SystemQty { get; set; }
    public required string SystemUom { get; set; }
    public decimal? PhysicalQty { get; set; }
    public string? PhysicalUom { get; set; }
    public decimal? FullQty { get; set; }
    public decimal? OpenQty { get; set; }
    public string? OpenUom { get; set; }
    public decimal? ConvertedPhysicalQty { get; set; }
    public string? BaseUom { get; set; }
    public decimal? VarianceQty { get; set; }
    public decimal? VariancePercent { get; set; }
    public decimal? UnitCost { get; set; }
    public decimal? VarianceValue { get; set; }
    public string? Currency { get; set; }
    public StockCountMethod? CountMethod { get; set; }
    public Guid? CountedBy { get; set; }
    public DateTime? CountedAt { get; set; }
    public StockCountLineStatus Status { get; set; } = StockCountLineStatus.NOT_COUNTED;
    public StockSapStatus SapStatus { get; set; } = StockSapStatus.NONE;
    public string? SapMaterialDocument { get; set; }
    public string? SapError { get; set; }
    public Guid? InventoryTransactionId { get; set; }
    public StockCountSession Session { get; set; } = null!;
    public Material Material { get; set; } = null!;
    public ICollection<StockCountCapture> Captures { get; set; } = [];
    public StockShortageEnquiry? Enquiry { get; set; }
}

public sealed class StockCountCapture
{
    public Guid Id { get; set; }
    public Guid StockCountLineId { get; set; }
    public int Sequence { get; set; }
    public decimal? FullQty { get; set; }
    public string? FullUom { get; set; }
    public decimal? OpenQty { get; set; }
    public string? OpenUom { get; set; }
    public decimal ConvertedQty { get; set; }
    public required string BaseUom { get; set; }
    public StockCountMethod Method { get; set; }
    public Guid CountedBy { get; set; }
    public DateTime CountedAt { get; set; }
    public string? Note { get; set; }
    public StockCountLine Line { get; set; } = null!;
}

public sealed class StockShortageEnquiry
{
    public Guid Id { get; set; }
    public Guid OrganizationId { get; set; }
    public required string EnquiryNumber { get; set; }
    public Guid StockCountSessionId { get; set; }
    public Guid StockCountLineId { get; set; }
    public Guid MaterialId { get; set; }
    public Guid InventoryLocationId { get; set; }
    public decimal SystemQty { get; set; }
    public decimal PhysicalQty { get; set; }
    public decimal ShortageQty { get; set; }
    public required string Uom { get; set; }
    public decimal? UnitCost { get; set; }
    public decimal? ShortageValue { get; set; }
    public string? Currency { get; set; }
    public Guid? AssignedManagerUserId { get; set; }
    public string? AssignedManagerGroup { get; set; }
    public bool ManagerConfigured { get; set; }
    public ShortageEnquiryStatus Status { get; set; } = ShortageEnquiryStatus.CREATED;
    public JustificationCategory? Category { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? RespondedAt { get; set; }
    public DateTime? ReviewedAt { get; set; }
    public DateTime? ClosedAt { get; set; }
    public StockCountLine Line { get; set; } = null!;
    public ICollection<StockShortageMessage> Messages { get; set; } = [];
}

public sealed class StockShortageMessage
{
    public Guid Id { get; set; }
    public Guid EnquiryId { get; set; }
    public required string Kind { get; set; }
    public JustificationCategory? Category { get; set; }
    public required string Comments { get; set; }
    public string? AttachmentName { get; set; }
    public Guid ActorUserId { get; set; }
    public DateTime CreatedAt { get; set; }
    public StockShortageEnquiry Enquiry { get; set; } = null!;
}
