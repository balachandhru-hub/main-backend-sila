namespace Buyer.Domain.Dtos
{
    public class PendingApprovalDto
    {
        public Guid PredefinedMaterialId { get; set; }

        public Guid ApprovalFlowPredefinedMaterialId { get; set; }

        public Guid ApprovalMappingId { get; set; }

        public int Order { get; set; }

        public string? ApprovalStatus { get; set; }

        public string? MaterialCode { get; set; }

        public string? ProductType { get; set; }

        public string? Description { get; set; }

        public string? MaterialGroup { get; set; }

        public string? Status { get; set; }
    }
}