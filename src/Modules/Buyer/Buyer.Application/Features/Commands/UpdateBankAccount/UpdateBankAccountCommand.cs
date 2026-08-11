
using Buyer.Domain.Dto;
using MediatR;

namespace Buyer.Application.Features.Commands.UpdateBankAccount
{
    public class UpdateBankAccountCommand : IRequest<Guid>
    {
        public Guid Id { get; set; }

        public UpdateBankAccountDto Data { get; set; } = new();
    }
}

