using MediatR;
using MasterData.Domain.Dto;

namespace MasterData.Application.Features.Lookup.Queries;

public class GetCountriesQuery : IRequest<PagedResultDto<CountryDto>>
{
    public int Index { get; set; }
    public int Limit { get; set; }
    public string? SearchTerm { get; set; }
}
