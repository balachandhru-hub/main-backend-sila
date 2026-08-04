using Buyer.Domain.Dto;
using MediatR;

namespace Buyer.Application.Features.Queries.SupplierVerificationRequest
{
    public class GetSupplierVerificationRequestDetailbyRequestIdQuery
        : IRequest<SupplierVerificationRequestDetailQuestinandAnswerDto>
    {
        public Guid RequestId { get; set; }
    }
}