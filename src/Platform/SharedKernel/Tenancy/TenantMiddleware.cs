using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using SharedKernel.ExceptionHandler;
using SharedKernel.LoggerServices;

namespace SharedKernel.Tenancy
{
    /// <summary>
    /// Binds each request to its customer realm before any database access, and prepares the realm's
    /// database the first time it is used.
    /// </summary>
    public class TenantMiddleware
    {
        private readonly RequestDelegate _next;

        public TenantMiddleware(RequestDelegate next)
        {
            _next = next;
        }

        public async Task InvokeAsync(
            HttpContext context,
            ITenantRegistry registry,
            ITenantContext tenantContext,
            TenantBootstrapState bootstrapState,
            ILoggerManager logger)
        {
            // Browsers send no custom headers on a CORS preflight, and it touches no data.
            if (HttpMethods.IsOptions(context.Request.Method))
            {
                await _next(context);
                return;
            }

            TenantInfo tenant = Resolve(context, registry);
            if (!tenant.IsActive)
            {
                logger.LogError($"Request refused. Realm is disabled. Realm: {tenant.Code}");
                throw new ForBiddenCustomException("Environment disabled", "This customer environment has been disabled.");
            }

            tenantContext.Use(tenant);

            if (!tenant.IsDefault)
            {
                ITenantBootstrapper? bootstrapper = context.RequestServices.GetService<ITenantBootstrapper>();
                if (bootstrapper != null)
                {
                    bootstrapState.EnsureInitialized(tenant.Code, () =>
                    {
                        logger.LogInfo($"Preparing database for realm. Realm: {tenant.Code}");
                        bootstrapper.Initialize(context.RequestServices);
                        logger.LogInfo($"Database prepared for realm. Realm: {tenant.Code}");
                    });
                }
            }

            await _next(context);
        }

        /// <summary>
        /// The realm is identified by the host the browser opened: the forwarded tenant header first, then
        /// the Origin header (browsers always send it on cross-origin and WebSocket requests), then Host.
        /// A host that is not registered belongs to the default realm.
        /// </summary>
        public static TenantInfo Resolve(HttpContext context, ITenantRegistry registry)
        {
            string? host = context.Request.Headers[TenancyConstants.TENANT_HOST_HEADER].FirstOrDefault();
            if (string.IsNullOrWhiteSpace(host))
            {
                string? origin = context.Request.Headers["Origin"].FirstOrDefault();
                if (!string.IsNullOrWhiteSpace(origin) && Uri.TryCreate(origin, UriKind.Absolute, out Uri? originUri))
                {
                    host = originUri.Authority;
                }
            }

            if (string.IsNullOrWhiteSpace(host))
            {
                host = context.Request.Host.Value;
            }

            return registry.FindByHost(host) ?? registry.Default;
        }
    }
}
