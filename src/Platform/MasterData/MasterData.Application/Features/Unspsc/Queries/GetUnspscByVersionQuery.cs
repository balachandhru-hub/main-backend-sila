using MediatR;
using MasterData.Domain.Dto;

namespace MasterData.Application.Features.Unspsc.Queries;

public record GetUnspscByVersionQuery(
    long Segment,
    long Family,
    int PageIndex,
    int PageSize
) : IRequest<List<ClassDto>>;