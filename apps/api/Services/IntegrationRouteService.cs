using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using SilaMe.Api.Data;
using SilaMe.Api.DTOs;
using SilaMe.Api.Models;

namespace SilaMe.Api.Services;

public sealed class IntegrationRouteService(SilaMeDbContext db)
{
    private static readonly IntegrationConfigurationStatus[] Suitable =
        [IntegrationConfigurationStatus.VALIDATED, IntegrationConfigurationStatus.ACTIVE, IntegrationConfigurationStatus.TESTED];

    public async Task<IReadOnlyList<IntegrationRouteResponse>> ListAsync(Guid organizationId, CancellationToken cancellationToken)
    {
        var rows = await db.IntegrationRoutes.AsNoTracking()
            .Include(item => item.CompanyCodes)
            .Include(item => item.Configuration)
            .Where(item => item.OrganizationId == organizationId)
            .OrderBy(item => item.ProcessType).ThenBy(item => item.UpdatedAt)
            .ToListAsync(cancellationToken);
        return rows.Select(ToResponse).ToList();
    }

    public async Task<IReadOnlyList<IntegrationRouteMatchResponse>> MatchingConfigurationsAsync(
        Guid organizationId, IntegrationProcessType processType, IntegrationSystemKind systemKind, CancellationToken cancellationToken)
    {
        return await db.ApiIntegrationConfigurations.AsNoTracking()
            .Where(item => item.OrganizationId == organizationId && item.ProcessType == processType && item.SystemKind == systemKind && Suitable.Contains(item.Status))
            .OrderBy(item => item.Name)
            .Select(item => new IntegrationRouteMatchResponse(item.Id, item.Name, item.ProcessType, item.SystemKind, item.Status.ToString()))
            .ToListAsync(cancellationToken);
    }

    public async Task<IntegrationRouteResponse> SaveAsync(Guid organizationId, Guid? id, IntegrationRouteInput input, Guid? userId, CancellationToken cancellationToken)
    {
        if (!Enum.IsDefined(input.ProcessType)) throw new IntegrationException("API_TYPE_INVALID", "The API type is not recognized.");
        if (!Enum.IsDefined(input.SystemKind)) throw new IntegrationException("SYSTEM_INVALID", "The system is not recognized.");

        var all = input.AppliesToAllCompanyCodes;
        var codes = NormalizeCodes(input.CompanyCodes);
        if (all && codes.Count > 0) throw new IntegrationException("COMPANY_CODE_SCOPE_INVALID", "ALL cannot be combined with individual company codes.");
        if (!all && codes.Count == 0) throw new IntegrationException("COMPANY_CODE_REQUIRED", "Select at least one company code, or ALL.");

        if (!all)
        {
            var known = await db.CompanyCodes.AsNoTracking()
                .Where(item => item.OrganizationId == organizationId && !item.IsDeleted)
                .Select(item => item.CompanyCode)
                .ToListAsync(cancellationToken);
            var knownSet = known.ToHashSet(StringComparer.OrdinalIgnoreCase);
            var missing = codes.Where(code => !knownSet.Contains(code)).ToList();
            if (missing.Count > 0)
                throw new IntegrationException("COMPANY_CODE_NOT_FOUND", $"Company code {missing[0]} does not exist in Company Code master.");
        }

        var configuration = await db.ApiIntegrationConfigurations.AsNoTracking()
            .SingleOrDefaultAsync(item => item.Id == input.ApiIntegrationConfigurationId && item.OrganizationId == organizationId, cancellationToken)
            ?? throw new IntegrationException("INTEGRATION_NOT_FOUND", "The API configuration was not found.", 404);
        if (configuration.ProcessType != input.ProcessType)
            throw new IntegrationException("API_CONFIGURATION_TYPE_MISMATCH", "The API configuration API type must match the route API type.");
        if (configuration.SystemKind != input.SystemKind)
            throw new IntegrationException("API_CONFIGURATION_SYSTEM_MISMATCH", "The API configuration system must match the route system.");
        if (!Suitable.Contains(configuration.Status))
            throw new IntegrationException("API_CONFIGURATION_NOT_READY", "Select a validated or active API configuration for this API type and system.");

        await EnsureNoConflictAsync(organizationId, id, input.ProcessType, all, codes, input.IsActive, cancellationToken);

        var route = id is null
            ? new IntegrationRoute { Id = Guid.NewGuid(), OrganizationId = organizationId, CreatedAt = DateTime.UtcNow, CreatedBy = userId }
            : await db.IntegrationRoutes.Include(item => item.CompanyCodes)
                .SingleOrDefaultAsync(item => item.Id == id && item.OrganizationId == organizationId, cancellationToken)
              ?? throw new IntegrationException("INTEGRATION_ROUTE_NOT_FOUND", "The integration route was not found.", 404);

        var wasActive = route.IsActive;
        var created = id is null;
        route.ProcessType = input.ProcessType;
        route.SystemKind = input.SystemKind;
        route.ApiIntegrationConfigurationId = input.ApiIntegrationConfigurationId;
        route.AppliesToAllCompanyCodes = all;
        route.IsActive = input.IsActive;
        route.UpdatedAt = DateTime.UtcNow;
        route.UpdatedBy = userId;
        db.IntegrationRouteCompanyCodes.RemoveRange(route.CompanyCodes);
        route.CompanyCodes.Clear();
        if (!all)
        {
            foreach (var code in codes)
                route.CompanyCodes.Add(new IntegrationRouteCompanyCode { Id = Guid.NewGuid(), IntegrationRouteId = route.Id, CompanyCode = code });
        }
        if (created) db.IntegrationRoutes.Add(route);

        if (created) Audit(organizationId, userId, "ROUTE_CREATED", route.Id, Snapshot(route, codes, all, configuration.Name));
        else Audit(organizationId, userId, "ROUTE_CHANGED", route.Id, Snapshot(route, codes, all, configuration.Name));
        if (!created && wasActive != route.IsActive)
            Audit(organizationId, userId, route.IsActive ? "ROUTE_ACTIVATED" : "ROUTE_DEACTIVATED", route.Id, Snapshot(route, codes, all, configuration.Name));
        if (created && route.IsActive)
            Audit(organizationId, userId, "ROUTE_ACTIVATED", route.Id, Snapshot(route, codes, all, configuration.Name));

        await db.SaveChangesAsync(cancellationToken);
        var saved = await db.IntegrationRoutes.AsNoTracking().Include(item => item.CompanyCodes).Include(item => item.Configuration)
            .SingleAsync(item => item.Id == route.Id, cancellationToken);
        return ToResponse(saved);
    }

    public async Task<IntegrationDeleteResponse> DeleteAsync(Guid organizationId, Guid id, Guid? userId, CancellationToken cancellationToken)
    {
        var route = await db.IntegrationRoutes
            .Include(item => item.CompanyCodes)
            .Include(item => item.Configuration)
            .SingleOrDefaultAsync(item => item.Id == id && item.OrganizationId == organizationId, cancellationToken)
            ?? throw new IntegrationException("INTEGRATION_ROUTE_NOT_FOUND", "The integration route was not found.", 404);

        var codes = route.AppliesToAllCompanyCodes
            ? (IReadOnlyList<string>)["ALL"]
            : route.CompanyCodes.Select(row => row.CompanyCode).ToList();
        Audit(organizationId, userId, "ROUTE_DELETED", route.Id, Snapshot(route, codes, route.AppliesToAllCompanyCodes, route.Configuration.Name));
        db.IntegrationRouteCompanyCodes.RemoveRange(route.CompanyCodes);
        db.IntegrationRoutes.Remove(route);
        await db.SaveChangesAsync(cancellationToken);
        return new IntegrationDeleteResponse(true, id);
    }

    private async Task EnsureNoConflictAsync(
        Guid organizationId, Guid? id, IntegrationProcessType processType, bool all, IReadOnlyList<string> codes, bool isActive, CancellationToken cancellationToken)
    {
        if (!isActive) return;
        var existing = await db.IntegrationRoutes.AsNoTracking().Include(item => item.CompanyCodes)
            .Where(item => item.OrganizationId == organizationId && item.ProcessType == processType && item.IsActive && item.Id != id)
            .ToListAsync(cancellationToken);
        if (all && existing.Any(item => item.AppliesToAllCompanyCodes))
            throw new IntegrationException("INTEGRATION_ROUTE_CONFLICT", $"An active ALL-company-code {processType} route already exists.");
        if (all) return;
        foreach (var code in codes)
        {
            if (existing.Any(item => !item.AppliesToAllCompanyCodes && item.CompanyCodes.Any(row => string.Equals(row.CompanyCode, code, StringComparison.OrdinalIgnoreCase))))
                throw new IntegrationException("INTEGRATION_ROUTE_CONFLICT", $"An active {processType} route already exists for Company Code {code}.");
        }
    }

    private void Audit(Guid organizationId, Guid? userId, string eventType, Guid routeId, string snapshot) =>
        db.AuditEvents.Add(new AuditEvent
        {
            Id = Guid.NewGuid(),
            OrganizationId = organizationId,
            UserId = userId,
            ChangedByUserId = userId,
            EventType = eventType,
            EntityType = "IntegrationRoute",
            EntityId = routeId,
            Reference = eventType,
            NewStateJson = snapshot,
            CreatedAt = DateTime.UtcNow,
        });

    private static string Snapshot(IntegrationRoute route, IReadOnlyList<string> codes, bool all, string configurationName) =>
        JsonSerializer.Serialize(new
        {
            route.ProcessType,
            CompanyCodes = all ? "ALL" : string.Join(",", codes),
            route.SystemKind,
            Configuration = configurationName,
            route.IsActive,
        });

    private static List<string> NormalizeCodes(IReadOnlyList<string>? values) =>
        (values ?? [])
            .Select(item => item?.Trim().ToUpperInvariant())
            .Where(item => !string.IsNullOrWhiteSpace(item) && !string.Equals(item, "ALL", StringComparison.OrdinalIgnoreCase))
            .Select(item => item!)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(item => item, StringComparer.OrdinalIgnoreCase)
            .ToList();

    private static IntegrationRouteResponse ToResponse(IntegrationRoute item) =>
        new(item.Id, item.ProcessType, item.SystemKind, item.ApiIntegrationConfigurationId, item.Configuration.Name,
            item.AppliesToAllCompanyCodes,
            item.AppliesToAllCompanyCodes ? ["ALL"] : item.CompanyCodes.Select(row => row.CompanyCode).OrderBy(code => code).ToList(),
            item.IsActive, item.UpdatedAt);
}
