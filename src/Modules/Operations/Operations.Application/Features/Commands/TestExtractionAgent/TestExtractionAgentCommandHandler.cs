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

namespace Operations.Application.Features.Commands.TestExtractionAgent
{
    public class TestExtractionAgentCommandHandler : IRequestHandler<TestExtractionAgentCommand, ExtractionAgentTestResponseDto>
    {
        private readonly IRepositoryWrapper _repository;
        private readonly ILoggerManager _logger;

        public TestExtractionAgentCommandHandler(IRepositoryWrapper repository, ILoggerManager logger)
        {
            _repository = repository;
            _logger = logger;
        }

        public async Task<ExtractionAgentTestResponseDto> Handle(TestExtractionAgentCommand request, CancellationToken cancellationToken)
        {
            _logger.LogInfo($"Testing extraction agent. AgentId: {request.AgentId}, OrganizationId: {request.OrganizationId}, UserId: {request.UserId}");

            ExtractionAgentConfig? configuration = await _repository.ExtractionAgentConfig
                .FindByCondition(x => x.Id == request.AgentId && (x.OrganizationId == null || x.OrganizationId == request.OrganizationId) && x.IsActive)
                .FirstOrDefaultAsync(cancellationToken);
            if (configuration == null)
            {
                _logger.LogError($"Extraction agent not found. AgentId: {request.AgentId}, OrganizationId: {request.OrganizationId}");
                throw new NotFoundCustomException("Extraction agent not found.", "The extraction agent does not exist in your organization.");
            }

            bool valid = Uri.TryCreate(configuration.EndpointUrl, UriKind.Absolute, out Uri? endpoint)
                && (endpoint.Scheme == Uri.UriSchemeHttp || endpoint.Scheme == Uri.UriSchemeHttps);
            AuditTrail.Add(_repository, request.OrganizationId, null, request.UserId, "AGENT_CONNECTION_TESTED", "ExtractionAgentConfig", configuration.Id, configuration.Name, valid ? "SUCCESS" : "FAILED");
            await _repository.SaveAsync();

            _logger.LogInfo($"Extraction agent tested. AgentId: {configuration.Id}, Valid: {valid}");
            return new ExtractionAgentTestResponseDto
            {
                Success = valid,
                Message = valid
                    ? "The extraction endpoint configuration is valid."
                    : "Set a valid HTTP or HTTPS endpoint before testing the connection."
            };
        }
    }
}
