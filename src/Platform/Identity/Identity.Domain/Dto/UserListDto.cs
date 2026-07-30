namespace Identity.Domain.Dto
{
    public class UserListDto
    {
        public Guid PersonId { get; set; }

        public Guid UserId { get; set; }

        public string Name { get; set; }

        public string Email { get; set; }

        public string UserName { get; set; }

        public Guid RoleId { get; set; }

        public string RoleName { get; set; }
    }
}