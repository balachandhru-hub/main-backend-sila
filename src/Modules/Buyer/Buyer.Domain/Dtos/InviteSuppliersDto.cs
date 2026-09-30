namespace Buyer.Domain.Dto
{
    public class InviteSuppliersDto
{
    public Guid RFQId { get; set; }

    public string RFQNumber { get; set; }

    public Guid BuyerId { get; set; }

    public Guid RFQVerificationTemplateId { get; set; }

    public List<Guid> SupplierInvites { get; set; }
    public DateTime EndDate { get; set; }
    public Guid TemplateId { get; set; }
}
}