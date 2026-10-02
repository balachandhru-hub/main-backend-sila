namespace Buyer.Domain.Dtos
{
    public class IntegrationSendResultDto
    {
        /// <summary>False when the API never answered (it could not be reached, timed out, or sign-in failed).</summary>
        public bool Answered { get; set; }

        /// <summary>True when the document may exist although no clear answer came back.</summary>
        public bool OutcomeUnknown { get; set; }
        public int StatusCode { get; set; }
        public string? ResponseBody { get; set; }
        public string? ErrorCode { get; set; }
        public string? ErrorMessage { get; set; }
        public long DurationMs { get; set; }
    }
}
