using SharedKernel.Dto;

namespace Supplier.Domain.Dto
{
    public class OrganizationDto
    {
        public Guid? OrganizationId { get; set; }

        public string SNID { get; set; }
        public List<GetAllSupplierDto> Suppliers { get; set; } = new();
        public SupplierBusinessProfileDto BusinessProfile { get; set; } = new();

        public List<SupplierRegistrationResponseDto> Registrations { get; set; } = new();

        public List<SupplierBankAccountDto> BankAccounts { get; set; } = new();

        public List<SupplierDispatchLocationDto> DispatchLocations { get; set; } = new();

        public List<SupplierCategoryDto> SupplierCategories { get; set; } = new();
    }
}