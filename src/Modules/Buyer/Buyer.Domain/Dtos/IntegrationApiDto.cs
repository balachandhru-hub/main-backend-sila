namespace Buyer.Domain.Dtos
{
    /// <summary>
    /// The organization's active API for one API type, as the integration service describes it.
    /// It carries no credentials: the integration service makes the call.
    /// </summary>
    public class IntegrationApiDto
    {
        public bool Configured { get; set; }
        public Guid? ConfigurationId { get; set; }
        public string? Name { get; set; }
        public string? SystemName { get; set; }
        public string? EntityCode { get; set; }
        public string? BaseUrl { get; set; }
        public string? ResourcePath { get; set; }
        public string? HttpMethod { get; set; }
        public string? PayloadFormat { get; set; }
        public string? RequestBody { get; set; }
    }
}
