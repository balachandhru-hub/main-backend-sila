namespace Operations.Domain.Dtos
{
    public class AdvancedExtractedFieldDto<T>
    {
        public T? Value { get; set; }
        public decimal? Confidence { get; set; }
        public string ConfidenceBand { get; set; } = string.Empty;
        public int? PageNumber { get; set; }
        public string? SourceLabel { get; set; }
        public string? ExtractionMethod { get; set; }
    }
}
