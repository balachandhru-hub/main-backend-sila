namespace Supplier.Domain.Dto
{
    public class SupplierRegistrationResponseDto
    {
        public string RegistrationType { get; set; }

        public string RegistrationNumber { get; set; }

        public string? RegistrationName { get; set; }

        public AssetDto? Asset { get; set; }

        public DateTime? ExpiryDate { get; set; }
    }
}