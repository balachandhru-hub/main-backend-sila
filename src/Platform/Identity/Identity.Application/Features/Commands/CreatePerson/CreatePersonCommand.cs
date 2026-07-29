using Identity.Domain.Dto;
using MediatR;

namespace Identity.Application.Features.Commands.CreatePerson
{
    public class CreatePersonCommand : IRequest<Guid>
    {
        public Guid OrganizationId { get; set; }

        public CreatePersonDto Model { get; set; } 
    }
}