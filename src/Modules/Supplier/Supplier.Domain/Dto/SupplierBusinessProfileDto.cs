namespace Supplier.Domain.Dto
{
    public class SupplierBusinessProfileDto
    {
        public string OrganizationName { get; set; }

        public string Email { get; set; }

        public string Phone { get; set; }

        public bool EmailVerified { get; set; }

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
        public string? Status {get;set;}
        public string? Comment {get;set;}
    }
}