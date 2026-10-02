using SilaMe.Api.Models;

namespace SilaMe.Api.DTOs;

public sealed record DocumentTransferResponse(
    Guid DocumentId,
    Guid DestinationId,
    string? ExternalFileId,
    DocumentStorageProvider Provider,
    DocumentTransferStatus Status,
    string? ResolutionSource,
    string? FolderPath,
    string? ExternalFileName,
    string? ExternalWebUrl,
    int AttemptCount,
    DateTime? NextAttemptAt,
    DateTime? CompletedAt,
    string? LastErrorCode,
    string? LastErrorMessage);