namespace Operations.Domain.Enums
{
    public enum StorageConnectionStatus
    {
        NOT_CONNECTED,
        PENDING,
        AUTHENTICATION_REQUIRED,
        AUTHENTICATING,
        AUTHENTICATED,
        SITE_SELECTION_REQUIRED,
        LIBRARY_SELECTION_REQUIRED,
        FOLDER_SELECTION_REQUIRED,
        VALIDATING,
        CONNECTED,
        CONSENT_REQUIRED,
        EXPIRED,
        VALIDATION_FAILED,
        DISCONNECTED,
        FAILED
    }
}
