using MediatR;
using Supplier.Domain.Dto;

namespace Supplier.Application.Features.Commands.CreateSupplierBankAccount
{
    public class CreateSupplierBankAccountCommand : IRequest<Guid>
    {
        public Guid SupplierId { get; set; }
        public SupplierBankAccountDto Data { get; set; } = new();
    }
}
