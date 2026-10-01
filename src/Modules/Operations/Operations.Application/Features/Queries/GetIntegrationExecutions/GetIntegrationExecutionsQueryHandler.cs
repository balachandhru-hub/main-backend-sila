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

namespace Operations.Application.Features.Queries.GetIntegrationExecutions
{
    public class GetIntegrationExecutionsQueryHandler : IRequestHandler<GetIntegrationExecutionsQuery, List<IntegrationExecutionResponseDto>>
    {
        private readonly IRepositoryWrapper _repository;
        private readonly ILoggerManager _logger;

        public GetIntegrationExecutionsQueryHandler(IRepositoryWrapper repository, ILoggerManager logger)
        {
            _repository = repository;
            _logger = logger;
        }

        public async Task<List<IntegrationExecutionResponseDto>> Handle(GetIntegrationExecutionsQuery request, CancellationToken cancellationToken)
        {
            _logger.LogInfo($"Fetching integration executions. ConfigurationId: {request.ConfigurationId}, OrganizationId: {request.OrganizationId}");

            List<Guid> configurationIds = await _repository.ApiIntegrationConfiguration
                .FindByCondition(x => x.OrganizationId == request.OrganizationId
                    && (request.ConfigurationId == null || x.Id == request.ConfigurationId))
                .Select(x => x.Id)
                .ToListAsync(cancellationToken);
            List<ApiIntegrationExecution> executions = await _repository.ApiIntegrationExecution
                .FindByCondition(x => configurationIds.Contains(x.ConfigurationId))
                .OrderByDescending(x => x.StartedAt)
                .Take(100)
                .ToListAsync(cancellationToken);

            _logger.LogInfo($"Integration executions fetched. Count: {executions.Count}, OrganizationId: {request.OrganizationId}");
            return executions.Select(ResponseBuilder.Execution).ToList();
        }
    }
}
