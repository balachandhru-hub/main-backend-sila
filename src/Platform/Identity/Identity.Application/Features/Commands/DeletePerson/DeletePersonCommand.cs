using MediatR;

namespace Identity.Application.Features.Commands.DeletePerson
{
    public class DeletePersonCommand : IRequest<Guid>
    {
        public Guid PersonId { get; set; }
    }
}