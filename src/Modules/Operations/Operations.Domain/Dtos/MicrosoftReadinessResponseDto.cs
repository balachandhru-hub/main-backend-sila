namespace Operations.Domain.Dtos
{
    public class MicrosoftReadinessResponseDto
    {
        public bool TenantConfigured { get; set; }
        public bool ClientIdConfigured { get; set; }
        public bool ClientSecretConfigured { get; set; }
        public bool RedirectUriConfigured { get; set; }
        public bool TokenEncryptionConfigured { get; set; }
        public bool GraphIntegrationReady { get; set; }
        public string? RedirectUri { get; set; }
    }
}
