using Buyer.Application.Contracts;
using Buyer.Domain.Dto;
using MediatR;

namespace Buyer.Application.Features.Queries.GetRFQESign
{
    public class GetRFQESignQueryHandler
        : IRequestHandler<GetRFQESignQuery, List<RFQESignDto>>
    {
        private readonly ISupplierApiClient _supplierApiClient;

        public GetRFQESignQueryHandler(ISupplierApiClient supplierApiClient)
        {
            _supplierApiClient = supplierApiClient;
        }

        public async Task<List<RFQESignDto>> Handle(
            GetRFQESignQuery request,
            CancellationToken cancellationToken)
        {
            return await _supplierApiClient.GetRFQESign(
                request.RFQId,
                cancellationToken);
        }
    }
}
