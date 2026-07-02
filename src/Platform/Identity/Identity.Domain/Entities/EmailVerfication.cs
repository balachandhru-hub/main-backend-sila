using SharedKernel.Models;
using System.ComponentModel.DataAnnotations;

namespace Identity.Domain.Entities
{
    public class EmailVerification : BaseModel
    {
        [Key]
        [Required]
        public Guid Id { get; set; }

        [Required]
        [EmailAddress]
        public string Email { get; set; }
        public string OtpHash { get; set; }
        public DateTime ExpiresOn { get; set; }
        public int AttemptCount { get; set; }
        public bool IsVerified { get; set; }
        public string? IpAddress { get; set; }
        public string? TemporaryVerificationToken { get; set; }
        public DateTime? TemporaryVerificationTokenExpiresOn { get; set; }

        public EmailVerification()
        {
        }
    }
}