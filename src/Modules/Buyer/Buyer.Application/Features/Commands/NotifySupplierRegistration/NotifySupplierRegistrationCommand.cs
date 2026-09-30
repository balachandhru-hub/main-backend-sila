using MediatR;

namespace Buyer.Application.Features.Commands.NotifySupplierRegistration
{
    /// <summary>
    /// Invites an external supplier to register their business profile on
    /// the portal, once their bidding on an RFQ is complete. Separate from
    /// <see cref="Buyer.Application.Features.Commands.NotifyExternalSupplier.NotifyExternalSupplierCommand"/>,
    /// which invites them to bid at RFQ creation time.
    /// </summary>
    public class NotifySupplierRegistrationCommand : IRequest<bool>
    {
        public Guid ExternalSupplierId { get; }

        public Guid BuyerRFQId { get; }

        public NotifySupplierRegistrationCommand(Guid externalSupplierId, Guid buyerRFQId)
        {
            ExternalSupplierId = externalSupplierId;
            BuyerRFQId = buyerRFQId;
        }
    }
}
