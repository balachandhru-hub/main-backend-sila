using MediatR;
using Operations.Domain.Dtos;

namespace Operations.Application.Features.Commands.ReprocessInvoice
{
    /// <summary>
    /// Re-reads the document of an invoice with the advanced extraction (manual re-read).
    /// </summary>
    public class ReprocessInvoiceCommand : IRequest<AdvancedInvoiceExtractionResponseDto>
    {
        public Guid OrganizationId { get; set; }
        public Guid UserId { get; set; }
        public Guid InvoiceId { get; set; }
    }
}
