namespace Identity.Domain.Dto
{
    public class UpdateOrganizationStatusDto
    {
        public Guid OrganizationId { get; set; }

        public bool IsActive { get; set; }
    }
}