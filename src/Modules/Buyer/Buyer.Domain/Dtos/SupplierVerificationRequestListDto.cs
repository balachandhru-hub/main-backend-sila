namespace Buyer.Domain.Dto
{
    public class SupplierVerificationRequestListDto
    {
        public Guid RequestId { get; set; }

        public string RFQNumber { get; set; }

        public Guid SupplierOrganizationId { get; set; }

        public string SupplierName { get; set; }

        public string TemplateName { get; set; }

        public string Status { get; set; }

        public DateTime? DueDate { get; set; }

        public DateTime DateCreated { get; set; }
        public Guid BuyerOrganizationId { get; set; }
    }
}