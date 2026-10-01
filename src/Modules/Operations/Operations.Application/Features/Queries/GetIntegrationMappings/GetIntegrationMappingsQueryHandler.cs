using MediatR;
using Microsoft.EntityFrameworkCore;
using Operations.Application.Features.Shared;
using Operations.Domain.Common;
using Operations.Domain.Dtos;
using Operations.Domain.Entities;
using Operations.Domain.Enums;
using Operations.Infrastructure.Contracts.IRepository;
using SharedKernel.ExceptionHandler;
using SharedKernel.LoggerServices;

namespace Operations.Application.Features.Queries.GetIntegrationMappings
{
    public class GetIntegrationMappingsQueryHandler : IRequestHandler<GetIntegrationMappingsQuery, List<IntegrationMappingResponseDto>>
    {
        private readonly IRepositoryWrapper _repository;
        private readonly ILoggerManager _logger;

        public GetIntegrationMappingsQueryHandler(IRepositoryWrapper repository, ILoggerManager logger)
        {
            _repository = repository;
            _logger = logger;
        }

        public async Task<List<IntegrationMappingResponseDto>> Handle(GetIntegrationMappingsQuery request, CancellationToken cancellationToken)
        {
            _logger.LogInfo($"Fetching integration mappings. ConfigurationId: {request.ConfigurationId}, OrganizationId: {request.OrganizationId}");

            bool exists = await _repository.ApiIntegrationConfiguration
                .FindByCondition(x => x.Id == request.ConfigurationId && x.OrganizationId == request.OrganizationId && x.IsActive)
                .AnyAsync(cancellationToken);
            if (!exists)
            {
                _logger.LogError($"Integration configuration not found. ConfigurationId: {request.ConfigurationId}, OrganizationId: {request.OrganizationId}");
                throw new NotFoundCustomException("Integration not found.", "The integration configuration does not exist in your organization.");
            }

            List<ApiFieldMapping> mappings = await _repository.ApiFieldMapping
                .FindByCondition(x => x.ConfigurationId == request.ConfigurationId)
                .OrderBy(x => x.TargetField)
                .ToListAsync(cancellationToken);

            _logger.LogInfo($"Integration mappings fetched. ConfigurationId: {request.ConfigurationId}, Count: {mappings.Count}");
            return mappings.Select(ResponseBuilder.Mapping).ToList();
        }
    }
}
