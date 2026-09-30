using Contracts.IRepository;
using Identity.Domain.Dto;
using MediatR;
using SharedKernel.LoggerServices;
namespace Identity.Application.Features.Queries.GetAllModel
{
    public class GetAllModelQueryHandler
        : IRequestHandler<GetAllModelQuery, List<ModelDto>>
    {
        private readonly IRepositoryWrapper _repository;
        private readonly ILoggerManager _logger;

        public GetAllModelQueryHandler(
            IRepositoryWrapper repository,
            ILoggerManager logger
            )
        {
            _repository = repository;
            _logger = logger;
        }

        public async Task<List<ModelDto>> Handle(
            GetAllModelQuery request,
            CancellationToken cancellationToken)
        {
            _logger.LogInfo("Fetching all active model from the database.");
            var result = _repository.ModelMapping
                .FindByConditionAsync(x => x.IsActive)
                .OrderBy(x => x.ModelName)
                .Select(x => new ModelDto
                {
                    Id = x.Id,
                    Key = x.Key,
                    ModelName = x.ModelName
                })
                .ToList();
        _logger.LogInfo($"Fetched {result.Count} active model from the database.");
            return await Task.FromResult(result);
        }
    }
}