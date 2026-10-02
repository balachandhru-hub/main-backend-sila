using SilaMe.Api.Models;
using SilaMe.Api.Tenancy;

namespace SilaMe.Api.Auth;

public static class SessionExtensions
{
    public static Guid CustomerUserId(this Session session)
    {
        if (session.UserId is Guid userId)
        {
            return userId;
        }

        throw new TenantException(
            "SUPPORT_SESSION_NO_CUSTOMER_USER",
            "This action is bound to a customer user identity and cannot use a delegated SILA Platform session.");
    }
}
