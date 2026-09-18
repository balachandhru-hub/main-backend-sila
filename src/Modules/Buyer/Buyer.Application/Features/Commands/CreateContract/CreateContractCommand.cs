using Buyer.Domain.Dto;
using MediatR;

namespace Buyer.Application.Features.Commands.CreateContract
{
    public class CreateContractCommand : IRequest<Guid>
    {
        public CreateContractDto Request { get; }
        public Guid BuyerId { get; }
        public Guid SupplierId { get; }

        public CreateContractCommand(CreateContractDto request, Guid buyerId, Guid supplierId)
        {
            Request = request;
            BuyerId = buyerId;
            SupplierId = supplierId;
        }
    }
}
