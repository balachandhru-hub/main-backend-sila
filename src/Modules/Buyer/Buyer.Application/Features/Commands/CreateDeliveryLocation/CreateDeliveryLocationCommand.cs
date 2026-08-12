using Buyer.Domain.Dto;
using MediatR;

namespace Buyer.Application.Features.Commands.CreateDeliveryLocation
{
    public class CreateDeliveryLocationCommand : IRequest<Guid>
    {
        public Guid BuyerId { get; set; }
        public BuyerDeliveryLocationDto Data { get; set; } = new();
    }
}
