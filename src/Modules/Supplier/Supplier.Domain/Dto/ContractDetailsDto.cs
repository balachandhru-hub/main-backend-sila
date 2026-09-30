namespace Supplier.Domain.Dto
{
    public class ContractAttachmentDetailsDto
    {
        public Guid Id { get; set; }
        public Guid AssetId { get; set; }
        public string? FileName { get; set; }
        public string? Title { get; set; }
        public long SegmentId { get; set; }
        public string? SegmentTitle { get; set; }
    }

    public class ContractDetailsResponseDto
    {
        public Guid Id { get; set; }
        public Guid PredefinedContractId { get; set; }
        public string? ContractNumber { get; set; }
        public string? ContractName { get; set; }
        public Guid RFQId { get; set; }
        public Guid BuyerId { get; set; }
        public Guid SupplierId { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public decimal Amount { get; set; }
        public string? Status { get; set; }
        public string? ContractStatus { get; set; }
        public DateTime DateCreated { get; set; }
        public List<ContractAttachmentDetailsDto> Attachments { get; set; } = new();
    }
}
