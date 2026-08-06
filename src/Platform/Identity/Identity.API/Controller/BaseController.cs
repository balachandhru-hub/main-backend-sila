using Microsoft.AspNetCore.Mvc;
using SharedKernel.ExceptionHandler;
using System.Security.Claims;

namespace Identity.API.Controllers
{
  
    public abstract class BaseController : ControllerBase
    {
        protected Guid GetOrganizationId()
        {
            var claim = User.FindFirst("OrganizationId")?.Value;

            if (!Guid.TryParse(claim, out Guid organizationId))
            {
                throw new UnAuthorizedCustomException(
                    "Unauthorized",
                    "Organization claim not found.");
            }

            return organizationId;
        }

        protected Guid GetUserId()
        {
            var claim = User.FindFirst("UserId")?.Value;

            if (!Guid.TryParse(claim, out Guid userId))
            {
                throw new UnAuthorizedCustomException(
                    "Unauthorized",
                    "User claim not found.");
            }

            return userId;
        }
        protected Guid GetPersonId()
        {
            var claim = User.FindFirst("PersonId")?.Value;

            if (!Guid.TryParse(claim, out Guid personId))
            {
                throw new UnAuthorizedCustomException(
                    "Unauthorized",
                    "Person claim not found.");
            }

            return personId;
        }
    }
}