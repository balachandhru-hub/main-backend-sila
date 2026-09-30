namespace Supplier.Domain.Dto
{
    public class SupplierRFQListDto
    {
        public string RFQNumber { get; set; }

        public string Title { get; set; }

        public DateTime EndDate { get; set; }

        public string DeliveryLocation { get; set; }
        public string OrganizationName { get; set; }
        public Guid RFQId {get;set;}
        public Guid SupplierRFQId { get; set; }
        public string? Status { get; set; }
    }
}