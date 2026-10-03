using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using SilaMe.Api.Auth;
using SilaMe.Api.Data;
using SilaMe.Api.Models;
using SilaMe.Api.Services;
using SilaMe.Api.Tenancy;
using System.Text.Json.Serialization;

var builder = WebApplication.CreateBuilder(args);

var allowedOrigins = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
{
    "http://localhost:5173",
    "https://localhost:3001",
    "http://localhost:3001",
    "http://localhost:5174",
    "http://localhost:8081",
    "http://localhost:19006",
    "http://127.0.0.1:5173",
    "https://127.0.0.1:3001",
    "http://127.0.0.1:3001",
    "http://127.0.0.1:5174",
    "http://127.0.0.1:8081",
    "http://127.0.0.1:19006",
};

foreach (var environmentVariable in new[] { "REPLIT_DEV_DOMAIN", "REPLIT_EXPO_DEV_DOMAIN" })
{
    var domain = Environment.GetEnvironmentVariable(environmentVariable);
    if (!string.IsNullOrWhiteSpace(domain))
    {
        allowedOrigins.Add($"https://{domain}");
        allowedOrigins.Add($"http://{domain}");
    }
}

var extraOrigins = Environment.GetEnvironmentVariable("SILA_ME_CORS_ORIGINS");
if (!string.IsNullOrWhiteSpace(extraOrigins))
{
    foreach (var origin in extraOrigins.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
    {
        allowedOrigins.Add(origin);
    }
}

static bool IsBrowserDevOrigin(string origin)
{
    if (!Uri.TryCreate(origin, UriKind.Absolute, out var uri))
    {
        return false;
    }

    return uri.Scheme is "http" or "https"
        && (uri.Host is "localhost" or "127.0.0.1" or "::1"
            || uri.Host.EndsWith(".localhost", StringComparison.OrdinalIgnoreCase)
            || uri.Host.Contains("cursor.", StringComparison.OrdinalIgnoreCase)
            || uri.Host.Contains(".cursor", StringComparison.OrdinalIgnoreCase));
}

var listenPort = Environment.GetEnvironmentVariable("PORT");
if (!string.IsNullOrWhiteSpace(listenPort))
{
    builder.WebHost.UseUrls($"http://*:{listenPort}");
}

builder.Services
    .AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
    });
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddProblemDetails();
builder.Services.AddCors(options =>
{
    options.AddPolicy("SilaMeDevelopment", policy =>
    {
        policy.SetIsOriginAllowed(origin =>
                !string.IsNullOrWhiteSpace(origin)
                && (allowedOrigins.Contains(origin) || IsBrowserDevOrigin(origin)))
            .AllowAnyHeader()
            .AllowAnyMethod()
            .AllowCredentials();
    });
});

var connectionString = DatabaseUrl.Resolve(
    Environment.GetEnvironmentVariable("DATABASE_URL"),
    builder.Configuration["DATABASE_URL"],
    builder.Configuration.GetConnectionString("DefaultConnection"));
var platformConnectionString = DatabaseUrl.Resolve(
    Environment.GetEnvironmentVariable("PLATFORM_DATABASE_URL"),
    builder.Configuration["PLATFORM_DATABASE_URL"],
    ReplaceDatabaseName(connectionString, "sila_platform"));

builder.Services.AddSingleton(new OperationalDatabaseOptions(connectionString));
builder.Services.AddSingleton<ITenantContextAccessor, TenantContextAccessor>();
builder.Services.AddSingleton<ISecretProvider, EnvironmentSecretProvider>();
builder.Services.AddSingleton<ITenantDatabaseResolver, TenantDatabaseResolver>();
builder.Services.AddSingleton<TenantJobRunner>();
builder.Services.AddDbContext<PlatformDbContext>(options =>
    options.UseNpgsql(DatabaseUrl.Normalize(platformConnectionString)));
builder.Services.AddDbContext<SilaMeDbContext>((provider, options) =>
{
    var accessor = provider.GetRequiredService<ITenantContextAccessor>();
    var resolver = provider.GetRequiredService<ITenantDatabaseResolver>();
    var bootstrap = provider.GetRequiredService<OperationalDatabaseOptions>();
    var active = accessor.Current is { } context ? resolver.GetConnectionString(context) : bootstrap.ConnectionString;
    options.UseNpgsql(DatabaseUrl.Normalize(active));
});
builder.Services.AddScoped<IPasswordService, PasswordService>();
builder.Services.AddScoped<ISessionService, SessionService>();
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<IUserService, UserService>();
builder.Services.AddScoped<IAccessService, AccessService>();
builder.Services.AddScoped<ITenantProvisioningService, TenantProvisioningService>();
builder.Services.AddScoped<IPlatformAuthService, PlatformAuthService>();
builder.Services.AddScoped<ITenantLaunchService, TenantLaunchService>();
builder.Services.AddScoped<ILicenseService, LicenseService>();
builder.Services.AddScoped<IEntitlementService, EntitlementService>();
builder.Services.AddScoped<OrganizationBrandingService>();
builder.Services.AddHttpClient("microsoft-graph", client => client.Timeout = TimeSpan.FromSeconds(30));
builder.Services.AddScoped<MicrosoftSharePointService>();
builder.Services.AddScoped<IDocumentStorageService, LocalDocumentStorageService>();
builder.Services.AddScoped<DocumentRoutingService>();
builder.Services.AddScoped<IPdfTextExtractor, EmbeddedPdfTextExtractor>();
builder.Services.AddSingleton<BuiltInOcrProvider>();
builder.Services.AddScoped<IOcrProvider>(services => services.GetRequiredService<BuiltInOcrProvider>());
builder.Services.AddHttpClient("external-extraction", client => client.Timeout = TimeSpan.FromSeconds(30));
builder.Services.AddHttpClient("api-integrations", client => client.Timeout = TimeSpan.FromSeconds(30))
    .ConfigurePrimaryHttpMessageHandler(() => new SocketsHttpHandler
    {
        UseCookies = false,
        AllowAutoRedirect = false,
        PooledConnectionLifetime = TimeSpan.FromMinutes(2),
    });
ApiDataProtection.Configure(builder.Services, builder.Environment.ContentRootPath);
builder.Services.AddScoped<ProtectedIntegrationCredentialStore>();
builder.Services.AddScoped<IntegrationService>();
builder.Services.AddScoped<IntegrationDesignerService>();
builder.Services.AddSingleton<CsrfSessionProvider>();
builder.Services.AddSingleton<IIntegrationAuthenticationHandler, NoAuthenticationHandler>();
builder.Services.AddSingleton<IIntegrationAuthenticationHandler, BasicAuthenticationHandler>();
builder.Services.AddSingleton<IIntegrationAuthenticationHandler, ApiKeyAuthenticationHandler>();
builder.Services.AddSingleton<IIntegrationAuthenticationHandler, StaticBearerAuthenticationHandler>();
builder.Services.AddSingleton<IIntegrationAuthenticationHandler, CustomHeaderAuthenticationHandler>();
builder.Services.AddSingleton<IIntegrationAuthenticationHandler, OAuth2ClientCredentialsHandler>();
builder.Services.AddSingleton<IIntegrationAuthenticationHandler, TokenApiAuthenticationHandler>();
builder.Services.AddSingleton<IntegrationAuthenticationResolver>();
builder.Services.AddScoped<IntegrationConnectionTestService>();
builder.Services.AddScoped<IntegrationRouteService>();
builder.Services.AddScoped<IntegrationRouteResolver>();
builder.Services.AddScoped<AribaPostGrnAdapter>();
builder.Services.AddScoped<S4HanaPostGrnAdapter>();
builder.Services.AddHostedService<IntegrationSchedulerWorker>();
builder.Services.AddScoped<IExtractionProviderResolver, ExtractionProviderResolver>();
builder.Services.AddScoped<IInvoiceExtractionService, InvoiceExtractionService>();
builder.Services.AddScoped<BasicInvoiceExtractionService>();
builder.Services.AddScoped<AdvancedInvoiceOcrService>();
builder.Services.AddScoped<IGrnPostingProvider, LocalGrnPostingProvider>();
builder.Services.AddSingleton<InvoiceProcessingQueue>();
builder.Services.AddHostedService<InvoiceProcessingWorker>();
builder.Services.AddHostedService<DocumentTransferWorker>();
builder.Services.AddScoped<OperationalService>();
builder.Services.AddScoped<MasterRecordService>();
builder.Services.AddScoped<MaterialMasterService>();
builder.Services.AddScoped<MaterialApprovalService>();
builder.Services.AddScoped<S4ProductMaterialAdapter>();
builder.Services.AddScoped<RecipeCatalogService>();
builder.Services.AddScoped<RecipeReferenceDataService>();
builder.Services.AddScoped<RecipeApprovalService>();
builder.Services.AddScoped<ApprovalWorkflowService>();
builder.Services.AddScoped<PosSourceService>();
builder.Services.AddScoped<RecipeExplosionService>();
builder.Services.AddScoped<RecipeInventoryPostingService>();
builder.Services.AddScoped<RecipePosIntakeService>();
builder.Services.AddScoped<InventoryLocationService>();
builder.Services.AddScoped<InventoryWorkspaceService>();
builder.Services.AddScoped<InternalTransferService>();
builder.Services.AddScoped<MaterialLocationService>();
builder.Services.AddScoped<StockCountService>();

var app = builder.Build();
app.UseForwardedHeaders(new ForwardedHeadersOptions
{
    ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto,
    KnownNetworks = { },
    KnownProxies = { },
});
app.UseExceptionHandler();
app.UseMiddleware<TenantExceptionMiddleware>();
app.UseCors("SilaMeDevelopment");
app.UseMiddleware<CloudOriginGuardMiddleware>();
app.UseMiddleware<TenantResolutionMiddleware>();
app.UseMiddleware<PlatformAuthenticationMiddleware>();
app.UseMiddleware<SessionAuthenticationMiddleware>();
app.MapControllers();

using (var scope = app.Services.CreateScope())
{
    await PlatformBootstrap.RunAsync(app.Services, connectionString);
    var db = scope.ServiceProvider.GetRequiredService<SilaMeDbContext>();
    await db.Database.MigrateAsync();
    await DevelopmentSeed.SeedAsync(scope.ServiceProvider);
    await AccessSeed.SeedAsync(scope.ServiceProvider);
}

app.Run();

static string ReplaceDatabaseName(string connectionString, string databaseName)
{
    var normalized = DatabaseUrl.Normalize(connectionString);
    var builder = new NpgsqlConnectionStringBuilder(normalized) { Database = databaseName };
    return builder.ConnectionString;
}

static class DevelopmentSeed
{
    public static async Task SeedAsync(IServiceProvider services)
    {
        var environment = services.GetRequiredService<IHostEnvironment>();
        if (!environment.IsDevelopment())
        {
            return;
        }

        var cloudPassword = Environment.GetEnvironmentVariable("SILA_ME_CLOUD_ADMIN_PASSWORD");
        var mobilePassword = Environment.GetEnvironmentVariable("SILA_ME_MOBILE_ADMIN_PASSWORD");
        var db = services.GetRequiredService<SilaMeDbContext>();
        var passwordService = services.GetRequiredService<IPasswordService>();
        if (!string.IsNullOrWhiteSpace(cloudPassword))
        {
            await SeedUserAsync(db, passwordService, "cloudadmin@silame.local", "SILA ME Cloud Admin", ApplicationKind.CLOUD, cloudPassword);
        }

        if (!string.IsNullOrWhiteSpace(mobilePassword))
        {
            await SeedUserAsync(db, passwordService, "mobileadmin@silame.local", "SILA ME Mobile Admin", ApplicationKind.MOBILE, mobilePassword);
        }

        var sharedAdminPassword = Environment.GetEnvironmentVariable("SILA_ME_ADMIN_PASSWORD");
        if (!string.IsNullOrWhiteSpace(sharedAdminPassword))
        {
            var protectedIdentity = await db.Users.AsNoTracking()
                .SingleOrDefaultAsync(user => user.NormalizedEmail == "BALA@CHERVIC.IN");
            if (protectedIdentity is not null)
            {
                await SeedUserAsync(db, passwordService, "bala@chervic.in", "SILA Administrator", ApplicationKind.CLOUD, sharedAdminPassword);
                await SeedUserAsync(db, passwordService, "bala@chervic.in", "SILA Administrator", ApplicationKind.MOBILE, sharedAdminPassword);
            }
        }
    }

    private static async Task SeedUserAsync(
        SilaMeDbContext db,
        IPasswordService passwordService,
        string email,
        string displayName,
        ApplicationKind application,
        string password)
    {
        var normalizedEmail = email.ToUpperInvariant();
        var user = await db.Users.Include(candidate => candidate.ApplicationAccess)
            .SingleOrDefaultAsync(candidate => candidate.NormalizedEmail == normalizedEmail);
        if (user is null)
        {
            user = new User
            {
                Id = Guid.NewGuid(),
                Email = email,
                NormalizedEmail = normalizedEmail,
                DisplayName = displayName,
                PasswordHash = string.Empty,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
            };
            user.PasswordHash = passwordService.HashPassword(user, password);
            db.Users.Add(user);
        }
        else
        {
            // Existing accounts keep their password, status, and identity.
            // Only a missing application-access row may be added (CLOUD vs MOBILE).
        }

        if (user.ApplicationAccess.All(access => access.Application != application))
        {
            db.UserApplicationAccess.Add(new UserApplicationAccess
            {
                Id = Guid.NewGuid(),
                UserId = user.Id,
                Application = application,
                Status = StatusKind.ACTIVE,
                CreatedAt = DateTime.UtcNow,
            });
        }

        await db.SaveChangesAsync();
    }
}

public partial class Program;