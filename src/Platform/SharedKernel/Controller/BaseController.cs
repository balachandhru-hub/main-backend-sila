using Microsoft.AspNetCore.Mvc;
using SharedKernel.ExceptionHandler;
using System.Security.Claims;

namespace SharedKernel.Controllers
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
        protected string GetSNID()
        {
            var claim = User.FindFirst("SNID")?.Value;

            if (string.IsNullOrWhiteSpace(claim))
            {
                throw new UnAuthorizedCustomException(
                    "Unauthorized",
                    "SNID claim not found.");
            }

            return claim;
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
        protected Guid GetRoleId()
        {
            var roleClaimValue = User.FindFirst(ClaimTypes.Role)?.Value;

            if (!Guid.TryParse(roleClaimValue, out Guid roleId))
            {
                throw new UnAuthorizedCustomException(
                    "Unauthorized",
                    "Role claim not found.");
            }

            return roleId;
        }
        protected string GetOrganizationType()
        {
            var claim = User.FindFirst("OrganizationType")?.Value;

            if (string.IsNullOrWhiteSpace(claim))
            {
                throw new UnAuthorizedCustomException(
                    "Unauthorized",
                    "Organization type claim not found.");
            }

            return claim;
        }
        protected Guid GetBuyerId()
        {
            

            var claim = User.FindFirst("BuyerId")?.Value;

            if (!Guid.TryParse(claim, out Guid buyerId))
            {
                 throw new NotFoundCustomException(
            "Not Found",
            $"{claim} not found.");
            }

            return buyerId;
        }
        protected Guid GetSupplierId()
        {
           
            var claim = User.FindFirst("SupplierId")?.Value;

            if (!Guid.TryParse(claim, out Guid supplierId))
            {
                   throw new NotFoundCustomException(
            "Not Found",
            $"{claim} not found.");
            }

            return supplierId;
        }
    }
}