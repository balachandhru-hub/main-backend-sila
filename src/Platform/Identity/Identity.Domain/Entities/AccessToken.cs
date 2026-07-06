using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using SharedKernel.Models;
namespace Identity.Domain.Entities
{
    public class AccessToken : BaseModel
    {
        [Key]
        [Required]
        public Guid Id { get; set; }
 
        [Required]
        [ForeignKey("User")]
        public Guid UserId { get; set; }
 
        [Required]
        public string AccessTokenValue { get; set; }
 
        public DateTime ExpiredTime { get; set; }
        public Guid RefreshToken {get; set; }
       
        public AccessToken()
        { }
    }
}