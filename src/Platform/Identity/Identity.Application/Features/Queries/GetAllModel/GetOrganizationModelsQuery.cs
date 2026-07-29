using Identity.Domain.Dto;
using MediatR;

namespace Identity.Application.Features.Queries.GetAllModel
{
    public class GetOrganizationModelQuery : IRequest<List<ModelDto>>
    {
        public Guid? OrganizationId { get; set; }
    }
}