using SharedKernel.Models;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Identity.Domain.Entities
{
    public class User : BaseModel
    {
        [Key]
        [Required]
        public Guid Id { get; set; }

        [Required]
        [ForeignKey("Person")]
        public Guid PersonId { get; set; }
        public Person Person { get; set; }
        public string? UserName { get; set; }
        public string? UserSecret {get; set;}
        public Guid? UserType{get;set;}    
        public int FailedLoginAttempts { get; set; }
        public DateTime? LockoutEnd { get; set; }
        public DateTime? LastFailedLogin { get; set; }
        public User()
        { }
    }
}