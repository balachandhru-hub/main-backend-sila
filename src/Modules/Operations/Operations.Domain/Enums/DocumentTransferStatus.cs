namespace Operations.Domain.Enums
{
    public enum DocumentTransferStatus
    {
        PENDING,
        PROCESSING,
        COMPLETED,
        RETRY_PENDING,
        FAILED,
        FAILED_AUTHENTICATION,
        SKIPPED
    }
}
