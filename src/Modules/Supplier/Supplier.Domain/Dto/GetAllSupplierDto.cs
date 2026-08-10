namespace Supplier.Domain.Dto
{
    public class GetAllSupplierDto
    {
        public Guid OrganizationId { get; set; }
        public string OrganizationName { get; set; }
        public string SNID { get; set; }

        public string Email { get; set; }

        public string Phone { get; set; }     

        public string Country { get; set; }  
        public string City { get; set; }

        public string State { get; set; }

        public string Industry { get; set; }

        public string BusinessType { get; set; } 

        public int? YearEstablished { get; set; }

        public string? Website { get; set; }

        public string? Description { get; set; }

    }
}