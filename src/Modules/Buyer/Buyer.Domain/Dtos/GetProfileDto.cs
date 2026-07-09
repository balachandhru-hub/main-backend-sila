using SharedKernel.Dto;

namespace Buyer.Domain.Dto
{
    public class OrganizationDto
    {
        public Guid Id { get; set; }

        public Guid OrganizationId { get; set; }

        public BusinessProfileDto BusinessProfile { get; set; } = new();

        public List<RegistrationDto> Registrations { get; set; } = new();

        public List<BankAccountDto> BankAccounts { get; set; } = new();

        public List<DeliveryLocationDto> DispatchLocations { get; set; } = new();
    }

    public class BusinessProfileDto
    {
        public string OrganizationName { get; set; }

        public string Email { get; set; }

        public string Phone { get; set; }

        public string Country { get; set; }

        public string AddressLine1 { get; set; }

        public string? AddressLine2 { get; set; }

        public string City { get; set; }

        public string State { get; set; }

        public string PinCode { get; set; }

        public string Industry { get; set; }

        public string BusinessType { get; set; }

        public int? EmployeeCount { get; set; }

        public decimal? AnnualTurnover { get; set; }

        public string Currency { get; set; }

        public int? YearEstablished { get; set; }

        public string? Website { get; set; }

        public string? Description { get; set; }
    }

    public class RegistrationDto
    {
        public string RegistrationType { get; set; }

        public string RegistrationNumber { get; set; }

        public string RegistrationName { get; set; }

        public AssetDto? Asset { get; set; }

        public DateTime? ExpiryDate { get; set; }
    }

    public class BankAccountDto
    {
        public string AccountHolderName { get; set; }

        public string BankName { get; set; }

        public string BranchName { get; set; }

        public string AccountNumber { get; set; }

        public string IFSCCode { get; set; }

        public string? SWIFTCode { get; set; }

        public string Currency { get; set; }

        public bool IsPrimary { get; set; }

        public bool IsVerified { get; set; }
    }

    public class DeliveryLocationDto
    {
        public string LocationName { get; set; }

        public string AddressLine1 { get; set; }

        public string? AddressLine2 { get; set; }

        public string City { get; set; }

        public string State { get; set; }

        public string Country { get; set; }

        public string PinCode { get; set; }

        public string? ContactPerson { get; set; }

        public string? ContactPhone { get; set; }

        public bool IsDefault { get; set; }
    }
}