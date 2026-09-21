namespace Buyer.Domain.Dto
{
    public class SupplierContractStatusDto
    {
        public bool ContractCreated { get; set; }

        public Guid? ContractId { get; set; }

        public string? ContractNumber { get; set; }

        public string? ContractStatus { get; set; }
    }
}
