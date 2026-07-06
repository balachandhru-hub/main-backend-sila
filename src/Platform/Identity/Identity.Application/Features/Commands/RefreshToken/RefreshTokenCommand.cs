using Identity.Domain.Dto;
using MediatR;

namespace Identity.Application.Features.Commands.RefreshToken.RefreshToken;

public class RefreshTokenCommand : IRequest<LoginResponse>
{
    public Guid RefreshToken { get; set; }
}