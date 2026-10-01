using Operations.Domain.Enums;

namespace Operations.Domain.Dtos
{
    public class MicrosoftConnectionValidationResponseDto
    {
        public Guid ConnectionId { get; set; }
        public StorageConnectionStatus Status { get; set; }
        public string? TenantId { get; set; }
        public string? SiteId { get; set; }
        public string? SiteDisplayName { get; set; }
        public string? SiteWebUrl { get; set; }
        public string? DriveId { get; set; }
        public string? DriveName { get; set; }
        public string? FolderId { get; set; }
        public string? FolderPath { get; set; }
        public DateTime? ValidatedAt { get; set; }
        public string? Message { get; set; }
    }
}
