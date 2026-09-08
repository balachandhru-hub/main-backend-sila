using MediatR;

namespace Buyer.Application.Features.Commands.NotifyExternalSupplier
{
    public class NotifyExternalSupplierCommand : IRequest<bool>
    {
        public Guid BuyerRFQId { get; }

        public Guid ExternalSupplierId { get; }

        public string SessionToken { get; }

        public NotifyExternalSupplierCommand(Guid buyerRFQId, Guid externalSupplierId, string sessionToken)
        {
            BuyerRFQId = buyerRFQId;
            ExternalSupplierId = externalSupplierId;
            SessionToken = sessionToken;
        }
    }
}
