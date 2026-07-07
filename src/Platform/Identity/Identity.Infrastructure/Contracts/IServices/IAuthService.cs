using Identity.Domain.Dto;

namespace Contracts.IServices
{
    public interface IAuthService
    {
        TokenClaimDto GetClaim(string token);
    }
}