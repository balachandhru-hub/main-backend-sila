using Buyer.Domain.Dto;
using MediatR;

namespace Buyer.Application.Features.Queries.SupplierVerification
{
    public class GetSupplierVerificationRequestListQuery
        : IRequest<List<SupplierVerificationRequestListDto>>
    {
        public Guid BuyerOrganizationId { get; set; }

        public int Index { get; set; }

        public int Limit { get; set; }
    }
}