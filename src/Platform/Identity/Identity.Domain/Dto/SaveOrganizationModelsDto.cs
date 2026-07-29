namespace Identity.Domain.Dto
{
    public class SaveOrganizationModelsDto
{
    public List<Guid> ModelIds { get; set; }
    public Guid OrganizationId { get; set; }
}
}