using MediatR;

namespace Buyer.Application.Features.Commands.NotifyExternalSupplier
{
    public class NotifyExternalSupplierCommand : IRequest<bool>
    {
        public Guid BuyerRFQId { get; }

        public Guid ExternalSupplierId { get; }

        public NotifyExternalSupplierCommand(Guid buyerRFQId, Guid externalSupplierId)
        {
            BuyerRFQId = buyerRFQId;
            ExternalSupplierId = externalSupplierId;
        }
    }
}
