using Microsoft.EntityFrameworkCore;
using Operations.Domain.Enums;
using Operations.Infrastructure.Contracts.IRepository;
using SharedKernel.ExceptionHandler;
using SharedKernel.LoggerServices;

namespace Operations.Application.Features.Shared
{
    /// <summary>
    /// Tenant guard shared by the handlers. Permissions are enforced by [ApiAuthorization] on the
    /// controllers; this makes sure an operating unit named by a request is an active unit of the
    /// caller's organization (the organization itself always comes from the token).
    /// </summary>
    internal static class OperationsScope
    {
        public static async Task EnsureUnitAsync(
            IRepositoryWrapper repository,
            ILoggerManager logger,
            Guid organizationId,
            Guid? operatingUnitId,
            CancellationToken cancellationToken)
        {
            if (operatingUnitId == null)
            {
                return;
            }

            bool exists = await repository.OrganizationUnit
                .FindByCondition(x => x.Id == operatingUnitId && x.OrganizationId == organizationId && x.IsActive && x.Status == StatusKind.ACTIVE)
                .AnyAsync(cancellationToken);
            if (!exists)
            {
                logger.LogError($"Operating unit not found. OperatingUnitId: {operatingUnitId}, OrganizationId: {organizationId}");
                throw new NotFoundCustomException("Operating unit not found.", "Select an active unit of your organization.");
            }
        }
    }
}
