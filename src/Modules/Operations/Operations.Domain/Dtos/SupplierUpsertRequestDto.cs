using System.ComponentModel.DataAnnotations;
using Operations.Domain.Enums;

namespace Operations.Domain.Dtos
{
    public class SupplierUpsertRequestDto
    {
        [Required, StringLength(100)] public string SupplierCode { get; set; } = string.Empty;
        [Required, StringLength(250)] public string Name { get; set; } = string.Empty;
        public string? SearchName { get; set; }
        public string? BusinessPartnerId { get; set; }
        public string? LegalName { get; set; }
        public string? TaxNumber { get; set; }
        public string? Trn { get; set; }
        public string? Email { get; set; }
        public string? Phone { get; set; }
        public string EntityCode { get; set; } = "DEFAULT";
        public string? Country { get; set; }
        public string? City { get; set; }
        public string? PostalCode { get; set; }
        public string? Street { get; set; }
        public string? Currency { get; set; }
        public bool IsBlocked { get; set; }
        public bool IsDeleted { get; set; }
        public StatusKind Status { get; set; } = StatusKind.ACTIVE;
        public List<string> Aliases { get; set; } = [];
    }
}
