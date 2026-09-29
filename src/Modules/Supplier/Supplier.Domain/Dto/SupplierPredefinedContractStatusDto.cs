namespace Supplier.Domain.Dto
{
    public class SupplierPredefinedContractStatusDto
    {
        public bool ContractCreated { get; set; }

        public Guid? ContractId { get; set; }

        public string? ContractNumber { get; set; }

        public string? ContractStatus { get; set; }
    }
}
