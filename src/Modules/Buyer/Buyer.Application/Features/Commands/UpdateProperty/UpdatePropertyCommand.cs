using Buyer.Domain.Dtos;
using MediatR;

namespace Buyer.Application.Features.Commands.UpdateProperty
{
    public class UpdatePropertyCommand : IRequest<Unit>
    {
        public Guid OrganizationId { get; set; }
        public Guid UserId { get; set; }
        public Guid PropertyId { get; set; }
        public PropertyWriteDto Request { get; set; } = new();
    }
}
