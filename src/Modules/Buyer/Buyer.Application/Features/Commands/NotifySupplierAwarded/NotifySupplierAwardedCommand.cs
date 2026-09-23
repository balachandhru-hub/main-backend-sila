using MediatR;

namespace Buyer.Application.Features.Commands.NotifySupplierAwarded
{
    /// <summary>
    /// Congratulates the supplier who won an RFQ award. When the winner is
    /// an external supplier with no portal account yet, the email also
    /// carries a registration link so they can register before the buyer
    /// proceeds to contract creation.
    /// </summary>
    public class NotifySupplierAwardedCommand : IRequest<bool>
    {
        public Guid RFQId { get; }

        public Guid SupplierId { get; }

        public NotifySupplierAwardedCommand(Guid rfqId, Guid supplierId)
        {
            RFQId = rfqId;
            SupplierId = supplierId;
        }
    }
}
