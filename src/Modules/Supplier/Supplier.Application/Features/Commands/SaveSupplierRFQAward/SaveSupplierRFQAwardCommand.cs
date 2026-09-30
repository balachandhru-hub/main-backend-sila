using MediatR;
using Supplier.Domain.Dto;

namespace Supplier.Application.Features.Commands.SaveSupplierRFQAward
{
    public class SaveSupplierRFQAwardCommand : IRequest<bool>
    {
        public SaveSupplierRFQAwardDto Request { get; }

        public SaveSupplierRFQAwardCommand(
            SaveSupplierRFQAwardDto request)
        {
            Request = request;
        }
    }
}
