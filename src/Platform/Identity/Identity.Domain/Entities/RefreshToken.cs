using SharedKernel.Models;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Identity.Domain.Entities
{
    public class RefreshToken : BaseModel
    {
         [Key]
        [Required]
        public Guid Id { get; set; }
         [Required]
        [ForeignKey("User")]
        public Guid UserId {get;set;}
        public User User {get;set;}
        public string Token { get; set; }
        public DateTime ExpiresOn { get; set; }
        public bool Revoked { get; set; }
      
        public RefreshToken(){}
    }
}