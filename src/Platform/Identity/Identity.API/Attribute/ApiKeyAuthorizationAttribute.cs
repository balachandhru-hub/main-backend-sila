using Identity.Domain.Entities;
using Identity.Infrastructure.Contracts.IServices;
using SharedKernel.ExceptionHandler;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.Filters;
using System.Security.Claims;


namespace Identity.API.Attributes
{
    /// <summary>
    /// Class <c>ApiKeyAuthorizationAttribute</c> used to validate the authorization of API 
    /// by reading the Authorize attribute and checking with the api key.
    /// </summary>
    public class ApiKeyAuthorizationAttribute : Attribute, IAuthorizationFilter
    {


        /// <summary>
        /// Check whether the user is Authorized to access the API or not
        /// </summary>
        /// <param name="context"></param>
        public void OnAuthorization(AuthorizationFilterContext context)
        {


            if (context.HttpContext.Request.Headers["X-API-KEY"].FirstOrDefault() != null)
            {
                string? apiKey = context.HttpContext.Request.Headers["X-API-KEY"].FirstOrDefault();
                if (context.HttpContext.RequestServices.GetService(typeof(IApiKeyService)) is IApiKeyService _apiKeyService && _apiKeyService.IsApiKeyValid(apiKey!))
                {
                    return; //User Authorized. With the Api Key
                }
            }
            else
            {
                throw new UnAuthorizedCustomException("Not authorized", "Invalid Data");
            }

        }
    }


}