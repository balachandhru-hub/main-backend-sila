using MediatR;
using Operations.Domain.Dtos;

namespace Operations.Application.Features.Commands.HandleMicrosoftCallback
{
    /// <summary>
    /// Completes the Microsoft sign-in (the browser arrives here from Microsoft, without a session) and returns the frontend URL to redirect to.
    /// </summary>
    public class HandleMicrosoftCallbackCommand : IRequest<string>
    {
        public MicrosoftCallbackRequestDto Request { get; set; } = new();
    }
}
