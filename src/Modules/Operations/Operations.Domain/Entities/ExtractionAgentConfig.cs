using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Operations.Domain.Enums;
using SharedKernel.Models;

namespace Operations.Domain.Entities
{
    /// <summary>
    /// External extraction (OCR / AI agent) endpoint of an organization. Enabled is the user's on/off switch; IsActive is the solution-wide record flag.
    /// </summary>
    public class ExtractionAgentConfig : BaseModel
    {
        [Key]
        [Required]
        public Guid Id { get; set; }

        public Guid? OrganizationId { get; set; }

        [Required]
        [MaxLength(200)]
        public string Name { get; set; } = string.Empty;

        [Required]
        [MaxLength(50)]
        public string DocumentType { get; set; } = string.Empty;

        [Required]
        [MaxLength(100)]
        public string ProviderType { get; set; } = string.Empty;

        [MaxLength(2000)]
        public string? EndpointUrl { get; set; }

        [Column(TypeName = "nvarchar(30)")]
        public ExtractionAuthenticationType AuthenticationType { get; set; }

        [MaxLength(500)]
        public string? CredentialReference { get; set; }

        [MaxLength(8)]
        public string? CredentialLast4 { get; set; }

        public string? ConfigurationJson { get; set; }

        public int Priority { get; set; } = 100;

        public bool Enabled { get; set; }
    }
}
