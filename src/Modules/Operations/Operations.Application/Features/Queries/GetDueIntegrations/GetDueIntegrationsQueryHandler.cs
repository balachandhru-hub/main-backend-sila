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

namespace Operations.Application.Features.Queries.GetDueIntegrations
{
    public class GetDueIntegrationsQueryHandler : IRequestHandler<GetDueIntegrationsQuery, List<DueIntegrationDto>>
    {
        private readonly IRepositoryWrapper _repository;
        private readonly ILoggerManager _logger;

        public GetDueIntegrationsQueryHandler(IRepositoryWrapper repository, ILoggerManager logger)
        {
            _repository = repository;
            _logger = logger;
        }

        public async Task<List<DueIntegrationDto>> Handle(GetDueIntegrationsQuery request, CancellationToken cancellationToken)
        {
            List<ApiIntegrationConfiguration> due = await _repository.ApiIntegrationConfiguration.ListDueAsync(DateTime.UtcNow, 10, cancellationToken);
            if (due.Count > 0)
            {
                _logger.LogInfo($"Scheduled integrations are due. Count: {due.Count}");
            }

            return due.Select(item => new DueIntegrationDto { OrganizationId = item.OrganizationId, ConfigurationId = item.Id }).ToList();
        }
    }
}
