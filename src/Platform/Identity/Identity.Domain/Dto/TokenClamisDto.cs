namespace Identity.Domain.Dto
{
    public class TokenClaimDto
    {
        public Guid UserId { get; set; }

        public Guid PersonId { get; set; }

        public Guid OrganizationId { get; set; }

        public Guid RoleId { get; set; }

        public List<string> Permissions { get; set; } 
    }
}