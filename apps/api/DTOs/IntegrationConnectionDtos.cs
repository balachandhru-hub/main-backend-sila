namespace SilaMe.Api.DTOs;

public sealed record IntegrationConnectionCheck(string Name, bool Success, string? Code = null);

public sealed record IntegrationConnectionTestResult(
    bool Success,
    Guid? TestId,
    string Status,
    string System,
    string InterfaceType,
    IReadOnlyList<IntegrationConnectionCheck> Checks,
    DateTime TestedAt,
    DateTime? ExpiresAt,
    string? ErrorCode = null,
    string? Message = null,
    string? Fingerprint = null,
    string? RequestJson = null);
