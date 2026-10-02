namespace Operations.Domain.Dtos
{
    public class SendIntegrationResponseDto
    {
        /// <summary>False when the API never answered (it could not be reached, timed out, or sign-in failed).</summary>
        public bool Answered { get; set; }

        /// <summary>
        /// True when the call may have reached the API although no answer came back. The document may
        /// exist, so the caller must not send it again on its own.
        /// </summary>
        public bool OutcomeUnknown { get; set; }
        public int StatusCode { get; set; }
        public string? ResponseBody { get; set; }
        public string? ErrorCode { get; set; }
        public string? ErrorMessage { get; set; }
        public long DurationMs { get; set; }
    }
}
