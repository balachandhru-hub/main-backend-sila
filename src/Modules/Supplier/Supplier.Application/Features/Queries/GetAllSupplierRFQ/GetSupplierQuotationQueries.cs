using MediatR;
using Supplier.Domain.Dto;

namespace Supplier.Application.Features.Queries.GetSupplierQuotation
{
    public class GetSupplierQuotationByBuyerRFQIdQuery : IRequest<GetAllSupplierQuotationDto>
    {
        public Guid RFQId { get; set; }
    }
}