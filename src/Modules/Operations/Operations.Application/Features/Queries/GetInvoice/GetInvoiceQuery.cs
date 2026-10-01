using MediatR;
using Operations.Domain.Dtos;

namespace Operations.Application.Features.Queries.GetInvoice
{
    /// <summary>
    /// Returns one invoice of the organization with its lines.
    /// </summary>
    public class GetInvoiceQuery : IRequest<InvoiceResponseDto>
    {
        public Guid OrganizationId { get; set; }
        public Guid UserId { get; set; }
        public Guid InvoiceId { get; set; }
    }
}
