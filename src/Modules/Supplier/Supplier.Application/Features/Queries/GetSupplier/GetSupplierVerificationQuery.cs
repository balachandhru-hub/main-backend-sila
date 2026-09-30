using Supplier.Domain.Dto;
using MediatR;

namespace Supplier.Application.Features.Queries.GetSupplier
{
    public class GetSupplierListQuery : IRequest<List<SupplierListDto>>
    {
        public GetSupplierListDto SupplierListDto { get; set; }

        public GetSupplierListQuery(GetSupplierListDto supplierListDto)
        {
            SupplierListDto = supplierListDto;
        }
    }
}