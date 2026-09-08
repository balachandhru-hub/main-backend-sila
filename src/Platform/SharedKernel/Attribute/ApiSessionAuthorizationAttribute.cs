using System.Security.Claims;

using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.DependencyInjection;

using SharedKernel.Contracts;
using SharedKernel.ExceptionHandler;

namespace SharedKernel.Attributes
{
    /// <summary>
    /// Validates an external-session token sent in the
    /// <c>X-Session-Token</c> header against the <c>rfqId</c> route value,
    /// instead of the normal JWT/cookie based <see cref="ApiAuthorizationAttribute"/>.
    /// The actual token lookup is delegated to an <see cref="ISessionTokenValidator"/>
    /// registered by the hosting module, so this attribute can be reused by
    /// any module/project without SharedKernel depending on module-specific
    /// repositories.
    /// </summary>
    public class ApiSessionAuthorizationAttribute : Attribute, IAsyncAuthorizationFilter
    {
        public const string SessionTokenHeaderName = "X-Session-Token";
        public const string RouteRFQIdKey = "rfqId";

        public async Task OnAuthorizationAsync(AuthorizationFilterContext context)
        {
            string? sessionToken = context.HttpContext.Request
                .Headers[SessionTokenHeaderName]
                .FirstOrDefault();

            if (string.IsNullOrWhiteSpace(sessionToken))
            {
                throw new UnAuthorizedCustomException(
                    "Unauthorized",
                    "Session token is required.");
            }

            if (!context.RouteData.Values.TryGetValue(RouteRFQIdKey, out var rfqIdValue) ||
                !Guid.TryParse(rfqIdValue?.ToString(), out Guid rfqId))
            {
                throw new UnAuthorizedCustomException(
                    "Unauthorized",
                    "RFQId is required.");
            }

            var validator = context.HttpContext.RequestServices
                .GetRequiredService<ISessionTokenValidator>();

            SessionTokenValidationResult result = await validator.ValidateAsync(
                sessionToken,
                rfqId,
                context.HttpContext.RequestAborted);

            var claims = new List<Claim>
            {
                new Claim("RFQId", result.RFQId.ToString()),
                new Claim("SupplierId", result.SupplierId.ToString()),
                new Claim("SupplierRFQId", result.SupplierRFQId.ToString())
            };

            var identity = new ClaimsIdentity(claims, "ExternalSupplierSession");
            context.HttpContext.User = new ClaimsPrincipal(identity);
        }
    }
}
