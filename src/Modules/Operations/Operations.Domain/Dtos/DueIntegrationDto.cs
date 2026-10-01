namespace Operations.Domain.Dtos
{
    /// <summary>
    /// An active integration whose scheduled pull is due.
    /// </summary>
    public class DueIntegrationDto
    {
        public Guid OrganizationId { get; set; }
        public Guid ConfigurationId { get; set; }
    }
}
