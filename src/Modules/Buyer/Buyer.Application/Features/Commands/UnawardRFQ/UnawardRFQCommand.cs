using Buyer.Domain.Dto;
using MediatR;

namespace Buyer.Application.Features.Commands.UnawardRFQ
{
    /// <summary>
    /// Cancels the active award on an RFQ so the buyer can pick a different
    /// winning supplier, without reopening bidding. The RFQ goes back to
    /// <see cref="Buyer.Domain.Common.Common.RFQ_FREEZING_STATUS"/> - the
    /// same "bids frozen" state used before the first award - so the buyer
    /// can re-award from the same frozen bid comparison.
    /// </summary>
    public class UnawardRFQCommand : IRequest<bool>
    {
        public UnawardRFQDto Request { get; }

        public UnawardRFQCommand(UnawardRFQDto request)
        {
            Request = request;
        }
    }
}
