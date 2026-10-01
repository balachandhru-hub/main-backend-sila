using Buyer.Domain.Dtos;
using MediatR;

namespace Buyer.Application.Features.Commands.SetUserOutlets
{
    public class SetUserOutletsCommand : IRequest<Unit>
    {
        public Guid OrganizationId { get; set; }
        public Guid UserId { get; set; }
        public OutletUsersWriteDto Request { get; set; } = new();
    }
}
