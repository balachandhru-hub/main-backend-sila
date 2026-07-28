using MediatR;
using MasterData.Domain.Dto;

namespace MasterData.Application.Features.Lookup.Queries;

public class GetCurrenciesQuery : IRequest<PagedResultDto<CurrencyDto>>
{
    public int Index { get; set; } = 0;
    public int Limit { get; set; } = 10;
}
