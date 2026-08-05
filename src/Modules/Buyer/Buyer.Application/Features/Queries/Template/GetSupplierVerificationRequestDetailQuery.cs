using Buyer.Domain.Dto;
using MediatR;

namespace Buyer.Application.Features.Queries.SupplierVerificationRequest
{
    public class GetSupplierVerificationRequestDetailQuery : IRequest<SupplierVerificationRequestDetailDto>
    {
        public Guid RequestId { get; set; }
    }
}