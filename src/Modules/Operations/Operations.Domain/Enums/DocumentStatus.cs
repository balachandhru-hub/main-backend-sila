namespace Operations.Domain.Enums
{
    public enum DocumentStatus
    {
        UPLOADED,
        READING,
        BASIC_EXTRACTION_COMPLETE,
        FULL_EXTRACTION_PROCESSING,
        FULL_EXTRACTION_COMPLETE,
        REVIEW_REQUIRED,
        PROCESSED,
        FAILED,
        GRN_READY,
        GRN_POSTED
    }
}
