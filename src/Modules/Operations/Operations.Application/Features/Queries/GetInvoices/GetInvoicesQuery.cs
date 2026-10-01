using MediatR;
using Operations.Domain.Dtos;

namespace Operations.Application.Features.Queries.GetInvoices
{
    /// <summary>
    /// Lists the latest invoices of the organization.
    /// </summary>
    public class GetInvoicesQuery : IRequest<List<InvoiceResponseDto>>
    {
        public Guid OrganizationId { get; set; }
        public Guid UserId { get; set; }
    }
}
