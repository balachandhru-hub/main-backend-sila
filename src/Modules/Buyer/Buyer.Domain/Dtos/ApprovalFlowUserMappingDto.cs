namespace Buyer.Domain.Dtos
{
    public class ApprovalFlowUserMappingDto
    {
        public Guid UserId { get; set; }

        public int Order { get; set; }
    }
}