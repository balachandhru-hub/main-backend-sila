using Operations.Domain.Enums;

namespace Operations.Domain.Dtos
{
    public class SupplierResponseDto
    {
        public Guid Id { get; set; }
        public string SupplierCode { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string? LegalName { get; set; }
        public string? TaxNumber { get; set; }
        public string? Email { get; set; }
        public string? Phone { get; set; }
        public string EntityCode { get; set; } = string.Empty;
        public string? Country { get; set; }
        public string? Currency { get; set; }
        public bool IsBlocked { get; set; }
        public bool IsDeleted { get; set; }
        public StatusKind Status { get; set; }
        public List<string> Aliases { get; set; } = new();
        public DateTime? LastSyncedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
        public string? SearchName { get; set; }
        public string? BusinessPartnerId { get; set; }
        public string? Trn { get; set; }
        public string? City { get; set; }
        public string? PostalCode { get; set; }
        public string? Street { get; set; }
        public bool IsActive { get; set; } = true;
        public string? SourceSystem { get; set; }
        public DateTime? SourceLastChangedAt { get; set; }
    }
}
