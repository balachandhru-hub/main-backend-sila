using MasterData.Application.Contracts;

using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.DependencyInjection;

using SharedKernel.ExceptionHandler;

namespace MasterData.API.Attributes
{
    /// <summary>
    /// Validates an external-session token sent in the <c>X-Session-Token</c>
    /// header against the <c>rfqId</c> route/query value, for endpoints that
    /// an external supplier hits without a Buyer/Supplier login. The token
    /// itself is issued and stored by the Supplier microservice, so this
    /// simply asks Supplier's API whether it is valid and throws Forbidden
    /// if not.
    /// </summary>
    public class ExternalSessionAuthorizationAttribute : Attribute, IAsyncAuthorizationFilter
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
                throw new ForBiddenCustomException(
                    "Forbidden",
                    "Session token is required.");
            }

            var rfqIdValue = context.RouteData.Values[RouteRFQIdKey]?.ToString();

            if (string.IsNullOrWhiteSpace(rfqIdValue))
            {
                rfqIdValue = context.HttpContext.Request.Query["rfqId"]
                    .FirstOrDefault();
            }

            if (!Guid.TryParse(rfqIdValue, out Guid rfqId))
            {
                throw new ForBiddenCustomException(
                    "Forbidden",
                    "RFQId is required.");
            }

            var supplierSessionApiClient = context.HttpContext.RequestServices
                .GetRequiredService<ISupplierSessionApiClient>();

            bool isValid = await supplierSessionApiClient.ValidateExternalSessionToken(
                sessionToken,
                rfqId,
                context.HttpContext.RequestAborted);

            if (!isValid)
            {
                throw new ForBiddenCustomException(
                    "Forbidden",
                    "Invalid or expired session token.");
            }
        }
    }
}
