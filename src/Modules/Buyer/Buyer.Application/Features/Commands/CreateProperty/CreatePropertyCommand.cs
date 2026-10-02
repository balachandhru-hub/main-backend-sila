using Buyer.Domain.Dtos;
using MediatR;

namespace Buyer.Application.Features.Commands.CreateProperty
{
    public class CreatePropertyCommand : IRequest<Guid>
    {
        public Guid OrganizationId { get; set; }
        public Guid UserId { get; set; }
        public PropertyWriteDto Request { get; set; } = new();
    }
}
