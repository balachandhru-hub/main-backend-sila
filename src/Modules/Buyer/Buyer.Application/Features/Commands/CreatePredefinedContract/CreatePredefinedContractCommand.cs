using Buyer.Domain.Dto;
using MediatR;

namespace Buyer.Application.Features.Commands.CreatePredefinedContract
{
    public class CreatePredefinedContractCommand : IRequest<Guid>
    {
        public CreatePredefinedContractDto Request { get; }
        public Guid BuyerId { get; }

        public CreatePredefinedContractCommand(CreatePredefinedContractDto request, Guid buyerId)
        {
            Request = request;
            BuyerId = buyerId;
        }
    }
}
