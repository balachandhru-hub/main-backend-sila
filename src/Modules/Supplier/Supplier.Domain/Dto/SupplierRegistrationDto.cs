namespace Supplier.Domain.Dto
{
    public class SupplierRegistrationDto
    {
        public string RegistrationType { get; set; }

        public string RegistrationNumber { get; set; }

        public string? RegistrationName { get; set; }

        public AssetUploadDto? Asset { get; set; }

        public DateTime? ExpiryDate { get; set; }
    }
}