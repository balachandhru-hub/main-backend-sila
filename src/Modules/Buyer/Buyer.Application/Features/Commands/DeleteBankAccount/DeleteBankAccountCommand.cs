using MediatR;

namespace Buyer.Application.Features.Commands.DeleteBankAccount
{
    public class DeleteBankAccountCommand : IRequest<Guid>
    {
        public Guid Id { get; set; }
        public Guid BuyerId { get; set; }
    }
}
