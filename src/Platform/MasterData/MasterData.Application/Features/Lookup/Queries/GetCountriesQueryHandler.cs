using MediatR;
using MasterData.Domain.Dto;
using MasterData.Infrastructure.Contracts.IRepository;
using SharedKernel.LoggerServices;

namespace MasterData.Application.Features.Lookup.Queries;

public class GetCountriesQueryHandler
    : IRequestHandler<GetCountriesQuery, PagedResultDto<CountryDto>>
{
    private readonly IRepositoryWrapper _repository;
    private readonly ILoggerManager _logger;

    public GetCountriesQueryHandler(IRepositoryWrapper repository, ILoggerManager logger)
    {
        _repository = repository;
        _logger = logger;
    }

    public Task<PagedResultDto<CountryDto>> Handle(
        GetCountriesQuery request,
        CancellationToken cancellationToken
    )
    {
        _logger.LogInfo("Fetching countries");

        var query = _repository
            .CountryList.FindByCondition(x => x.IsActive)
            .Where(x =>
                string.IsNullOrWhiteSpace(request.SearchTerm)
                || x.CountryName.ToLower().Contains(request.SearchTerm.ToLower())
            )
            .OrderBy(x => x.CountryName);

        int totalCount = query.Count();

        var items = query
            .Skip(request.Index)
            .Take(request.Limit)
            .Select(x => new CountryDto
            {
                Id = x.Id,
                CountryName = x.CountryName,
                CountryCode = x.CountryCode,
                MobileCountryCode = x.MobileCountryCode,
            })
            .ToList();

        _logger.LogInfo($"Fetched {items.Count} countries");

        return Task.FromResult(
            new PagedResultDto<CountryDto>
            {
                Items = items
            }
        );
    }
}
