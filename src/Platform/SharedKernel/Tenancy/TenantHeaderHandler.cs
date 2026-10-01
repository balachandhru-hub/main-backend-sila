using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;

namespace SharedKernel.Tenancy
{
    /// <summary>
    /// Forwards the current realm to the other services, so a call made on behalf of a request
    /// reads the same customer's databases.
    /// </summary>
    public class TenantHeaderHandler : DelegatingHandler
    {
        private readonly IHttpContextAccessor _httpContextAccessor;

        public TenantHeaderHandler(IHttpContextAccessor httpContextAccessor)
        {
            _httpContextAccessor = httpContextAccessor;
        }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            ITenantContext? tenantContext = _httpContextAccessor.HttpContext?.RequestServices.GetService<ITenantContext>();
            if (tenantContext != null
                && !tenantContext.Tenant.IsDefault
                && !request.Headers.Contains(TenancyConstants.TENANT_HOST_HEADER))
            {
                request.Headers.Add(TenancyConstants.TENANT_HOST_HEADER, tenantContext.Tenant.HostName);
            }

            return base.SendAsync(request, cancellationToken);
        }
    }
}
