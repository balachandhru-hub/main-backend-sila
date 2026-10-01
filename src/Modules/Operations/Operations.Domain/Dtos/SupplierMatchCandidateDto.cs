namespace Operations.Domain.Dtos
{
    public class SupplierMatchCandidateDto
    {
        public Guid SupplierId { get; set; }
        public string SupplierCode { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string? TaxNumber { get; set; }
        public decimal Confidence { get; set; }
        public string MatchReason { get; set; } = string.Empty;
    }
}
