using MediatR;
using Supplier.Domain.Dto;

namespace Supplier.Application.Features.Commands.UpdateSupplierBankAccount
{
    public class UpdateSupplierBankAccountCommand : IRequest<Guid>
    {
        public Guid Id { get; set; }

        public Guid SupplierId { get; set; }

        public SupplierBankAccountUpdateDto Data { get; set; }
    }
}