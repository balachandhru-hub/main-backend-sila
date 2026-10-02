using MediatR;
using Microsoft.EntityFrameworkCore;
using Operations.Application.Features.Shared;
using Operations.Application.Services.Integration;
using Operations.Domain.Common;
using Operations.Domain.Dtos;
using Operations.Domain.Entities;
using Operations.Domain.Enums;
using Operations.Infrastructure.Contracts.IRepository;
using SharedKernel.ExceptionHandler;
using SharedKernel.LoggerServices;

namespace Operations.Application.Features.Commands.TestIntegration
{
    public class TestIntegrationCommandHandler : IRequestHandler<TestIntegrationCommand, IntegrationTestResponseDto>
    {
        private readonly IRepositoryWrapper _repository;
        private readonly ILoggerManager _logger;
        private readonly IIntegrationHttpExecutor _executor;

        public TestIntegrationCommandHandler(
            IRepositoryWrapper repository,
            ILoggerManager logger,
            IIntegrationHttpExecutor executor)
        {
            _repository = repository;
            _logger = logger;
            _executor = executor;
        }

        public async Task<IntegrationTestResponseDto> Handle(TestIntegrationCommand request, CancellationToken cancellationToken)
        {
            _logger.LogInfo($"Testing integration. ConfigurationId: {request.ConfigurationId}, OrganizationId: {request.OrganizationId}, UserId: {request.UserId}");

            ApiIntegrationConfiguration configuration = await IntegrationConfigurationRules.GetTrackedAsync(_repository, _logger, request.ConfigurationId, request.OrganizationId);
            ApiIntegrationExecution execution = new ApiIntegrationExecution
            {
                Id = Guid.NewGuid(),
                ConfigurationId = configuration.Id,
                Trigger = IntegrationExecutionTrigger.TEST,
                Status = IntegrationExecutionStatus.RUNNING,
                StartedAt = DateTime.UtcNow
            };
            _repository.ApiIntegrationExecution.Create(execution);

            bool success;
            string message;
            int? httpStatus;
            string? errorCode = null;
            try
            {
                using HttpResponseMessage response = await _executor.SendAsync(configuration, _executor.BuildUrl(configuration, true), HttpMethod.Get, null, cancellationToken);
                httpStatus = (int)response.StatusCode;

                // A POST-only endpoint answers a probe with 405: it is reachable and the credentials were accepted.
                success = response.IsSuccessStatusCode || (IntegrationProcessCatalog.Find(configuration.ProcessType)?.IsPush == true && httpStatus == 405);
                message = success ? "API connection succeeded." : $"The API returned HTTP {httpStatus}.";
                errorCode = success ? null : "REMOTE_HTTP_ERROR";
            }
            catch (IntegrationException exception)
            {
                success = false;
                message = exception.Message;
                httpStatus = exception.Status;
                errorCode = exception.Code;
            }

            DateTime testedAt = DateTime.UtcNow;
            execution.Status = success ? IntegrationExecutionStatus.SUCCESS : IntegrationExecutionStatus.FAILED;
            execution.CompletedAt = testedAt;
            execution.ErrorCode = errorCode;
            execution.ErrorMessageSafe = success ? null : message;
            configuration.TestedAt = testedAt;
            configuration.LastErrorSafe = success ? null : message;
            if (configuration.Status != IntegrationConfigurationStatus.ACTIVE || !success)
            {
                configuration.Status = success ? IntegrationConfigurationStatus.TESTED : IntegrationConfigurationStatus.TEST_FAILED;
            }

            await _repository.SaveAsync();
            if (success)
            {
                _logger.LogInfo($"Integration test succeeded. ConfigurationId: {configuration.Id}, HttpStatus: {httpStatus}");
            }
            else
            {
                _logger.LogError($"Integration test failed. ConfigurationId: {configuration.Id}, Code: {errorCode}, HttpStatus: {httpStatus}");
            }

            return new IntegrationTestResponseDto { Success = success, Message = message, HttpStatus = httpStatus, TestedAt = testedAt };
        }
    }
}
