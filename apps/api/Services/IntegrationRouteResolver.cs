using Microsoft.EntityFrameworkCore;
using SilaMe.Api.Data;
using SilaMe.Api.DTOs;
using SilaMe.Api.Models;

namespace SilaMe.Api.Services;

// Downstream of POST GRN. Routes by Company Code. Do not modify Invoice Review/OCR.
// See docs/PROTECTED_INVOICE_FLOW.md.
public sealed class IntegrationRouteResolver(SilaMeDbContext db)
{
    public async Task<bool> HasActiveRoutesAsync(Guid organizationId, IntegrationProcessType processType, CancellationToken cancellationToken) =>
        await db.IntegrationRoutes.AsNoTracking().AnyAsync(item =>
            item.OrganizationId == organizationId && item.ProcessType == processType && item.IsActive, cancellationToken);

    public async Task<ResolvedIntegrationRoute> ResolveAsync(
        Guid organizationId,
        IntegrationProcessType processType,
        string? companyCode,
        CancellationToken cancellationToken)
    {
        var code = Normalize(companyCode);
        if (string.IsNullOrWhiteSpace(code))
            throw NotFound(processType, companyCode);

        var routes = await db.IntegrationRoutes.AsNoTracking()
            .Include(item => item.CompanyCodes)
            .Include(item => item.Configuration)
            .Where(item => item.OrganizationId == organizationId && item.ProcessType == processType && item.IsActive)
            .ToListAsync(cancellationToken);

        var exact = routes.Where(item => !item.AppliesToAllCompanyCodes &&
                item.CompanyCodes.Any(row => string.Equals(row.CompanyCode, code, StringComparison.OrdinalIgnoreCase)))
            .ToList();
        if (exact.Count > 1)
            throw new IntegrationException("INTEGRATION_ROUTE_CONFLICT", $"Multiple active {processType} integration routes match Company Code {code}.");
        if (exact.Count == 1) return ToResolved(exact[0], code);

        var fallback = routes.Where(item => item.AppliesToAllCompanyCodes).ToList();
        if (fallback.Count > 1)
            throw new IntegrationException("INTEGRATION_ROUTE_CONFLICT", $"Multiple active ALL-company-code {processType} integration routes are configured.");
        if (fallback.Count == 1) return ToResolved(fallback[0], code);

        throw NotFound(processType, code);
    }

    private static ResolvedIntegrationRoute ToResolved(IntegrationRoute route, string companyCode) =>
        new(route.Id, route.ProcessType, route.SystemKind, route.ApiIntegrationConfigurationId, route.Configuration.Name,
            route.AppliesToAllCompanyCodes, companyCode, route.Configuration);

    private static IntegrationException NotFound(IntegrationProcessType processType, string? companyCode)
    {
        var display = string.IsNullOrWhiteSpace(companyCode) ? "(missing)" : companyCode.Trim().ToUpperInvariant();
        return new IntegrationException("INTEGRATION_ROUTE_NOT_FOUND",
            $"No active {processType} integration route is configured for Company Code {display}.", 404);
    }

    private static string Normalize(string? value) => string.IsNullOrWhiteSpace(value) ? string.Empty : value.Trim().ToUpperInvariant();
}
