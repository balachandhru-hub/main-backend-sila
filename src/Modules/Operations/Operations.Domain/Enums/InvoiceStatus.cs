namespace Operations.Domain.Enums
{
    public enum InvoiceStatus
    {
        UPLOADED,
        PROCESSING,
        REVIEW_REQUIRED,
        PO_MATCHED,
        READY_FOR_GRN,
        GRN_POSTED,
        OCR_FAILED,
        FAILED
    }
}
