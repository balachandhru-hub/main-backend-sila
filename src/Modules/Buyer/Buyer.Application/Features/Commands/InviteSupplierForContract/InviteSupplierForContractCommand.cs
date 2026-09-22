using MediatR;

namespace Buyer.Application.Features.Commands.InviteSupplierForContract
{
    public class InviteSupplierForContractCommand : IRequest<Guid>
    {
        public Guid RFQId { get; set; }
        public Guid SupplierId { get; set; }
    }
}
