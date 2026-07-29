using Identity.Domain.Dto;
using MediatR;

namespace Identity.Application.Features.Queries.GetAllModels
{
    public class GetAllModelsQuery : IRequest<List<ModelDto>>
    {
    }
}