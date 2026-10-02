namespace Operations.Domain.Enums
{
    public enum IntegrationAuthenticationType
    {
        NONE,
        BASIC,
        BEARER_TOKEN,
        OAUTH2_CLIENT_CREDENTIALS,
        CUSTOM_TOKEN_ENDPOINT,
        API_KEY
    }
}
