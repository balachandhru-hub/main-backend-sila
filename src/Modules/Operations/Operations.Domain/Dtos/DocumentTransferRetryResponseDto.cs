using Operations.Domain.Enums;

namespace Operations.Domain.Dtos
{
    public class DocumentTransferRetryResponseDto
    {
        public Guid TransferId { get; set; }
        public DocumentTransferStatus Status { get; set; }
    }
}
