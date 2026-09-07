namespace Buyer.Domain.Dto
{
    public class UpdateRFQStatusDto
    {
        public Guid RFQId { get; set; }
        public string Status { get; set; } 
    }
}