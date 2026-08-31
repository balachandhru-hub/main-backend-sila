using MediatR;
using Supplier.Domain.Dto;

namespace Supplier.Application.Features.Queries.GetSupplierQuotationHistoryComparison
{
    public class GetSupplierQuotationHistoryComparisonQuery
        : IRequest<SupplierQuotationHistoryComparisonDto>
    {
        public Guid SupplierQuotationId { get; }

        public GetSupplierQuotationHistoryComparisonQuery(
            Guid supplierQuotationId)
        {
            SupplierQuotationId = supplierQuotationId;
        }
    }
}