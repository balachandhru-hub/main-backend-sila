using Identity.Domain.Dto;
using MediatR;

namespace Identity.Application.Features.Auth.Queries.GetClaim
{
    public class GetClaimQuery : IRequest<TokenClaimDto>
    {
        public string Token { get; set; } 
    }
}