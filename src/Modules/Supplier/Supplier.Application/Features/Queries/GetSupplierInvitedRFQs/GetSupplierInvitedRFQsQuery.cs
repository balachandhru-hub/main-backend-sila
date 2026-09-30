using Supplier.Domain.Dto;
using MediatR;

namespace Supplier.Application.Features.Queries.GetSupplierInvitedRFQs
{
    public class GetSupplierInvitedRFQsQuery : IRequest<List<SupplierInvitedRFQContractDto>>
    {
        public Guid OrganizationId { get; set; }
        public Guid UserId { get; set; }
        public Guid RoleId { get; set; }
        public int Index { get; set; }
        public int Limit { get; set; }
        public string? Search { get; set; }
    }
}
