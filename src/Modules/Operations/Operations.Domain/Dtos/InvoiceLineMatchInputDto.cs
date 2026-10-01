using System.ComponentModel.DataAnnotations;

namespace Operations.Domain.Dtos
{
    public class InvoiceLineMatchInputDto
    {
        [Required]
        public Guid InvoiceLineId { get; set; }
        public Guid? PurchaseOrderItemId { get; set; }
    }
}
