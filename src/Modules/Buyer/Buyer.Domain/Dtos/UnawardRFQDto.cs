using System.ComponentModel.DataAnnotations;

namespace Buyer.Domain.Dto
{
    public class UnawardRFQDto
    {
        [Required]
        public Guid RFQId { get; set; }
    }
}
