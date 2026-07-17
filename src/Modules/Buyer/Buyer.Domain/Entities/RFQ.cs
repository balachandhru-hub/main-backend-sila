using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using SharedKernel.Models;

namespace Buyer.Domain.Entities
{
    public class RFQ : BaseModel
    {
        [Key]
        [Required]
        public Guid Id { get; set; }

        public string RFQNumber { get; set; }

        public Guid BuyerOrganizationId { get; set; }
        [Required]
        [ForeignKey("BuyerBusinessProfile")]
        public Guid BuyerId { get; set; }
        public BuyerBusinessProfile BuyerBusinessProfile { get; set; }

        public string Title { get; set; }

        public string Description { get; set; }

        public string DeliveryLocation { get; set; }

        public DateTime StartDate { get; set; }

        public DateTime EndDate { get; set; }

        public DateTime DeliveryTargetDate { get; set; }

        public string Status { get; set; }

        public string CostCenter { get; set; }

        public string Department { get; set; }

        public bool AddLotOption { get; set; }

        public decimal Budget { get; set; }

        public string? Region { get; set; }

        public decimal? Discount { get; set; }

        public decimal? TaxCharge { get; set; }

        public decimal? deliveryCharge { get; set; }
        public RFQ() { }

    }
}