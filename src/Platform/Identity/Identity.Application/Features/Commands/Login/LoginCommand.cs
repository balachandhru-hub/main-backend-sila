using MediatR;

namespace Identity.Application.Features.Commands.Login
{
    public class LoginCommand : IRequest<LoginResponse>
    {
        public string UserName { get; set; } = string.Empty;

        public string Password { get; set; } = string.Empty;
    }
}