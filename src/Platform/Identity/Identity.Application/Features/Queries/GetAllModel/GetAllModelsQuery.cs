using Identity.Domain.Dto;
using MediatR;

namespace Identity.Application.Features.Queries.GetAllModel
{
    public class GetAllModelQuery : IRequest<List<ModelDto>>
    {
    }
}