using System.ComponentModel.DataAnnotations;

namespace Operations.Domain.Dtos
{
    public class GrnLineInputDto
    {
        [Required]
        public Guid PurchaseOrderItemId { get; set; }
        [Range(0, double.MaxValue)]
        public decimal ReceivedQuantity { get; set; }
        [Range(0, double.MaxValue)]
        public decimal AcceptedQuantity { get; set; }
        [Range(0, double.MaxValue)]
        public decimal DamagedQuantity { get; set; }
        [Range(0, double.MaxValue)]
        public decimal RejectedQuantity { get; set; }
        public string? BatchNumber { get; set; }
        public DateOnly? ExpiryDate { get; set; }
    }
}
