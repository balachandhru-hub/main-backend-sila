using System.ComponentModel.DataAnnotations;

namespace Operations.Domain.Dtos
{
    public class MatchInvoiceLinesRequestDto
    {
        [Required]
        public List<InvoiceLineMatchInputDto> Lines { get; set; } = [];
    }
}
