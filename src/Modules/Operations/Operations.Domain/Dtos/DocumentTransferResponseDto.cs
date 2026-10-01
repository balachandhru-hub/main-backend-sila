using Operations.Domain.Enums;

namespace Operations.Domain.Dtos
{
    public class DocumentTransferResponseDto
    {
        /// <summary>Id of the transfer job; this (or DestinationId) is accepted by the retry endpoint.</summary>
        public Guid Id { get; set; }
        public Guid DocumentId { get; set; }
        public Guid DestinationId { get; set; }
        public string? ExternalFileId { get; set; }
        public DocumentStorageProvider Provider { get; set; }
        public DocumentTransferStatus Status { get; set; }
        public string? ResolutionSource { get; set; }
        public string? FolderPath { get; set; }
        public string? ExternalFileName { get; set; }
        public string? ExternalWebUrl { get; set; }
        public int AttemptCount { get; set; }
        public DateTime? NextAttemptAt { get; set; }
        public DateTime? CompletedAt { get; set; }
        public string? LastErrorCode { get; set; }
        public string? LastErrorMessage { get; set; }
    }
}
