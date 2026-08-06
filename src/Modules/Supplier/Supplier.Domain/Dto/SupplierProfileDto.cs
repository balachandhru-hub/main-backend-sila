namespace Supplier.Domain.Dto
{
    public class SupplierProfileDto
    {
        
    public Guid OrganizationId { get; set; }

    public string? SNID { get; set; }        
    public SupplierBusinessProfileDto BusinessProfile { get; set; }

    public List<SupplierRegistrationDto> Registrations { get; set; } 

    public List<SupplierBankAccountDto> BankAccounts { get; set; } 

    public List<SupplierDispatchLocationDto> DispatchLocations { get; set; } 

    public List<SupplierCategoryDto> SupplierCategories { get; set; } 
    
}
    }
