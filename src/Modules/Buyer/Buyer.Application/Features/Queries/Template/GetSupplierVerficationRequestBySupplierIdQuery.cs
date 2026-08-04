using Buyer.Domain.Dto;
using MediatR;

namespace Buyer.Application.Features.Queries.SupplierVerification
{
    public class GetSupplierVerificationRequestBySupplierIdQuery
        : IRequest<List<SupplierVerificationRequestListBySupplierIdDto>>
    {
        public Guid SupplierOrganizationId { get; set; }

        public int Index { get; set; }

        public int Limit { get; set; }
    }
}