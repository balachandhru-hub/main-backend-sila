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

namespace Operations.Application.Features.Commands.UpdateExtractionAgent
{
    public class UpdateExtractionAgentCommandHandler : IRequestHandler<UpdateExtractionAgentCommand, ExtractionAgentConfigResponseDto>
    {
        private readonly IRepositoryWrapper _repository;
        private readonly ILoggerManager _logger;

        public UpdateExtractionAgentCommandHandler(IRepositoryWrapper repository, ILoggerManager logger)
        {
            _repository = repository;
            _logger = logger;
        }

        public async Task<ExtractionAgentConfigResponseDto> Handle(UpdateExtractionAgentCommand request, CancellationToken cancellationToken)
        {
            _logger.LogInfo($"Updating extraction agent. AgentId: {request.AgentId}, OrganizationId: {request.OrganizationId}, UserId: {request.UserId}");

            ExtractionAgentConfig? configuration = await _repository.ExtractionAgentConfig.FindFirstByConditionAsync(
                x => x.Id == request.AgentId && x.OrganizationId == request.OrganizationId && x.IsActive);
            if (configuration == null)
            {
                _logger.LogError($"Extraction agent not found. AgentId: {request.AgentId}, OrganizationId: {request.OrganizationId}");
                throw new NotFoundCustomException("Extraction agent not found.", "The extraction agent does not exist in your organization.");
            }

            ExtractionAgentRules.Apply(configuration, request.Request, _logger);
            AuditTrail.Add(_repository, request.OrganizationId, null, request.UserId, "AGENT_CONFIG_UPDATED", "ExtractionAgentConfig", configuration.Id, configuration.Name);
            await _repository.SaveAsync();

            _logger.LogInfo($"Extraction agent updated. AgentId: {configuration.Id}, OrganizationId: {request.OrganizationId}");
            return ResponseBuilder.ExtractionAgent(configuration);
        }
    }
}
