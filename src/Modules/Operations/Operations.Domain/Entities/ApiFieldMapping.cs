using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Operations.Domain.Enums;
using SharedKernel.Models;

namespace Operations.Domain.Entities
{
    /// <summary>
    /// Mapping of a source field of the ERP payload to a target field of the purchase order or supplier.
    /// </summary>
    public class ApiFieldMapping : BaseModel
    {
        [Key]
        [Required]
        public Guid Id { get; set; }

        [Required]
        [ForeignKey("Configuration")]
        public Guid ConfigurationId { get; set; }

        public ApiIntegrationConfiguration Configuration { get; set; } = null!;

        [Required]
        [MaxLength(250)]
        public string SourceField { get; set; } = string.Empty;

        [Required]
        [MaxLength(250)]
        public string TargetField { get; set; } = string.Empty;

        [MaxLength(50)]
        public string? Transformation { get; set; }

        [Column(TypeName = "nvarchar(20)")]
        public IntegrationNullPolicy NullPolicy { get; set; } = IntegrationNullPolicy.IGNORE_NULL;

        [MaxLength(500)]
        public string? DefaultValue { get; set; }

        public bool IsValidated { get; set; }
    }
}
