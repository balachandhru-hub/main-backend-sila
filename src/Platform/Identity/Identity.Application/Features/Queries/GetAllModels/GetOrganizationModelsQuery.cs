using Identity.Domain.Dto;
using MediatR;

namespace Identity.Application.Features.Queries.GetAllModels
{
    public class GetOrganizationModelsQuery : IRequest<List<ModelDto>>
    {
        public Guid? OrganizationId { get; set; }
    }
}