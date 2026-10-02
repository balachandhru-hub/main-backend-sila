using System.ComponentModel.DataAnnotations;
using SilaMe.Api.Models;

namespace SilaMe.Api.DTOs;

public sealed class LoginRequest
{
    [Required, EmailAddress]
    public string Email { get; set; } = string.Empty;

    [Required, MinLength(1)]
    public string Password { get; set; } = string.Empty;
}

public sealed record CurrentUserResponse(
    Guid Id,
    string DisplayName,
    string Email,
    ApplicationKind Application,
    StatusKind Status,
    Guid? TenantId = null,
    string? TenantCode = null,
    string? CustomerName = null,
    string? Environment = null,
    bool SupportSession = false,
    string? SupportUserDisplayName = null,
    IReadOnlyList<string>? ProductEntitlements = null,
    IReadOnlyList<string>? ModuleEntitlements = null,
    string ActorType = "CUSTOMER_USER",
    string? PlatformRole = null,
    string? EnvironmentCode = null);

public sealed record AuthResponse(CurrentUserResponse User);

public sealed record MobileAuthResponse(CurrentUserResponse User, string Token);

public sealed record LaunchConsumeRequest
{
    public string Code { get; set; } = string.Empty;
}

public sealed record ApiError(
    string Code,
    string Message,
    IReadOnlyList<string>? MissingFields = null,
    Guid? ExistingInvoiceId = null,
    string? DuplicateType = null);