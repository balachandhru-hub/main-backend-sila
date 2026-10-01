namespace Operations.Application.Services.Integration
{
    /// <summary>
    /// Outcome of posting a goods receipt to the ERP. Unknown means the ERP may or may not have
    /// booked it (timeout, network error) and the receipt needs reconciliation.
    /// </summary>
    public class ErpGoodsReceiptResult
    {
        public bool Configured { get; set; }
        public bool Success { get; set; }
        public bool Unknown { get; set; }
        public int? HttpStatus { get; set; }
        public string? MaterialDocument { get; set; }
        public string? DocumentYear { get; set; }
        public string? ResponseJson { get; set; }
        public string? ErrorCode { get; set; }
        public string? ErrorMessage { get; set; }
    }
}
