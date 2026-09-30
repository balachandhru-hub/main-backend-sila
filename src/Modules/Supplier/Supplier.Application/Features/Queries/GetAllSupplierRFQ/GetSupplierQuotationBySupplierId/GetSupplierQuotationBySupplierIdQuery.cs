using MediatR;
using Supplier.Domain.Dto;

namespace Supplier.Application.Features.Queries.GetSupplierQuotationBySupplierId
{
    public class GetSupplierQuotationBySupplierIdQuery
        : IRequest<GetAllSupplierQuotationBySupplierIdDto>
    {
        public Guid RFQId { get; set; }
        public Guid OrganizationId { get; set; }

        public GetSupplierQuotationBySupplierIdQuery(
            Guid rfqId,
            Guid organizationId)
        {
            RFQId = rfqId;
            OrganizationId = organizationId;
        }
    }
}