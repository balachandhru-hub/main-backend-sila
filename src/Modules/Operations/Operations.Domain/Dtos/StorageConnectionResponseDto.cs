using Operations.Domain.Enums;

namespace Operations.Domain.Dtos
{
    public class StorageConnectionResponseDto
    {
        public Guid Id { get; set; }
        public Guid OrganizationId { get; set; }
        public DocumentStorageProvider Provider { get; set; }
        public string Name { get; set; } = string.Empty;
        public StorageConnectionStatus ConnectionStatus { get; set; }
        public string? TenantIdentifier { get; set; }
        public string? SiteIdentifier { get; set; }
        public string? DriveIdentifier { get; set; }
        public string? FolderIdentifier { get; set; }
        public string? DisplayUrl { get; set; }
        public string? DisplayName { get; set; }
        public DateTime? ValidatedAt { get; set; }
    }
}
