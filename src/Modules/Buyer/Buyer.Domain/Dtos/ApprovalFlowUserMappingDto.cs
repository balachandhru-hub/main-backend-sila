namespace Buyer.Domain.Dtos
{
    public class ApprovalFlowUserMappingDto
    {
        public Guid Id { get; set; }
        public Guid UserId { get; set; }

        public int Order { get; set; }
    }
}