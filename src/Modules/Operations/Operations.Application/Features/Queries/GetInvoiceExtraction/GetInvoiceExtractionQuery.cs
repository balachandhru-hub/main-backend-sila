using MediatR;
using Operations.Domain.Dtos;

namespace Operations.Application.Features.Queries.GetInvoiceExtraction
{
    /// <summary>
    /// Returns the current extraction result of an invoice: header, lines and how it was read.
    /// </summary>
    public class GetInvoiceExtractionQuery : IRequest<InvoiceExtractionResponseDto>
    {
        public Guid OrganizationId { get; set; }
        public Guid UserId { get; set; }
        public Guid InvoiceId { get; set; }
    }
}
