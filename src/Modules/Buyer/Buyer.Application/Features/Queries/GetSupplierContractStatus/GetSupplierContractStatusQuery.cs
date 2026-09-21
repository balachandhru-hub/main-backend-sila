using Buyer.Domain.Dto;
using MediatR;

namespace Buyer.Application.Features.Queries.GetSupplierContractStatus
{
    public class GetSupplierContractStatusQuery
        : IRequest<SupplierContractStatusDto>
    {
        public Guid RFQId { get; set; }

        public Guid SupplierId { get; set; }
    }
}
