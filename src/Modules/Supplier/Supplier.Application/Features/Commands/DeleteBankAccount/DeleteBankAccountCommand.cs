using MediatR;

namespace Supplier.Application.Features.Commands.DeleteBankAccount
{
    public class DeleteBankAccountCommand : IRequest<Guid>
    {
        public Guid Id { get; set; }
        public Guid SupplierId { get; set; }
    }
}
