using SharedKernel.Models;
using System.ComponentModel.DataAnnotations;

namespace Entities.Models
{
    public class Role : BaseModel
    {
        [Key]
        [Required]
        public Guid Id { get; set; }

        public string UserRole{ get; set; }


        public Role()
        {}
    }
}