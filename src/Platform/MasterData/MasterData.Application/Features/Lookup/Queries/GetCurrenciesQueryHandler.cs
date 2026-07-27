using MediatR;
using MasterData.Domain.Dto;
using MasterData.Infrastructure.Contracts.IRepository;
using SharedKernel.LoggerServices;

namespace MasterData.Application.Features.Lookup.Queries;

public class GetCurrenciesQueryHandler : IRequestHandler<GetCurrenciesQuery, List<CurrencyDto>>
{
    private readonly IRepositoryWrapper _repository;
    private readonly ILoggerManager _logger;

    public GetCurrenciesQueryHandler(IRepositoryWrapper repository, ILoggerManager logger)
    {
        _repository = repository;
        _logger = logger;
    }

    public Task<List<CurrencyDto>> Handle(GetCurrenciesQuery request, CancellationToken cancellationToken)
    {
        _logger.LogInfo("Fetching currencies");

        var result = _repository.Currency
            .FindByCondition(x => x.IsActive)
            .OrderBy(x => x.SortNumber)
            .Select(x => new CurrencyDto
            {
                Id = x.Id,
                CurrencyName = x.CurrencyName,
                SortNumber = x.SortNumber
            })
            .ToList();

        _logger.LogInfo($"Fetched {result.Count} currencies");

        return Task.FromResult(result);
    }
}
