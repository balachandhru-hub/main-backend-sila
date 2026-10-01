namespace Operations.Domain.Dtos
{
    /// <summary>
    /// Query string Microsoft sends to the OAuth redirect URI.
    /// </summary>
    public class MicrosoftCallbackRequestDto
    {
        public string? Code { get; set; }
        public string? State { get; set; }
        public string? Error { get; set; }
    }
}
