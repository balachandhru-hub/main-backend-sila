namespace Operations.Domain.Dtos
{
    public class SupplierMatchResponseDto
    {
        public Guid InvoiceId { get; set; }
        public Guid? SupplierId { get; set; }
        public string? SupplierName { get; set; }
        public List<SupplierMatchCandidateDto> Candidates { get; set; } = new();
        public bool RequiresSelection { get; set; }
    }
}
