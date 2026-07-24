using Supplier.Domain.Dto;
using MediatR;

namespace Supplier.Application.Features.Queries.GetSupplierAllRFQ
{
    public class GetSupplierRFQByIdQuery : IRequest<GetRFQByIdDto>
    {
        public Guid RFQId { get; set; }
    }
}