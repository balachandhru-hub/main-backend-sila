namespace Buyer.Domain.Dto
{
    public class UpdateVerificationRequestStatusDto
    {
        public Guid VerificationRequestId { get; set; }

        public string Status { get; set; }
    }
}