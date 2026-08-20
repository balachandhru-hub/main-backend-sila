namespace Buyer.Domain.Dto
{
    public class SupplierProfileDto
    {
        public Guid Id { get; set; }

        public Guid OrganizationId { get; set; }

        public SupplierBusinessProfileDto BusinessProfile { get; set; }

        public List<SupplierRegistrationResponseDto> Registrations { get; set; }

        public List<SupplierBankAccountDto> BankAccounts { get; set; } = new();

        public List<SupplierDispatchLocationDto> DispatchLocations { get; set; } 
        public List<SupplierVerificationQuestionAndAnswerDto>? Questions { get; set; }
    }
}