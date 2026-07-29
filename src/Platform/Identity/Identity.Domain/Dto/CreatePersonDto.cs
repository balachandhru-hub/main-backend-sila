namespace Identity.Domain.Dto
{
    public class CreatePersonDto
    {
        public string Name { get; set; }

        public string Email { get; set; }

        public string Phone { get; set; }

        public string Country { get; set; }

        public string AddressLine { get; set; }

        public string UserName { get; set; }

        public string Password { get; set; }

     public Guid RoleId { get; set; }
    }
}