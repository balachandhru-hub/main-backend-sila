using MediatR;
using MasterData.Domain.Dto;
using MasterData.Infrastructure.Contracts.IRepository;
using SharedKernel.LoggerServices;
using SharedKernel.ExceptionHandler;

namespace MasterData.Application.Features.Metadata.Queries;

public class GetMetadataByTypeQueryHandler
    : IRequestHandler<GetMetadataByTypeQuery, List<MetadataDto>>
{
    private readonly IRepositoryWrapper _repository;
    private readonly ILoggerManager _logger;

    public GetMetadataByTypeQueryHandler(
        IRepositoryWrapper repository,
        ILoggerManager logger)
    {
        _repository = repository;
        _logger = logger;
    }

    public Task<List<MetadataDto>> Handle(
    GetMetadataByTypeQuery request,
    CancellationToken cancellationToken)
    {
        _logger.LogInfo(
            $"Fetching metadata for type: {string.Join(",", request.Type)}");

        var result = _repository.Metadata
            .FindByCondition(x =>
                x.IsActive &&
                request.Type.Contains(x.Type))
            .Select(x => new MetadataDto
            {
                Id = x.Id,
                Key = x.Key,
                Type = x.Type,
                Description = x.Description
            })
            .ToList();

        if (!result.Any())
        {
            _logger.LogError(
                $"No metadata found for type: {string.Join(",", request.Type)}");

            throw new NotFoundCustomException(
                $"No metadata found for type: {string.Join(",", request.Type)}",
                $"No metadata found for type: {string.Join(",", request.Type)}");
        }

        _logger.LogInfo(
            $"Fetched metadata for type: {string.Join(",", request.Type)}");

        return Task.FromResult(result);
    }
}