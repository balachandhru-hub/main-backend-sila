using Buyer.Domain.Dto;
using MediatR;

namespace Buyer.Application.Features.Commands.UpdateContractTemplate
{
    public class UpdateContractTemplateCommand : IRequest<Guid>
    {
        public Guid Id { get; }
        public UpdateContractTemplateDto Request { get; }
        public Guid BuyerId { get; }

        public UpdateContractTemplateCommand(Guid id, UpdateContractTemplateDto request, Guid buyerId)
        {
            Id = id;
            Request = request;
            BuyerId = buyerId;
        }
    }
}
