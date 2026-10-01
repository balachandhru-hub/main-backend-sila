using Operations.Domain.Enums;

namespace Operations.Domain.Dtos
{
    public class OrganizationUnitResponseDto
    {
        public Guid Id { get; set; }
        public Guid OrganizationId { get; set; }
        public Guid? ParentUnitId { get; set; }
        public string Code { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public OrganizationUnitKind Kind { get; set; }
        public StatusKind Status { get; set; }
    }
}
