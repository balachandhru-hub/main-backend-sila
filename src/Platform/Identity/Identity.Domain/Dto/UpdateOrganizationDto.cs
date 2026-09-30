namespace Identity.Domain.Dto
{
    public class UpdateOrganizationDto
    {
        public Guid OrganizationId { get; set; }

        public string OrganizationName { get; set; }

        public string Email { get; set; }

        public string Phone { get; set; }

        public string Country { get; set; }

        public string AddressLine1 { get; set; }

        public string? AddressLine2 { get; set; }

        public string City { get; set; }

        public string State { get; set; }

        public string PinCode { get; set; }
    }
}