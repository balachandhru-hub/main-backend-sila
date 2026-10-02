using System.Diagnostics;
using MediatR;
using Operations.Application.Features.Shared;
using Operations.Application.Services.Integration;
using Operations.Domain.Dtos;
using Operations.Domain.Entities;
using Operations.Domain.Enums;
using Operations.Infrastructure.Contracts.IRepository;
using SharedKernel.ExceptionHandler;
using SharedKernel.LoggerServices;

namespace Operations.Application.Features.Commands.SendIntegrationRequest
{
    /// <summary>
    /// Sends one document (a purchase order) to an organization's API. The document is built by the
    /// service that owns it; this service adds the saved sign-in and makes the call, once.
    /// </summary>
    public class SendIntegrationRequestCommandHandler : IRequestHandler<SendIntegrationRequestCommand, SendIntegrationResponseDto>
    {
        private const int RESPONSE_LIMIT = 4000;

        // Failures after which the API may still have received the document.
        private static readonly string[] UnknownOutcomeCodes = { "REMOTE_UNAVAILABLE", "REMOTE_TIMEOUT" };

        private readonly IRepositoryWrapper _repository;
        private readonly ILoggerManager _logger;
        private readonly IIntegrationHttpExecutor _executor;

        public SendIntegrationRequestCommandHandler(
            IRepositoryWrapper repository,
            ILoggerManager logger,
            IIntegrationHttpExecutor executor)
        {
            _repository = repository;
            _logger = logger;
            _executor = executor;
        }

        public async Task<SendIntegrationResponseDto> Handle(SendIntegrationRequestCommand request, CancellationToken cancellationToken)
        {
            _logger.LogInfo($"Sending document to integration. ConfigurationId: {request.Request.ConfigurationId}, OrganizationId: {request.Request.OrganizationId}");

            ApiIntegrationConfiguration configuration = await IntegrationConfigurationRules.GetTrackedAsync(
                _repository, _logger, request.Request.ConfigurationId, request.Request.OrganizationId);
            if (configuration.Status != IntegrationConfigurationStatus.ACTIVE
                || IntegrationProcessCatalog.Find(configuration.ProcessType)?.IsPush != true)
            {
                _logger.LogError($"Integration cannot be sent to. ConfigurationId: {configuration.Id}, Status: {configuration.Status}, ProcessType: {configuration.ProcessType}");
                throw new BadRequestCustomException("API is not active.", "Activate this API before documents are sent to it.");
            }

            SendIntegrationResponseDto result = new SendIntegrationResponseDto();
            Stopwatch watch = Stopwatch.StartNew();
            configuration.LastAttemptAt = DateTime.UtcNow;
            try
            {
                using HttpResponseMessage response = await _executor.SendOnceAsync(
                    configuration,
                    _executor.ResourceUrl(configuration),
                    new HttpMethod(configuration.HttpMethod),
                    request.Request.Body,
                    request.Request.Headers,
                    cancellationToken);
                string body = await response.Content.ReadAsStringAsync(cancellationToken);
                result.Answered = true;
                result.StatusCode = (int)response.StatusCode;
                result.ResponseBody = body.Length > RESPONSE_LIMIT ? body[..RESPONSE_LIMIT] : body;

                // A server error or a request timeout answer leaves it open whether the document was created.
                result.OutcomeUnknown = (int)response.StatusCode >= 500 || (int)response.StatusCode == 408;
                if (response.IsSuccessStatusCode)
                {
                    configuration.LastSuccessfulRunAt = DateTime.UtcNow;
                    configuration.LastErrorSafe = null;
                }
                else
                {
                    result.ErrorCode = "REMOTE_HTTP_ERROR";
                    result.ErrorMessage = $"The API returned HTTP {(int)response.StatusCode}.";
                    configuration.LastErrorSafe = result.ErrorMessage;
                }
            }
            catch (IntegrationException exception)
            {
                result.Answered = false;
                result.OutcomeUnknown = UnknownOutcomeCodes.Contains(exception.Code);
                result.ErrorCode = exception.Code;
                result.ErrorMessage = exception.Message;
                configuration.LastErrorSafe = exception.Message;
            }

            watch.Stop();
            result.DurationMs = watch.ElapsedMilliseconds;
            await _repository.SaveAsync();

            if (result.ErrorCode == null)
            {
                _logger.LogInfo($"Document sent to integration. ConfigurationId: {configuration.Id}, StatusCode: {result.StatusCode}, DurationMs: {result.DurationMs}");
            }
            else
            {
                _logger.LogError($"Document could not be sent to integration. ConfigurationId: {configuration.Id}, Code: {result.ErrorCode}, StatusCode: {result.StatusCode}, DurationMs: {result.DurationMs}");
            }

            return result;
        }
    }
}
