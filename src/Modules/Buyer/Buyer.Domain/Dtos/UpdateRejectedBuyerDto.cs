using SharedKernel.Dto;

namespace Buyer.Domain.Dto
{
    public class UpdateBuyerBusinessProfileDto
    {
        public Guid OrganizationId { get; set; }
        public string? OrganizationName { get; set; }
        public string? Email { get; set; }
        public string? Phone { get; set; }
        public string? Country { get; set; }
        public string? AddressLine1 { get; set; }
        public string? AddressLine2 { get; set; }
        public string? City { get; set; }
        public string? State { get; set; }
        public string? PinCode { get; set; }
        public string? Industry { get; set; }
        public string? BusinessType { get; set; }
        public int? EmployeeCount { get; set; }
        public decimal? AnnualTurnover { get; set; }
        public string? Currency { get; set; }
        public int? YearEstablished { get; set; }
        public string? Website { get; set; }
        public string? Description { get; set; }
        public string? Status {get;set;}
    }
     public class UpdateRejectedBuyerDto
{
    public Guid BuyerId { get; set; }

    public UpdateBuyerBusinessProfileDto BusinessProfile { get; set; } = default!;

    public List<UpdateBuyerCategoryDto>? BuyerCategories { get; set; }

    public List<UpdateBuyerBankAccountDto>? BuyerBankAccounts { get; set; }

    public List<UpdateBuyerDocumentRegistrationDto>? BuyerDocumentRegistrations { get; set; }

    public List<UpdateBuyerDeliveryLocationDto>? BuyerDeliveryLocations { get; set; }
}

    public class UpdateBuyerCategoryDto
    {
          public Guid Id { get; set; }
        public long  Segment { get; set; }

        public string? SegmentTitle { get; set; }

        public long? Family { get; set; }

        public string? FamilyTitle { get; set; }

        public long? Class { get; set; }

        public string? ClassTitle { get; set; }

        public long? Commodity { get; set; }

        public string? CommodityTitle { get; set; }
    }

    public class UpdateBuyerBankAccountDto
    {
          public Guid Id { get; set; }
        public string? AccountHolderName { get; set; }

        public string? BankName { get; set; }

        public string? BranchName { get; set; }

        public string? AccountNumber { get; set; }

        public string? IFSCCode { get; set; }

        public string? SWIFTCode { get; set; }

        public string? Currency { get; set; }

        public bool IsPrimary { get; set; }
    }

    public class UpdateBuyerDocumentRegistrationDto
    {
          public Guid Id { get; set; }
        public string? RegistrationNumber { get; set; }
        public string? RegistrationName { get; set; }
        public DateTime? ExpiryDate { get; set; }
        public string? RegistrationType { get; set; }
        public AssetUploadDto? RegistrationDocument { get; set; }
    }

    public class UpdateBuyerDeliveryLocationDto
    {
          public Guid Id { get; set; }
        public string?LocationName { get; set; }

        public string? AddressLine1 { get; set; }

        public string? AddressLine2 { get; set; }

        public string? City { get; set; }

        public string? State { get; set; }

        public string? Country { get; set; }

        public string? PinCode { get; set; }

        public string? ContactPerson { get; set; }

        public string? ContactPhone { get; set; }

        public bool IsDefault { get; set; }
    }
}