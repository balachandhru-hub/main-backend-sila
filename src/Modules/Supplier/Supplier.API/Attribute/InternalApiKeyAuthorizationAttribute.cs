using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Supplier.Domain.Common;

namespace Supplier.API.Attribute
{
    public class InternalApiKeyAuthorizationAttribute : System.Attribute, IAuthorizationFilter
    {
        public void OnAuthorization(AuthorizationFilterContext context)
        {
            IConfiguration configuration = context.HttpContext.RequestServices.GetRequiredService<IConfiguration>();
            string? expected = configuration[Common.INTERNAL_API_KEY];
            string? provided = context.HttpContext.Request.Headers["X-Internal-Api-Key"].FirstOrDefault();
            if (string.IsNullOrWhiteSpace(expected) || string.IsNullOrWhiteSpace(provided) || !string.Equals(expected, provided, StringComparison.Ordinal))
            {
                context.Result = new UnauthorizedObjectResult(new
                {
                    message = "Unauthorized",
                    description = "Internal API key is missing or invalid."
                });
            }
        }
    }
}
