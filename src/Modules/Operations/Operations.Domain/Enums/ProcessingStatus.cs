namespace Operations.Domain.Enums
{
    public enum ProcessingStatus
    {
        QUEUED,
        PROCESSING,
        COMPLETED,
        PARTIAL,
        REVIEW_REQUIRED,
        FAILED
    }
}
