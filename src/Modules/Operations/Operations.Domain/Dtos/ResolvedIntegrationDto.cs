namespace Operations.Domain.Dtos
{
    /// <summary>
    /// The active API of an organization for one API type, as another service needs it to build the
    /// call. It carries no credentials: the call itself is made by this service.
    /// </summary>
    public class ResolvedIntegrationDto
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
