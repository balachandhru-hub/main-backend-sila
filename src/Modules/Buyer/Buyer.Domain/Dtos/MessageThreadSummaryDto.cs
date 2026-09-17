namespace Buyer.Domain.Dto
{
    public class MessageThreadSummaryDto
    {
        public Guid ThreadId { get; set; }

        public Guid RFQId { get; set; }

        public string RFQNumber { get; set; }

        public Guid BuyerId { get; set; }

        public Guid? SupplierId { get; set; }

        public Guid? ExternalSupplierId { get; set; }

        public string? CounterpartyName { get; set; }

        public string? LastMessageBody { get; set; }

        public DateTime? LastMessageAt { get; set; }

        public int UnreadCount { get; set; }
    }
}
