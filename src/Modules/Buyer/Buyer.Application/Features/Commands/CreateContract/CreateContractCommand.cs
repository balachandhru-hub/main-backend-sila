using Buyer.Domain.Dto;
using MediatR;

namespace Buyer.Application.Features.Commands.CreateContract
{
    public class CreateContractCommand : IRequest<Guid>
    {
        public CreateContractDto Request { get; }
        public Guid BuyerId { get; }

        public CreateContractCommand(CreateContractDto request, Guid buyerId)
        {
            Request = request;
            BuyerId = buyerId;
        }
    }
}
