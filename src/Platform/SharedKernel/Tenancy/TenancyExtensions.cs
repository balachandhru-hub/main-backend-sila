using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace SharedKernel.Tenancy
{
    public static class TenancyExtensions
    {
        /// <summary>
        /// Registers realm resolution. The DbContext then takes its connection string from
        /// <see cref="ITenantContext"/>. A service that owns the registry (Identity) registers its own
        /// <see cref="ITenantRegistry"/> before calling this.
        /// </summary>
        public static IServiceCollection AddTenancy(this IServiceCollection services)
        {
            services.AddHttpContextAccessor();
            services.AddHttpClient();
            services.TryAddSingleton<ITenantRegistry, HttpTenantRegistry>();
            services.TryAddSingleton<TenantBootstrapState>();
            services.TryAddScoped<ITenantContext, TenantContext>();
            services.TryAddTransient<TenantHeaderHandler>();
            services.AddHostedService<TenantRegistryRefresher>();

            // Every outgoing HttpClient call carries the realm of the request that caused it.
            services.ConfigureHttpClientDefaults(builder => builder.AddHttpMessageHandler<TenantHeaderHandler>());
            return services;
        }

        /// <summary>
        /// Resolves the realm of each request. Place it after the exception middleware and before
        /// anything that reads the database.
        /// </summary>
        public static IApplicationBuilder UseTenancy(this IApplicationBuilder app)
        {
            TenantCors.Registry = app.ApplicationServices.GetRequiredService<ITenantRegistry>();
            return app.UseMiddleware<TenantMiddleware>();
        }
    }

    /// <summary>
    /// CORS check shared by the services: the configured frontend origin, plus the URL of every active realm.
    /// </summary>
    public static class TenantCors
    {
        /// <summary>Set once at startup by <see cref="TenancyExtensions.UseTenancy"/>.</summary>
        public static ITenantRegistry? Registry { get; set; }

        public static bool IsAllowedOrigin(string origin, string? configuredOrigin)
        {
            if (!string.IsNullOrWhiteSpace(configuredOrigin)
                && origin.Equals(configuredOrigin, StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }

            if (Registry == null || !Uri.TryCreate(origin, UriKind.Absolute, out Uri? originUri))
            {
                return false;
            }

            TenantInfo? tenant = Registry.FindByHost(originUri.Authority);
            return tenant != null && tenant.IsActive;
        }
    }
}
