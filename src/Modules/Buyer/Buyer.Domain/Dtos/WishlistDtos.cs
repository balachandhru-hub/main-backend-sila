namespace Buyer.Domain.Dtos
{
    public class WishlistItemWriteDto
    {
        public Guid MaterialId { get; set; }
        public decimal Quantity { get; set; }
        public decimal? UnitPrice { get; set; }
        public string? Currency { get; set; }
        public DateTime? RequiredDate { get; set; }
    }

    public class WishlistWriteDto
    {
        public Guid OutletId { get; set; }
        public string WishlistName { get; set; } = string.Empty;
        public string? Description { get; set; }
        public Guid? SupplierOrganizationId { get; set; }
        public string? SupplierName { get; set; }
        public Guid? MasterApprovalFlowId { get; set; }
        public string? Currency { get; set; }
        public string? DeliveryInstruction { get; set; }
        public DateTime? RequiredDate { get; set; }
        public List<WishlistItemWriteDto> Items { get; set; } = new();
    }

    public class WishlistItemResponseDto
    {
        public Guid Id { get; set; }
        public Guid MaterialId { get; set; }
        public string MaterialCode { get; set; } = string.Empty;
        public string MaterialName { get; set; } = string.Empty;
        public string? UnitOfMeasure { get; set; }
        public decimal Quantity { get; set; }
        public decimal? UnitPrice { get; set; }
        public string? Currency { get; set; }
        public DateTime? RequiredDate { get; set; }
    }

    public class WishlistApprovalStepDto
    {
        public Guid UserId { get; set; }
        public int Order { get; set; }
        public string Status { get; set; } = string.Empty;
        public string? Comment { get; set; }
        public DateTime? ActedOn { get; set; }
    }

    public class WishlistAuditDto
    {
        public string Action { get; set; } = string.Empty;
        public string? Detail { get; set; }
        public Guid? ActorUserId { get; set; }
        public DateTime DateCreated { get; set; }
    }

    public class WishlistIntegrationDto
    {
        public string IntegrationType { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public string? DocumentType { get; set; }
        public string? ExternalDocumentNumber { get; set; }
        public string? ErrorMessage { get; set; }
        public int RetryCount { get; set; }
        public DateTime? LastAttemptOn { get; set; }
        public DateTime? NextAttemptOn { get; set; }
        public string? CorrelationId { get; set; }
        public bool OutcomeUnknown { get; set; }
        public Guid? ConfigurationId { get; set; }
    }

    public class WishlistResponseDto
    {
        public Guid Id { get; set; }
        public Guid BuyerOrganizationId { get; set; }
        public Guid BuyerId { get; set; }
        public Guid OutletId { get; set; }
        public string? OutletName { get; set; }
        public string WishlistName { get; set; } = string.Empty;
        public string? Description { get; set; }
        public Guid? SupplierOrganizationId { get; set; }
        public string? SupplierName { get; set; }
        public string Status { get; set; } = string.Empty;
        public Guid? MasterApprovalFlowId { get; set; }
        public string? ApprovalName { get; set; }
        public Guid CreatedBy { get; set; }
        public DateTime DateCreated { get; set; }
        public Guid UpdatedBy { get; set; }
        public DateTime DateUpdated { get; set; }
        public DateTime? SubmittedOn { get; set; }
        public DateTime? FinalApprovedOn { get; set; }
        public string? BuyerErpDocumentType { get; set; }
        public string? BuyerErpDocumentNumber { get; set; }
        public string? SupplierErpDocumentNumber { get; set; }
        public string? Currency { get; set; }
        public string? DeliveryInstruction { get; set; }
        public DateTime? RequiredDate { get; set; }
        public string? LastError { get; set; }
        public bool IsFrozen { get; set; }
        public List<WishlistItemResponseDto> Items { get; set; } = new();
        public List<WishlistApprovalStepDto> ApprovalSteps { get; set; } = new();
        public List<WishlistAuditDto> Audit { get; set; } = new();
        public List<WishlistIntegrationDto> Integrations { get; set; } = new();
    }

    public class WishlistListItemDto
    {
        public Guid Id { get; set; }
        public string WishlistName { get; set; } = string.Empty;
        public string? OutletName { get; set; }
        public Guid CreatedBy { get; set; }
        public DateTime DateCreated { get; set; }
        public string Status { get; set; } = string.Empty;
        public string? ApprovalName { get; set; }
        public string? BuyerErpDocumentNumber { get; set; }
        public string? SupplierErpDocumentNumber { get; set; }
        public string? LastError { get; set; }
    }

    public class WishlistDecisionDto
    {
        public string Status { get; set; } = string.Empty;
        public string? Comment { get; set; }
    }

    public class OutletWriteDto
    {
        public string OutletName { get; set; } = string.Empty;
        public string? OutletCode { get; set; }
        public string? Description { get; set; }
        public string? ExternalShipTo { get; set; }
        public string? AddressLine1 { get; set; }
        public string? City { get; set; }
        public string? Country { get; set; }
    }

    public class OutletResponseDto
    {
        public Guid Id { get; set; }
        public string OutletName { get; set; } = string.Empty;
        public string? OutletCode { get; set; }
        public string? Description { get; set; }
        public string? ExternalShipTo { get; set; }
        public string? AddressLine1 { get; set; }
        public string? City { get; set; }
        public string? Country { get; set; }
    }

    public class ErpIntegrationWriteDto
    {
        public string ErpType { get; set; } = string.Empty;
        public string DocumentType { get; set; } = string.Empty;
        public string BaseUrl { get; set; } = string.Empty;
        public string CreateDocumentPath { get; set; } = string.Empty;
        public string HttpMethod { get; set; } = "POST";
        public string AuthType { get; set; } = string.Empty;
        public string? TokenUrl { get; set; }
        public string? Username { get; set; }
        public string? Password { get; set; }
        public string? ClientId { get; set; }
        public string? ClientSecret { get; set; }
        public string? Scope { get; set; }
        public string? ApiKeyHeader { get; set; }
        public string? ApiKey { get; set; }
        public string? AccessToken { get; set; }
        public string? HeadersJson { get; set; }
        public int TimeoutSeconds { get; set; } = 60;
        public int MaxRetryCount { get; set; } = 3;
        public bool IsActive { get; set; } = true;
    }

    public class ErpIntegrationResponseDto
    {
        public Guid Id { get; set; }
        public string ErpType { get; set; } = string.Empty;
        public string DocumentType { get; set; } = string.Empty;
        public string BaseUrl { get; set; } = string.Empty;
        public string CreateDocumentPath { get; set; } = string.Empty;
        public string HttpMethod { get; set; } = string.Empty;
        public string AuthType { get; set; } = string.Empty;
        public string? TokenUrl { get; set; }
        public string? Username { get; set; }
        public bool HasPassword { get; set; }
        public string? ClientId { get; set; }
        public bool HasClientSecret { get; set; }
        public string? Scope { get; set; }
        public string? ApiKeyHeader { get; set; }
        public bool HasApiKey { get; set; }
        public bool HasAccessToken { get; set; }
        public string? HeadersJson { get; set; }
        public int TimeoutSeconds { get; set; }
        public int MaxRetryCount { get; set; }
        public int Version { get; set; }
        public bool IsActive { get; set; }
    }
}
