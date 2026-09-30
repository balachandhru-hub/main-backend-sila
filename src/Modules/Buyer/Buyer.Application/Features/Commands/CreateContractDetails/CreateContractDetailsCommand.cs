using Buyer.Domain.Dto;
using MediatR;

namespace Buyer.Application.Features.Commands.CreateContractDetails
{
    public class CreateContractDetailsCommand : IRequest<Guid>
    {
        public CreateContractDetailsDto Request { get; }
        public Guid BuyerId { get; }

        public CreateContractDetailsCommand(CreateContractDetailsDto request, Guid buyerId)
        {
            Request = request;
            BuyerId = buyerId;
        }
    }
}
