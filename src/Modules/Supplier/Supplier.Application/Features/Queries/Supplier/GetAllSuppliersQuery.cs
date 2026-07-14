using MediatR;
using Supplier.Domain.Dto;

namespace Supplier.Application.Features.Queries.Supplier
{
    public class GetAllSuppliersQuery : IRequest<List<SupplierProfileDto>>
    {
        public int Index { get; set; }

        public int Limit { get; set; }
         public string? OrganizationName { get; set; }
 
        public string? Status { get; set; }
    }
}