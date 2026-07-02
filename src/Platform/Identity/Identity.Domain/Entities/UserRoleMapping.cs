using System.ComponentModel.DataAnnotations;
using System;
using System.ComponentModel.DataAnnotations.Schema;
using SharedKernel.Models;
namespace Identity.Domain.Entities
{
    public class UserRoleMapping : BaseModel
    {
        [Key]
        [Required]
        public Guid Id { get; set; }
        
        [Required]
        [ForeignKey("User")]
        public Guid UserId { get; set; }

        public User User { get; set; }

        public Guid RoleId { get; set; }

        public UserRoleMapping()
        {}
    }
}