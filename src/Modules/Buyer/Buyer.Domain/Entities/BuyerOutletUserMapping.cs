using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using SharedKernel.Models;

namespace Buyer.Domain.Entities
{
    /// <summary>
    /// Assigns a buyer user to an outlet. A user who has assignments works only with those outlets.
    /// </summary>
    public class BuyerOutletUserMapping : BaseModel
    {
        [Key]
        [Required]
        public Guid Id { get; set; }

        [Required]
        [ForeignKey("Outlet")]
        public Guid OutletId { get; set; }

        public BuyerOutlet Outlet { get; set; } = null!;

        /// <summary>
        /// Identity user id. The user lives in the Identity service, so this is a reference, not a foreign key.
        /// </summary>
        [Required]
        public Guid UserId { get; set; }
    }
}
