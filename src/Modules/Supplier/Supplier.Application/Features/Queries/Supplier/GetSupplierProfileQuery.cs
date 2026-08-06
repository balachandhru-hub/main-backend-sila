using MediatR;
using Supplier.Domain.Dto;

namespace Supplier.Application.Features.Queries.Supplier
{
    public class GetSupplierProfileQuery : IRequest<OrganizationDto>
    {
       public Guid OrganizationId { get; set; }

        public GetSupplierProfileQuery(Guid organizationId)
        {
            OrganizationId = organizationId;
        }
    }
}