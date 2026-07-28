using MasterData.Domain.Dto;
using MasterData.Infrastructure.Contracts.IRepository;
using MediatR;
using SharedKernel.LoggerServices;

namespace MasterData.Application.Features.Lookup.Queries;

public class GetUnitsQueryHandler : IRequestHandler<GetUnitsQuery, PagedResultDto<UnitDto>>
{
    private readonly IRepositoryWrapper _repository;
    private readonly ILoggerManager _logger;

    public GetUnitsQueryHandler(IRepositoryWrapper repository, ILoggerManager logger)
    {
        _repository = repository;
        _logger = logger;
    }

    public Task<PagedResultDto<UnitDto>> Handle(
        GetUnitsQuery request,
        CancellationToken cancellationToken
    )
    {
        var query = _repository.Unit.FindByCondition(x => x.IsActive);

        if (!string.IsNullOrWhiteSpace(request.SearchTerm))
        {
            var term = request.SearchTerm.ToLower();
            query = query.Where(x =>
                x.Key.ToLower().Contains(term) || x.Type.ToLower().Contains(term)
            );
        }

        var totalCount = query.Count();

        var items = query
            .OrderBy(x => x.Key)
            .Skip(request.Index)
            .Take(request.Limit)
            .Select(x => new UnitDto
            {
                Id = x.Id,
                Key = x.Key,
                Type = x.Type,
                Description = x.Description,
            })
            .ToList();

        return Task.FromResult(
            new PagedResultDto<UnitDto>
            {
                Items = items
            }
        );
    }
}
