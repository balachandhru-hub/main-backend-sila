using MediatR;
using Supplier.Domain.Dto;

namespace Supplier.Application.Features.Commands.ResetSupplierRFQAward
{
    public class ResetSupplierRFQAwardCommand : IRequest<bool>
    {
        public ResetSupplierRFQAwardDto Request { get; }

        public ResetSupplierRFQAwardCommand(
            ResetSupplierRFQAwardDto request)
        {
            Request = request;
        }
    }
}
