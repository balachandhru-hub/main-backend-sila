using System.ComponentModel.DataAnnotations;
using SharedKernel.Models;

namespace Identity.Domain.Entities
{
    public class ApiKey : BaseModel
    {
        [Key]
        [Required]
        public Guid Id { get; set; }
        [Required]
        public string ApiKeyValue { get; set; }
        public ApiKey()
        { }
    }
}