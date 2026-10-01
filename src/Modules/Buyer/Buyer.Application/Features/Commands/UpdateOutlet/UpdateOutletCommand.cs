using Buyer.Domain.Dtos;
using MediatR;

namespace Buyer.Application.Features.Commands.UpdateOutlet
{
    public class UpdateOutletCommand : IRequest<Unit>
    {
        public Guid OrganizationId { get; set; }
        public Guid OutletId { get; set; }
        public OutletWriteDto Request { get; set; } = new();
    }
}
