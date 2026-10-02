using MediatR;
using Microsoft.EntityFrameworkCore;
using Operations.Application.Features.Commands.RunIntegration;
using Operations.Domain.Dtos;
using Operations.Domain.Entities;
using Operations.Domain.Enums;
using Operations.Infrastructure.Contracts.IRepository;
using SharedKernel.ExceptionHandler;
using SharedKernel.LoggerServices;

namespace Operations.Application.Features.Commands.RunOrganizationIntegrations
{
    public class RunOrganizationIntegrationsCommandHandler : IRequestHandler<RunOrganizationIntegrationsCommand, RunOrganizationIntegrationsResultDto>
    {
        // A pull that succeeded moments ago is not repeated: several buyers may refresh at the same time.
        private static readonly TimeSpan FreshFor = TimeSpan.FromSeconds(30);

        private readonly IRepositoryWrapper _repository;
        private readonly IMediator _mediator;
        private readonly ILoggerManager _logger;

        public RunOrganizationIntegrationsCommandHandler(
            IRepositoryWrapper repository,
            IMediator mediator,
            ILoggerManager logger)
        {
            _repository = repository;
            _mediator = mediator;
            _logger = logger;
        }

        public async Task<RunOrganizationIntegrationsResultDto> Handle(RunOrganizationIntegrationsCommand request, CancellationToken cancellationToken)
        {
            List<Guid> organizationIds = request.Request.OrganizationIds.Distinct().ToList();
            _logger.LogInfo($"Running organization integrations. ProcessType: {request.Request.ProcessType}, Organizations: {organizationIds.Count}");

            RunOrganizationIntegrationsResultDto result = new RunOrganizationIntegrationsResultDto();
            List<ApiIntegrationConfiguration> configurations = await _repository.ApiIntegrationConfiguration
                .FindByCondition(x => organizationIds.Contains(x.OrganizationId)
                    && x.ProcessType == request.Request.ProcessType
                    && x.Status == IntegrationConfigurationStatus.ACTIVE
                    && x.IsActive)
                .ToListAsync(cancellationToken);

            DateTime freshAfter = DateTime.UtcNow - FreshFor;
            foreach (ApiIntegrationConfiguration configuration in configurations)
            {
                if (configuration.LastSuccessfulRunAt != null && configuration.LastSuccessfulRunAt > freshAfter)
                {
                    result.Skipped++;
                    continue;
                }

                try
                {
                    await _mediator.Send(new RunIntegrationCommand
                    {
                        OrganizationId = configuration.OrganizationId,
                        ConfigurationId = configuration.Id,
                        Trigger = IntegrationExecutionTrigger.SCHEDULED,
                        FullSync = true
                    }, cancellationToken);
                    result.Succeeded++;
                }
                catch (ConflictCustomException)
                {
                    // Already running: its result is on its way.
                    result.Skipped++;
                }
                catch (BaseCustomException exception)
                {
                    // The run is already recorded as failed on the configuration; the others continue.
                    result.Failed++;
                    _logger.LogError($"Organization integration failed. ConfigurationId: {configuration.Id}, Error: {exception.Message}");
                }
            }

            _logger.LogInfo($"Organization integrations run. Succeeded: {result.Succeeded}, Failed: {result.Failed}, Skipped: {result.Skipped}");
            return result;
        }
    }
}
