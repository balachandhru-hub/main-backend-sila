using Microsoft.EntityFrameworkCore;
using SilaMe.Api.Data;
using SilaMe.Api.DTOs;
using SilaMe.Api.Models;
using SilaMe.Api.Tenancy;

namespace SilaMe.Api.Services;

public interface IAuthService
{
    Task<(User User, Session Session, string RawToken)?> LoginAsync(
        LoginRequest request,
        ApplicationKind application,
        CancellationToken cancellationToken);
}

public sealed class AuthService(
    SilaMeDbContext db,
    IPasswordService passwordService,
    ISessionService sessionService,
    IEntitlementService entitlements,
    ILogger<AuthService> logger) : IAuthService
{
    public async Task<(User User, Session Session, string RawToken)?> LoginAsync(
        LoginRequest request,
        ApplicationKind application,
        CancellationToken cancellationToken)
    {
        var normalizedEmail = request.Email.Trim().ToUpperInvariant();
        var user = await db.Users
            .Include(candidate => candidate.ApplicationAccess)
            .SingleOrDefaultAsync(candidate => candidate.NormalizedEmail == normalizedEmail, cancellationToken);

        var hasAccess = user?.ApplicationAccess.Any(access =>
            access.Application == application && access.Status == StatusKind.ACTIVE) == true;
        if (user is null || user.Status != StatusKind.ACTIVE || !hasAccess || !passwordService.VerifyPassword(user, request.Password))
        {
            logger.LogInformation("Login failed for application {Application}", application);
            return null;
        }

        try
        {
            var products = (await entitlements.ListAsync(cancellationToken)).Products;
            if (!products.Contains(ProductCodes.SilaMe, StringComparer.OrdinalIgnoreCase))
            {
                logger.LogInformation("Login rejected because SILA_ME is not entitled");
                return null;
            }
        }
        catch (TenantException)
        {
            logger.LogInformation("Login rejected because product entitlements could not be resolved");
            return null;
        }

        user.LastLoginAt = DateTime.UtcNow;
        user.UpdatedAt = DateTime.UtcNow;
        var session = await sessionService.CreateAsync(user, application, cancellationToken);
        logger.LogInformation("Login succeeded for application {Application}", application);
        await db.SaveChangesAsync(cancellationToken);
        return (user, session.Session, session.RawToken);
    }
}