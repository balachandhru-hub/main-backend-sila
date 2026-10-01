using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using SharedKernel.Models;

namespace Operations.Domain.Entities
{
    /// <summary>
    /// OData $metadata schema discovered for an integration configuration.
    /// </summary>
    public class IntegrationSchemaSnapshot : BaseModel
    {
        [Key]
        [Required]
        public Guid Id { get; set; }

        [Required]
        [ForeignKey("Configuration")]
        public Guid ConfigurationId { get; set; }

        public ApiIntegrationConfiguration Configuration { get; set; } = null!;

        [Required]
        [MaxLength(2000)]
        public string MetadataUrl { get; set; } = string.Empty;

        [Required]
        public string SchemaJson { get; set; } = string.Empty;

        public DateTime DiscoveredAt { get; set; }
    }
}
