using MediatR;

namespace Buyer.Application.Features.Commands.DeleteDeliveryLocation
{
    public class DeleteDeliveryLocationCommand : IRequest<Guid>
    {
        public Guid Id { get; set; }
        public Guid BuyerId { get; set; }
    }
}
