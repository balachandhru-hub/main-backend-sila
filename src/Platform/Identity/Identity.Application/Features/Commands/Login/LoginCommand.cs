using MediatR;
using Identity.Domain.Dto;

namespace Identity.Application.Features.Commands.Login
{
    public class LoginCommand : IRequest<LoginResponse>
    {
        public string UserName { get; set; } 

        public string Password { get; set; }
    }
}