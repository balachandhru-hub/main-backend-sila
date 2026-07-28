using MasterData.Domain.Dto;
using MasterData.Infrastructure.Contracts.IRepository;
using MediatR;
using SharedKernel.LoggerServices;

namespace MasterData.Application.Features.Lookup.Queries;

public class GetCurrenciesQueryHandler
    : IRequestHandler<GetCurrenciesQuery, PagedResultDto<CurrencyDto>>
{
    private readonly IRepositoryWrapper _repository;
    private readonly ILoggerManager _logger;

    public GetCurrenciesQueryHandler(IRepositoryWrapper repository, ILoggerManager logger)
    {
        _repository = repository;
        _logger = logger;
    }

    public Task<PagedResultDto<CurrencyDto>> Handle(
        GetCurrenciesQuery request,
        CancellationToken cancellationToken
    )
    {
        _logger.LogInfo("Fetching currencies");

        var query = _repository
            .Currency.FindByCondition(x => x.IsActive)
            .OrderBy(x => x.SortNumber);

        var totalCount = query.Count();

        var items = query
            .Skip(request.Index)
            .Take(request.Limit)
            .Select(x => new CurrencyDto
            {
                Id = x.Id,
                CurrencyName = x.CurrencyName,
                SortNumber = x.SortNumber,
            })
            .ToList();

        _logger.LogInfo($"Fetched {items.Count} currencies");

        return Task.FromResult(
            new PagedResultDto<CurrencyDto>
            {
                Items = items
            }
        );
    }
}
