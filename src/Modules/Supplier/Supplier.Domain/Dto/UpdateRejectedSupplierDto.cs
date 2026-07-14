
using SharedKernel.Dto;
namespace Supplier.Domain.Dto
{
    public class UpdateRejectedSupplierDto
    {
        public Guid SupplierId { get; set; }

        public UpdateSupplierBusinessProfileDto BusinessProfile { get; set; } 

        public List<UpdateSupplierRegistrationDto> Registrations { get; set; } 

        public List<UpdateSupplierBankAccountDto> BankAccounts { get; set; } 

        public List<UpdateSupplierDispatchLocationDto> DispatchLocations { get; set; } 
    }
 
    public class UpdateSupplierBusinessProfileDto
    {
        public string? Industry { get; set; }

        public string? BusinessType { get; set; }

        public int? EmployeeCount { get; set; }

        public decimal? AnnualTurnover { get; set; }

        public string? Currency { get; set; }

        public int? YearEstablished { get; set; }

        public string? Website { get; set; }

        public string? Description { get; set; }
    }
   

  
    public class UpdateSupplierRegistrationDto
    {
        public Guid Id { get; set; }

        public string? RegistrationType { get; set; }

        public string? RegistrationNumber { get; set; }

        public string? RegistrationName { get; set; }

        public AssetUploadDto? Asset { get; set; }

        public DateTime? ExpiryDate { get; set; }
    }




    public class UpdateSupplierBankAccountDto
    {
        public Guid Id { get; set; }

        public string? AccountHolderName { get; set; }

        public string? BankName { get; set; }

        public string? BranchName { get; set; }

        public string? AccountNumber { get; set; }

        public string? IFSCCode { get; set; }

        public string? SWIFTCode { get; set; }

        public string? IBAN { get; set; }

        public string? Currency { get; set; }

        public bool IsPrimary { get; set; }
    }


    public class UpdateSupplierDispatchLocationDto
    {
        public Guid Id { get; set; }

        public string? LocationName { get; set; }

        public string? AddressLine1 { get; set; }

        public string? AddressLine2 { get; set; }

        public string? City { get; set; }

        public string? State { get; set; }

        public string? Country { get; set; }

        public string? PinCode { get; set; }

        public string? ContactPerson { get; set; }

        public string? ContactEmail { get; set; }

        public string? ContactPhone { get; set; }

        public bool IsDefault { get; set; }
    }
}

