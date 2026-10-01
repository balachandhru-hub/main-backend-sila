using MediatR;
using Operations.Domain.Dtos;

namespace Operations.Application.Features.Commands.ProcessInvoice
{
    /// <summary>
    /// Reads the stored document of an invoice again (basic extraction) and matches supplier, purchase order and lines.
    /// </summary>
    public class ProcessInvoiceCommand : IRequest<InvoiceResponseDto>
    {
        public Guid OrganizationId { get; set; }
        public Guid UserId { get; set; }
        public Guid InvoiceId { get; set; }
    }
}
