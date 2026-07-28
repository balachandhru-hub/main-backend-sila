using MediatR;
using Supplier.Domain.Dto;

namespace Supplier.Application.Features.Queries.SupplierAnswers
{
    public class GetSupplierRFQAnswerQuery
        : IRequest<SupplierRFQAnswerResponseDto>
    {
        public Guid BuyerRFQId { get; set; }

        public GetSupplierRFQAnswerQuery(Guid buyerRFQId)
        {
            BuyerRFQId = buyerRFQId;
        }
    }
}