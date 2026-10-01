namespace Operations.Domain.Dtos
{
    public class IntegrationTestResponseDto
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
        public int? HttpStatus { get; set; }
        public DateTime TestedAt { get; set; }
    }
}
