using System.ComponentModel.DataAnnotations;
using MasterData.Domain.Common;

namespace MasterData.Domain.Entities
{
    public class ApiConfig : BaseEntity
    {
        [Key]
        [Required]
        public Guid Id { get; set; }

        [Required]
        public string? Description { get; set; }

        [Required]
        public string BaseUrl { get; set; }

        [Required]
        public string? Username { get; set; }

        [Required]
        public string? Password { get; set; }

        public string? ClientId { get; set; }

        public string? ClientSecret { get; set; }

        public string? TenantId { get; set; }

        public ApiConfig() { }
    }
}