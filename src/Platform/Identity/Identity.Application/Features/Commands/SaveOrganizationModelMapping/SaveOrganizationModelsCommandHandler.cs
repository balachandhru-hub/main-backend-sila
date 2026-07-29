using Contracts.IRepository;
using Identity.Domain.Entities;
using MediatR;
using SharedKernel.LoggerServices;

namespace Identity.Application.Features.Commands.SaveOrganizationModelMapping
{
    public class SaveOrganizationModelsCommandHandler
        : IRequestHandler<SaveOrganizationModelsCommand, bool>
    {
        private readonly IRepositoryWrapper _repository;
        private readonly ILoggerManager _logger;

        public SaveOrganizationModelsCommandHandler(
            IRepositoryWrapper repository,
            ILoggerManager logger)
        {
            _repository = repository;
            _logger = logger;
        }

        public async Task<bool> Handle(
     SaveOrganizationModelsCommand request,
     CancellationToken cancellationToken)
        {
            _logger.LogInfo("Starting to save organization model mappings.");
            var organizationId = request.Model.OrganizationId;
            _logger.LogInfo($"Fetching existing mappings for organization ID: {organizationId}.");
            var existingMappings = _repository.OrganizationModelMapping
                .FindByConditionAsync(x => x.OrganizationId == organizationId)
                .ToList();
            _logger.LogInfo($"Found {existingMappings.Count} existing mappings for organization ID: {organizationId}.");
            var existingModelIds = existingMappings
                .Select(x => x.ModelId)
                .ToList();
            _logger.LogInfo($"Existing model IDs for organization ID {organizationId}: {string.Join(", ", existingModelIds)}");
            var incomingModelIds = request.Model.ModelIds;

           _logger.LogInfo($"Incoming model IDs for organization ID {organizationId}: {string.Join(", ", incomingModelIds)}");
            var newModelIds = incomingModelIds.Except(existingModelIds);

            foreach (var modelId in newModelIds)
            {
                _logger.LogInfo($"Adding new mapping for organization ID {organizationId} and model ID {modelId}.");
                await _repository.OrganizationModelMapping.CreateAsync(
                    new OrganizationModelMapping
                    {
                        Id = Guid.NewGuid(),
                        OrganizationId = organizationId,
                        ModelId = modelId
                    });
            }

            _logger.LogInfo($"Removing mappings for organization ID {organizationId} that are not in the incoming model IDs.");
            var removeModelIds = existingModelIds.Except(incomingModelIds);

            foreach (var modelId in removeModelIds)
            {
                _logger.LogInfo($"Removing mapping for organization ID {organizationId} and model ID {modelId}.");
                var mapping = existingMappings.First(x => x.ModelId == modelId);

                _repository.OrganizationModelMapping.Delete(mapping);
            }

            await _repository.SaveAsync();

            return true;
        }
    }
}