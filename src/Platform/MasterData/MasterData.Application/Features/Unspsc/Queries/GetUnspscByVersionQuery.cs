using MediatR;
using MasterData.Domain.Dto;

namespace MasterData.Application.Features.Unspsc.Queries;

public record GetUnspscByVersionQuery(
    string Version,
    int PageIndex,
    int PageSize
) : IRequest<List<UnspscDto>>;