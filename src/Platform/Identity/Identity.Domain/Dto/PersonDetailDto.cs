namespace Identity.Domain.Dto
{
    public class PersonDetailDto
    {
        public Guid PersonId { get; set; }

        public Guid UserId { get; set; }

        public Guid OrganizationId { get; set; }

        public string Name { get; set; }

        public string Email { get; set; }

        public string Phone { get; set; }

        public string UserName { get; set; }

        public string AddressLine { get; set; }

        public string Country { get; set; }

        public Guid RoleId { get; set; }

        public string RoleName { get; set; }
    }
}