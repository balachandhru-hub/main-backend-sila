namespace Buyer.Domain.Dto
{
    public class UpdateBuyerStatusDto
{
    public Guid OrganizationId { get; set; }
    public bool IsActive { get; set; }
}
}