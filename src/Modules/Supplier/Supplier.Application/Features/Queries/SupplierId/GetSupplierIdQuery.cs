using MediatR;

namespace Supplier.Application.Features.Profile.Queries.GetSupplierId
{
    public class GetSupplierIdQuery : IRequest<Guid>
    {
        public Guid OrganizationId { get; }

        public GetSupplierIdQuery(Guid organizationId)
        {
            OrganizationId = organizationId;
        }
    }
}