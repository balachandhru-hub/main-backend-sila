using Operations.Domain.Enums;

namespace Operations.Domain.Dtos
{
    public class IntegrationSupplierRowDto
    {
        public Guid Id { get; set; }
        public string SupplierCode { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string? TaxNumber { get; set; }
        public StatusKind Status { get; set; }
        public DateTime UpdatedAt { get; set; }
    }
}
