using MediatR;
using Operations.Domain.Dtos;

namespace Operations.Application.Features.Commands.MatchInvoiceLines
{
    /// <summary>
    /// Matches invoice lines to the items of the invoice's purchase order.
    /// </summary>
    public class MatchInvoiceLinesCommand : IRequest<InvoiceResponseDto>
    {
        public Guid OrganizationId { get; set; }
        public Guid UserId { get; set; }
        public Guid InvoiceId { get; set; }
        public MatchInvoiceLinesRequestDto Request { get; set; } = new();
    }
}
