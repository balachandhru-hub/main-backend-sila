using Supplier.Domain.Dto;
using MediatR;

namespace Supplier.Application.Features.Queries.GetAllSupplierRFQ
{
    public class GetSupplierRFQListQuery : IRequest<List<SupplierRFQListDto>>
    {
        public Guid SupplierId { get; set; }

        public int Index { get; set; }

        public int Limit { get; set; }
    }
}