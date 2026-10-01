using Buyer.Domain.Dtos;
using MediatR;

namespace Buyer.Application.Features.Commands.CreateOutlet
{
    public class CreateOutletCommand : IRequest<Guid>
    {
        public Guid OrganizationId { get; set; }
        public OutletWriteDto Request { get; set; } = new();
    }
}
