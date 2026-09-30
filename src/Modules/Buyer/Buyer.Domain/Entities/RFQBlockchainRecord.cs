using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using SharedKernel.Models;
namespace Buyer.Domain.Entities
{
    public class RFQBlockchainRecord: BaseModel
    {
        [Key]
        [Required]
        public Guid Id { get; set; }
        [Required]
        [ForeignKey("RFQ")]       
         public Guid RFQId { get; set; }

        public string EntityType { get; set; } 


        public string EventType { get; set; } 

        public string DataHash { get; set; }

        public string? BlockchainTransactionId { get; set; }

        public long? BlockNumber { get; set; }

        public string? BlockchainNetwork { get; set; } 

        public RFQBlockchainRecord() { }

    }
}