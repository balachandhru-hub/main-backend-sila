using System.ComponentModel.DataAnnotations;

namespace Operations.Domain.Dtos
{
    public class ValidateGrnRequestDto
    {
        [Required]
        public Guid InvoiceId { get; set; }
        [Required]
        public Guid PurchaseOrderId { get; set; }
        [Required]
        public Guid OperatingUnitId { get; set; }
        public DateTime? ReceiptDate { get; set; }
        [Required, MinLength(1)]
        public List<GrnLineInputDto> Lines { get; set; } = [];
    }
}
