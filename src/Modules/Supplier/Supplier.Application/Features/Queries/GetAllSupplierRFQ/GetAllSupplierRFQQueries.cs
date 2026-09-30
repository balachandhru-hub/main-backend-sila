using Supplier.Domain.Dto;
using MediatR;

namespace Supplier.Application.Features.Queries.GetSupplierAllRFQ
{
    public class GetSupplierRFQByIdQuery : IRequest<GetRFQByIdDto>
    {
        public Guid RFQId { get; set; }
        public Guid OrganizationId { get; set; }
        public Guid UserId { get; set; }
        public Guid RoleId { get; set; }
    }
}