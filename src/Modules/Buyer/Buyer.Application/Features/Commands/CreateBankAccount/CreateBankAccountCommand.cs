using Buyer.Domain.Dto;
using MediatR;

namespace Buyer.Application.Features.Commands.CreateBankAccount
{
    public class CreateBankAccountCommand : IRequest<Guid>
    {
        public Guid BuyerId { get; set; }
        public BuyerBankAccountDto Data { get; set; } = new();
    }
}
