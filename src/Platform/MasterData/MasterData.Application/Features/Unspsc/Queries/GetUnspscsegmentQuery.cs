using MediatR;
using MasterData.Domain.Dto;

namespace MasterData.Application.Features.Unspsc.Queries;

public record GetUnspscsegmentQuery(
    int PageIndex,
    int PageSize
) : IRequest<List<GetSegmentDto>>;