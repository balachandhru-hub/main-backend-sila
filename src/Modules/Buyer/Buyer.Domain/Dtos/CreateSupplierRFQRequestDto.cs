namespace Buyer.Domain.Dto
{
    public class CreateSupplierRFQRequestDto
    {
        public Guid BuyerRFQId { get; set; }

        public string RFQNumber { get; set; }

        public Guid BuyerId { get; set; }

        public Guid SupplierId { get; set; }

        public Guid OrganizationId { get; set; }

        public List<Guid> InvitedUserIds { get; set; } = new();

        public string BuyerName { get; set; }

        public string Title { get; set; }

        public string Description { get; set; }

        public DateTime StartDate { get; set; }

        public DateTime EndDate { get; set; }

        public bool AddLotOption { get; set; }
        public string Currency { get; set; }

        public string Status { get; set; }
        public string DeliveryLocation { get; set; }
        public string? SessionToken { get; set; }
        public List<CreateSupplierRFQItemRequestDto>? Items { get; set; }
    }
}