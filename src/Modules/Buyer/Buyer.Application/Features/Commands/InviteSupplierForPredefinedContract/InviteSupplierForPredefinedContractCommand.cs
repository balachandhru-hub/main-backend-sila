using MediatR;

namespace Buyer.Application.Features.Commands.InviteSupplierForPredefinedContract
{
    public class InviteSupplierForPredefinedContractCommand : IRequest<Guid>
    {
        public Guid RFQId { get; set; }
        public Guid SupplierId { get; set; }
    }
}
