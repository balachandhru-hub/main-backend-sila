namespace Operations.Domain.Dtos
{
    public class DocumentTransfersResponseDto
    {
        public Guid DocumentId { get; set; }
        public List<DocumentTransferResponseDto> Transfers { get; set; } = new();
    }
}
