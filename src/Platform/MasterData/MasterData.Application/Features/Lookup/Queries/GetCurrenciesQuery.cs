using MediatR;
using MasterData.Domain.Dto;

namespace MasterData.Application.Features.Lookup.Queries;

public class GetCurrenciesQuery : IRequest<List<CurrencyDto>>
{
}
