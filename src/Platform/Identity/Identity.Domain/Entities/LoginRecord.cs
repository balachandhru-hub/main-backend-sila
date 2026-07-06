using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using SharedKernel.Models;

namespace Identity.Domain.Entities
{
    public class LoginRecord : BaseModel
    {
        [Key]
        [Required]
        public Guid Id { get; set; } 

        [Required]
        [ForeignKey("User")]
        public Guid UserId { get; set; }
        public string? AttributeId { get; set; }

        [Required]
        public Guid OrganizationId { get; set; }

        public DateTime LoginTime { get; set; }

       public string? EntityType { get; set; }
        public string? IpAddress { get; set; }
        public string? UserName { get; set; }

        public LoginRecord() { }
    }
}
