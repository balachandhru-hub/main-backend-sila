using MediatR;
using Operations.Domain.Dtos;

namespace Operations.Application.Features.Commands.MatchInvoicePurchaseOrder
{
    /// <summary>
    /// Matches an invoice to a purchase order: the given order, or the automatic match by number when no order is given.
    /// </summary>
    public class MatchInvoicePurchaseOrderCommand : IRequest<InvoiceResponseDto>
    {
        public Guid OrganizationId { get; set; }
        public Guid UserId { get; set; }
        public Guid InvoiceId { get; set; }
        public MatchPurchaseOrderRequestDto Request { get; set; } = new();
    }
}
