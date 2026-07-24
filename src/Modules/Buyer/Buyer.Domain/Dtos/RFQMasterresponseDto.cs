namespace Buyer.Domain.Dto
{
    public class RFQListDto
    {
        public string RFQNumber { get; set; }

        public string Title { get; set; }

        public DateTime EndDate { get; set; }

        public string DeliveryLocation { get; set; }
        public string OrganizationName { get; set; }
        public Guid RFQId {get;set;}
    }
}