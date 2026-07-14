using MediatR;
 
namespace Identity.Application.Features.Commands.Logout
{
    public class LogoutCommand : IRequest<bool>
    {
    }
}