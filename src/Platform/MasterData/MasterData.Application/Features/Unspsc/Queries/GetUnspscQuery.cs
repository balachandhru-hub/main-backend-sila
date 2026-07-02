using MediatR;
using MasterData.Domain.Dto;

namespace MasterData.Application.Features.Unspsc.Queries;

public record GetUnspscQuery(
    int PageIndex,
    int PageSize
) : IRequest<List<SegmentDto>>;