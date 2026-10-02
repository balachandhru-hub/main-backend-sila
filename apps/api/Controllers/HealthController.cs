using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SilaMe.Api.Data;
using SilaMe.Api.Services;
using SilaMe.Api.Tenancy;

namespace SilaMe.Api.Controllers;

[ApiController]
[Route("api")]
public sealed class HealthController(SilaMeDbContext db, PlatformDbContext platform, ITenantContextAccessor tenants, IHostEnvironment environment) : ControllerBase
{
    [HttpGet("healthz")]
    public async Task<IActionResult> Get(CancellationToken cancellationToken)
    {
        var connected = await db.Database.CanConnectAsync(cancellationToken);
        var platformConnected = await platform.Database.CanConnectAsync(cancellationToken);
        var context = tenants.Current;
        if (!connected || !platformConnected)
        {
            return StatusCode(StatusCodes.Status503ServiceUnavailable, new
            {
                api = "OK",
                status = "unhealthy",
                database = connected ? "connected" : "unavailable",
                platformDatabase = platformConnected ? "connected" : "unavailable",
                tenantResolved = context is not null,
            });
        }

        return Ok(new
        {
            api = "OK",
            status = "ok",
            database = "connected",
            platformDatabase = "connected",
            tenantResolved = context is not null,
            tenantCode = context?.TenantCode,
            environment = context?.EnvironmentType.ToString(),
        });
    }

    [HttpGet("dev/database-status")]
    public async Task<IActionResult> DatabaseStatus(CancellationToken cancellationToken)
    {
        if (!environment.IsDevelopment())
        {
            return NotFound();
        }

        var connected = await db.Database.CanConnectAsync(cancellationToken);
        var pending = connected ? await db.Database.GetPendingMigrationsAsync(cancellationToken) : [];
        return Ok(new
        {
            api = "OK",
            database = connected ? "CONNECTED" : "UNAVAILABLE",
            provider = "PostgreSQL",
            migrationStatus = !connected ? "UNKNOWN" : pending.Any() ? "PENDING" : "CURRENT",
            ocr = new
            {
                tesseract = BuiltInOcrProvider.HasCommand("tesseract") ? "AVAILABLE" : "MISSING",
                pdftoppm = BuiltInOcrProvider.HasCommand("pdftoppm") ? "AVAILABLE" : "MISSING",
            },
        });
    }
}
