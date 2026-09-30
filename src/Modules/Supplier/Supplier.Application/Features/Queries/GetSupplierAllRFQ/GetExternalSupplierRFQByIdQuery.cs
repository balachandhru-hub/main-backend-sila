using Supplier.Domain.Dto;
using MediatR;

namespace Supplier.Application.Features.Queries.GetSupplierAllRFQ
{
    public class GetExternalSupplierRFQByIdQuery : IRequest<GetRFQByIdDto>
    {
        public Guid RFQId { get; set; }
        public Guid SupplierId { get; set; }
    }
}
