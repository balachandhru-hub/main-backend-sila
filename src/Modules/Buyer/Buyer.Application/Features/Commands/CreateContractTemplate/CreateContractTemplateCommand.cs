using Buyer.Domain.Dto;
using MediatR;

namespace Buyer.Application.Features.Commands.CreateContractTemplate
{
    public class CreateContractTemplateCommand : IRequest<Guid>
    {
        public CreateContractTemplateDto Request { get; }
        public Guid BuyerId { get; }

        public CreateContractTemplateCommand(CreateContractTemplateDto request, Guid buyerId)
        {
            Request = request;
            BuyerId = buyerId;
        }
    }
}
