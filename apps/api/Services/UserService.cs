using SilaMe.Api.DTOs;
using SilaMe.Api.Models;
using SilaMe.Api.Tenancy;

namespace SilaMe.Api.Services;

public interface IUserService
{
    CurrentUserResponse ToResponse(User? user, ApplicationKind application, Session? session = null);
}

public sealed class UserService(ITenantContextAccessor tenants, IEntitlementService entitlements) : IUserService
{
    public CurrentUserResponse ToResponse(User? user, ApplicationKind application, Session? session = null)
    {
        var context = tenants.Current;
        IReadOnlyList<string>? products = null;
        IReadOnlyList<string>? modules = null;
        try
        {
            var listed = entitlements.ListAsync(CancellationToken.None).GetAwaiter().GetResult();
            products = listed.Products;
            modules = listed.Modules;
        }
        catch
        {
            // Entitlements are additive; missing platform rows must not break /api/me.
        }

        var support = session?.IsSupportSession == true;
        return new(
            user?.Id ?? session?.PlatformUserId ?? Guid.Empty,
            support ? session!.PlatformUserDisplayName ?? "SILA Platform" : user?.DisplayName ?? "User",
            support ? session!.PlatformUserEmail ?? "platform@silame.internal" : user?.Email ?? string.Empty,
            application,
            user?.Status ?? StatusKind.ACTIVE,
            context?.TenantId,
            context?.TenantCode,
            context?.CustomerName,
            context?.EnvironmentType.ToString(),
            support,
            session?.PlatformUserDisplayName,
            products,
            modules,
            session?.ActorType ?? "CUSTOMER_USER",
            session?.PlatformRole,
            TenantOperationalDatabase.EnvironmentCode(context?.EnvironmentType ?? TenantEnvironmentType.TEST));
    }
}
