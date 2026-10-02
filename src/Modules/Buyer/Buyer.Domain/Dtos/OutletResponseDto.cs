namespace Buyer.Domain.Dtos
{
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
        public Guid? MasterApprovalFlowId { get; set; }
        public string? ApprovalName { get; set; }
        public Guid? PropertyId { get; set; }
        public string? PropertyName { get; set; }
        public string? PlantCode { get; set; }
        public string? CompanyCode { get; set; }
        public string? StorageLocation { get; set; }
    }
}
