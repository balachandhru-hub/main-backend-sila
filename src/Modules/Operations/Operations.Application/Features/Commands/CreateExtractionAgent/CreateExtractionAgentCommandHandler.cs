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

namespace Operations.Application.Features.Commands.CreateExtractionAgent
{
    public class CreateExtractionAgentCommandHandler : IRequestHandler<CreateExtractionAgentCommand, ExtractionAgentConfigResponseDto>
    {
        private readonly IRepositoryWrapper _repository;
        private readonly ILoggerManager _logger;

        public CreateExtractionAgentCommandHandler(IRepositoryWrapper repository, ILoggerManager logger)
        {
            _repository = repository;
            _logger = logger;
        }

        public async Task<ExtractionAgentConfigResponseDto> Handle(CreateExtractionAgentCommand request, CancellationToken cancellationToken)
        {
            _logger.LogInfo($"Creating extraction agent. Name: {request.Request.Name}, OrganizationId: {request.OrganizationId}, UserId: {request.UserId}");

            ExtractionAgentConfig configuration = new ExtractionAgentConfig
            {
                Id = Guid.NewGuid(),
                OrganizationId = request.OrganizationId
            };
            ExtractionAgentRules.Apply(configuration, request.Request, _logger);
            _repository.ExtractionAgentConfig.Create(configuration);
            AuditTrail.Add(_repository, request.OrganizationId, null, request.UserId, "AGENT_CONFIG_CREATED", "ExtractionAgentConfig", configuration.Id, configuration.Name);
            await _repository.SaveAsync();

            _logger.LogInfo($"Extraction agent created. AgentId: {configuration.Id}, OrganizationId: {request.OrganizationId}");
            return ResponseBuilder.ExtractionAgent(configuration);
        }
    }
}
