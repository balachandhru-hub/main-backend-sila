namespace Operations.Domain.Dtos
{
    public class GrnValidationResponseDto
    {
        public bool Valid { get; set; }
        public List<string> Errors { get; set; } = new();
        public List<string> Warnings { get; set; } = new();
    }
}
