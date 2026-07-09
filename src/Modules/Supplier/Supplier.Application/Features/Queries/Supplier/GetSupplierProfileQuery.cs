using MediatR;
using Supplier.Domain.Dto;

namespace Supplier.Application.Features.Queries.Supplier
{
    public class GetSupplierProfileQuery : IRequest<SupplierProfileDto>
    {
       public Guid OrganizationId { get; set; }

        public GetSupplierProfileQuery(Guid organizationId)
        {
            OrganizationId = organizationId;
        }
    }
}