using Buyer.Domain.Dto;
using MediatR;

namespace Buyer.Application.Features.Queries.GetSupplierPredefinedContractStatus
{
    public class GetSupplierPredefinedContractStatusQuery
        : IRequest<SupplierPredefinedContractStatusDto>
    {
        public Guid RFQId { get; set; }

        public Guid SupplierId { get; set; }
    }
}
