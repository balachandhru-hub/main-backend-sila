namespace Supplier.Domain.Dto
{
    public class SupplierInvitedRFQContractDto
    {
        public Guid RFQId { get; set; }

        public Guid SupplierRFQId { get; set; }

        public string RFQNumber { get; set; }

        public string Title { get; set; }

        public string? Status { get; set; }

        public DateTime EndDate { get; set; }

        public string DeliveryLocation { get; set; }

        public string BuyerName { get; set; }

        public bool ContractCreated { get; set; }

        public Guid? ContractId { get; set; }

        public string? ContractNumber { get; set; }

        public string? ContractStatus { get; set; }
    }
}
