namespace Operations.Domain.Dtos
{
    public class MicrosoftConnectResponseDto
    {
        public string AuthorizationUrl { get; set; } = string.Empty;
        public string State { get; set; } = string.Empty;
        public Guid DraftId { get; set; }
    }
}
