using System.ComponentModel.DataAnnotations;
using Operations.Domain.Enums;

namespace Operations.Domain.Dtos
{
    public class UpdateOrganizationUnitRequestDto
    {
        [Required, StringLength(200, MinimumLength = 2)]
        public string Name { get; set; } = string.Empty;

        [Required]
        public OrganizationUnitKind Kind { get; set; }

        public Guid? ParentUnitId { get; set; }

        public StatusKind Status { get; set; } = StatusKind.ACTIVE;
    }
}
