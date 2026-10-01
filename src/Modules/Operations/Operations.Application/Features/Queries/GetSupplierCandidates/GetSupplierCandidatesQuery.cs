using MediatR;
using Operations.Domain.Dtos;

namespace Operations.Application.Features.Queries.GetSupplierCandidates
{
    /// <summary>
    /// Suggests suppliers for an invoice by tax number, name or alias.
    /// </summary>
    public class GetSupplierCandidatesQuery : IRequest<SupplierMatchResponseDto>
    {
        public Guid OrganizationId { get; set; }
        public Guid UserId { get; set; }
        public Guid InvoiceId { get; set; }
    }
}
