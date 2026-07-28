using MasterData.Domain.Dto;
using MediatR;

namespace MasterData.Application.Features.Lookup.Queries;

public class GetUnitsQuery : IRequest<PagedResultDto<UnitDto>>
{
    public int Index { get; set; } = 0;
    public int Limit { get; set; } = 10;
    public string? SearchTerm { get; set; }
}
