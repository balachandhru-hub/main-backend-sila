using MediatR;

namespace Supplier.Application.Features.Commands.RFQAttachment
{
    public class InviteSupplierForContractCommand : IRequest<Guid>
    {
        public Guid RFQId { get; set; }
        public Guid SupplierId { get; set; }
    }
}
