namespace Buyer.Domain.Dtos
{
    public class PropertyResponseDto
    {
        public Guid Id { get; set; }
        public string CompanyCode { get; set; } = string.Empty;
        public string PlantCode { get; set; } = string.Empty;
        public string PropertyName { get; set; } = string.Empty;
        public Guid? MasterApprovalFlowId { get; set; }
        public string? ApprovalName { get; set; }
    }
}
