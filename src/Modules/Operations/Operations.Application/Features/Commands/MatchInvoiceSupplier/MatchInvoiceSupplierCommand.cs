using MediatR;
using Operations.Domain.Dtos;

namespace Operations.Application.Features.Commands.MatchInvoiceSupplier
{
    /// <summary>
    /// Sets the supplier of an invoice: the given supplier, or the automatic match when no supplier is given.
    /// </summary>
    public class MatchInvoiceSupplierCommand : IRequest<InvoiceResponseDto>
    {
        public Guid OrganizationId { get; set; }
        public Guid UserId { get; set; }
        public Guid InvoiceId { get; set; }
        public MatchSupplierRequestDto Request { get; set; } = new();
    }
}
