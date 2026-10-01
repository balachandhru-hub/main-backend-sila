using MediatR;
using Operations.Domain.Dtos;

namespace Operations.Application.Features.Queries.GetBasicExtraction
{
    /// <summary>
    /// Returns the seven header fields read from an invoice.
    /// </summary>
    public class GetBasicExtractionQuery : IRequest<BasicInvoiceExtractionResponseDto>
    {
        public Guid OrganizationId { get; set; }
        public Guid UserId { get; set; }
        public Guid InvoiceId { get; set; }
    }
}
