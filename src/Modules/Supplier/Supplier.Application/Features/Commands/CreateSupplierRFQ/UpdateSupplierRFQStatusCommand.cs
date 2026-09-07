using MediatR;
using Supplier.Domain.Dto;

namespace Supplier.Application.Features.Commands.RFQ
{
    public class UpdateSupplierRFQStatusCommand : IRequest<bool>
    {
        public UpdateSupplierRFQStatusDto Request { get; }

        public UpdateSupplierRFQStatusCommand(
            UpdateSupplierRFQStatusDto request)
        {
            Request = request;
        }
    }
}