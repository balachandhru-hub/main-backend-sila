namespace Supplier.Domain.Dto
{
    public class SupplierProfileDto
    {
        public Guid Id { get; set; }

        public Guid OrganizationId { get; set; }

        public SupplierBusinessProfileDto BusinessProfile { get; set; }

        public List<SupplierRegistrationResponseDto> Registrations { get; set; } = new();

        public List<SupplierBankAccountDto> BankAccounts { get; set; } = new();

        public List<SupplierDispatchLocationDto> DispatchLocations { get; set; } = new();

        public List<SupplierCategoryDto> Categories { get; set; } = new();
    }
}