namespace Buyer.Domain.Dtos
{
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
}
