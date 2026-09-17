using Buyer.Domain.Dto;
using MediatR;

namespace Buyer.Application.Features.Queries.GetExternalSupplierName
{
    /// <summary>
    /// Resolves an external supplier's own name from its id, for the
    /// session-token authenticated external supplier flow.
    /// </summary>
    public class GetExternalSupplierNameQuery : IRequest<ExternalSupplierNameDto>
    {
        public Guid ExternalSupplierId { get; }

        public GetExternalSupplierNameQuery(Guid externalSupplierId)
        {
            ExternalSupplierId = externalSupplierId;
        }
    }
}
