namespace Operations.Domain.Dtos
{
    public class SupplierSearchRequestDto
    {
        public string? EntityCode { get; set; }
        public string? Query { get; set; }
        public string? Status { get; set; }
    }
}
