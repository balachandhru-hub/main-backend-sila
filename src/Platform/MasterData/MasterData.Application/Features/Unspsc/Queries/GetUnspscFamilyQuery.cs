using MediatR;
using MasterData.Domain.Dto;

namespace MasterData.Application.Features.Unspsc.Queries;

public record GetUnspscFamilyQuery(
    long Segment,
    int PageIndex,
    int PageSize
) : IRequest<List<FamilyDto>>;