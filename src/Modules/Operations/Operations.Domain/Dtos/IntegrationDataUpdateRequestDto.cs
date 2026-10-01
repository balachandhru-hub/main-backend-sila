using Operations.Domain.Enums;

namespace Operations.Domain.Dtos
{
    /// <summary>
    /// Query string of the data-update list and export endpoints.
    /// </summary>
    public class IntegrationDataUpdateRequestDto
    {
        public IntegrationImportKind Kind { get; set; } = IntegrationImportKind.PURCHASE_ORDERS;
        public string? Search { get; set; }
        public string? Status { get; set; }
        public string? SortBy { get; set; }
        public bool Descending { get; set; }
        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 25;
    }
}
