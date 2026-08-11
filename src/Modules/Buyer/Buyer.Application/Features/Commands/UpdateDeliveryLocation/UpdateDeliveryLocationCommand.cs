
using Buyer.Domain.Dto;
using MediatR;

namespace Buyer.Application.Features.Commands.UpdateDeliveryLocation
{
    public class UpdateDeliveryLocationCommand : IRequest<Guid>
    {
        public Guid Id { get; set; }

        public UpdateDeliveryLocationDto Data { get; set; } = new();
    }
}

