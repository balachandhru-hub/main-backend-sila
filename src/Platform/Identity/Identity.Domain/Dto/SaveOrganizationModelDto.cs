namespace Identity.Domain.Dto
{
    public class SaveOrganizationModelDto
{
    public List<Guid> ModelIds { get; set; }
    public Guid OrganizationId { get; set; }
}
}