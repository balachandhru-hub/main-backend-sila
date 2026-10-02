using System.Security.Cryptography;
using Microsoft.EntityFrameworkCore;
using SilaMe.Api.Auth;
using SilaMe.Api.Data;
using SilaMe.Api.Models;
using SilaMe.Api.Tenancy;

namespace SilaMe.Api.Services;

public interface ISessionService
{
    Task<(Session Session, string RawToken)> CreateAsync(User user, ApplicationKind application, CancellationToken cancellationToken);
    Task<(Session Session, string RawToken)> CreateSupportAsync(
        Guid platformUserId,
        string platformDisplayName,
        string platformEmail,
        string platformRole,
        Guid environmentId,
        Guid sourcePlatformSessionId,
        string delegatedPermissionsCsv,
        CancellationToken cancellationToken);
    Task<Session?> ValidateAsync(string? rawToken, ApplicationKind application, CancellationToken cancellationToken);
    Task RevokeAsync(Session? session, CancellationToken cancellationToken);
}

public sealed class SessionService(
    SilaMeDbContext db,
    IConfiguration configuration,
    ITenantContextAccessor tenants,
    PlatformDbContext platform) : ISessionService
{
    private const int TokenBytes = 32;

    public async Task<(Session Session, string RawToken)> CreateAsync(
        User user,
        ApplicationKind application,
        CancellationToken cancellationToken)
    {
        var rawToken = Convert.ToBase64String(RandomNumberGenerator.GetBytes(TokenBytes));
        var now = DateTime.UtcNow;
        var session = new Session
        {
            Id = Guid.NewGuid(),
            UserId = user.Id,
            Application = application,
            TokenHash = Hash(rawToken),
            CreatedAt = now,
            LastUsedAt = now,
            ExpiresAt = now.Add(CloudSessionCookie.Lifetime(configuration)),
            ActorType = "CUSTOMER_USER",
            TenantId = tenants.Current?.TenantId,
            TenantEnvironmentId = tenants.Current?.EnvironmentId,
        };

        db.Sessions.Add(session);
        await db.SaveChangesAsync(cancellationToken);
        return (session, rawToken);
    }

    public async Task<(Session Session, string RawToken)> CreateSupportAsync(
        Guid platformUserId,
        string platformDisplayName,
        string platformEmail,
        string platformRole,
        Guid environmentId,
        Guid sourcePlatformSessionId,
        string delegatedPermissionsCsv,
        CancellationToken cancellationToken)
    {
        var now = DateTime.UtcNow;
        var previous = await db.Sessions
            .Where(item =>
                item.IsSupportSession &&
                item.PlatformUserId == platformUserId &&
                item.TenantEnvironmentId == environmentId &&
                item.RevokedAt == null)
            .ToListAsync(cancellationToken);
        foreach (var item in previous)
        {
            item.RevokedAt = now;
        }

        var rawToken = Convert.ToBase64String(RandomNumberGenerator.GetBytes(TokenBytes));
        var session = new Session
        {
            Id = Guid.NewGuid(),
            UserId = null,
            Application = ApplicationKind.CLOUD,
            TokenHash = Hash(rawToken),
            CreatedAt = now,
            LastUsedAt = now,
            ExpiresAt = now.Add(CloudSessionCookie.Lifetime(configuration)),
            IsSupportSession = true,
            SupportSessionId = Guid.NewGuid(),
            PlatformUserId = platformUserId,
            PlatformUserDisplayName = platformDisplayName,
            PlatformUserEmail = platformEmail,
            PlatformRole = platformRole,
            ActorType = "PLATFORM_USER",
            SourcePlatformSessionId = sourcePlatformSessionId,
            DelegatedPermissionsCsv = delegatedPermissionsCsv,
            TenantId = tenants.Current?.TenantId,
            TenantEnvironmentId = environmentId,
        };
        db.Sessions.Add(session);
        await db.SaveChangesAsync(cancellationToken);
        return (session, rawToken);
    }

    public async Task<Session?> ValidateAsync(
        string? rawToken,
        ApplicationKind application,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(rawToken))
        {
            return null;
        }

        var tokenHash = Hash(rawToken);
        var session = await db.Sessions
            .Include(candidate => candidate.User)
            .SingleOrDefaultAsync(candidate =>
                candidate.TokenHash == tokenHash &&
                candidate.Application == application &&
                candidate.RevokedAt == null,
                cancellationToken);

        if (session is null || session.ExpiresAt <= DateTime.UtcNow)
        {
            return null;
        }

        if (session.UserId is not null && (session.User is null || session.User.Status != StatusKind.ACTIVE))
        {
            return null;
        }

        if (session.TenantId is Guid boundTenant && tenants.Current is { } context && boundTenant != context.TenantId)
        {
            return null;
        }

        if (session.TenantEnvironmentId is Guid boundEnvironment && tenants.Current is { } envContext && boundEnvironment != envContext.EnvironmentId)
        {
            return null;
        }

        if (session.IsSupportSession && !await SupportStillAuthorizedAsync(session, cancellationToken))
        {
            session.RevokedAt = DateTime.UtcNow;
            await db.SaveChangesAsync(cancellationToken);
            return null;
        }

        session.LastUsedAt = DateTime.UtcNow;
        session.ExpiresAt = DateTime.UtcNow.Add(CloudSessionCookie.Lifetime(configuration));
        await db.SaveChangesAsync(cancellationToken);
        return session;
    }

    public async Task RevokeAsync(Session? session, CancellationToken cancellationToken)
    {
        if (session is null || session.RevokedAt is not null)
        {
            return;
        }

        session.RevokedAt = DateTime.UtcNow;
        await db.SaveChangesAsync(cancellationToken);
    }

    private async Task<bool> SupportStillAuthorizedAsync(Session session, CancellationToken cancellationToken)
    {
        if (session.PlatformUserId is not Guid platformUserId || session.SourcePlatformSessionId is not Guid platformSessionId)
        {
            return false;
        }

        var platformSession = await platform.PlatformSessions
            .Include(item => item.User).ThenInclude(item => item.Roles).ThenInclude(item => item.Role)
            .Include(item => item.User).ThenInclude(item => item.TenantAssignments)
            .SingleOrDefaultAsync(item => item.Id == platformSessionId, cancellationToken);
        if (platformSession is null
            || platformSession.RevokedAt is not null
            || platformSession.ExpiresAt <= DateTime.UtcNow
            || platformSession.PlatformUserId != platformUserId
            || platformSession.User.Status != PlatformUserStatus.ACTIVE)
        {
            return false;
        }

        if (tenants.Current is { } tenantContext
            && (tenantContext.TenantStatus == TenantStatus.SUSPENDED || tenantContext.TenantStatus == TenantStatus.ARCHIVED))
        {
            return false;
        }

        return session.TenantId is not Guid tenantId
            || session.TenantEnvironmentId is not Guid environmentId
            || PlatformAuthorization.CanLaunch(platformSession.User, tenantId, environmentId);
    }

    private static string Hash(string rawToken) =>
        Convert.ToHexString(SHA256.HashData(System.Text.Encoding.UTF8.GetBytes(rawToken)));
}
