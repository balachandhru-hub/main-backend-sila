namespace Operations.Domain.Dtos
{
    public class SendIntegrationRequestDto
    {
        public Guid OrganizationId { get; set; }
        public Guid ConfigurationId { get; set; }

        /// <summary>The document to send, already in the API's payload format.</summary>
        public string Body { get; set; } = string.Empty;

        /// <summary>Headers of this one call, for example an idempotency key.</summary>
        public Dictionary<string, string>? Headers { get; set; }
    }
}
