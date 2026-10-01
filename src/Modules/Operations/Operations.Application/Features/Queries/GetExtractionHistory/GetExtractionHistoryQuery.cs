using MediatR;
using Operations.Domain.Dtos;

namespace Operations.Application.Features.Queries.GetExtractionHistory
{
    /// <summary>
    /// Lists every extraction run of the document of an invoice, newest first.
    /// </summary>
    public class GetExtractionHistoryQuery : IRequest<List<ExtractionHistoryItemDto>>
    {
        public Guid OrganizationId { get; set; }
        public Guid UserId { get; set; }
        public Guid InvoiceId { get; set; }
    }
}
