
using MediatR;
using SharedKernel.Dto;
namespace Supplier.Application.Features.StatusUpdate.Commands
{
    public class UpdateSupplierStatusCommand : IRequest<bool>
    {
        public Guid SupplierId { get; set; }

        public string Status { get; set; }

        public string? Comments { get; set; }
    }
}