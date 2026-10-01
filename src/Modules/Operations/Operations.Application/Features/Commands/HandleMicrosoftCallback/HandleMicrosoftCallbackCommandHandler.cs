using System.Security.Cryptography;
using System.Text;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Operations.Application.Features.Shared;
using Operations.Application.Services.Graph;
using Operations.Domain.Common;
using Operations.Domain.Dtos;
using Operations.Domain.Entities;
using Operations.Domain.Enums;
using Operations.Infrastructure.Contracts.IRepository;
using Operations.Infrastructure.Contracts.IServices;
using SharedKernel.ExceptionHandler;
using SharedKernel.LoggerServices;

namespace Operations.Application.Features.Commands.HandleMicrosoftCallback
{
    public class HandleMicrosoftCallbackCommandHandler : IRequestHandler<HandleMicrosoftCallbackCommand, string>
    {
        private readonly IRepositoryWrapper _repository;
        private readonly ILoggerManager _logger;
        private readonly IMicrosoftGraphClient _graph;
        private readonly IConfiguration _configuration;
        private readonly IUserContext _userContext;

        public HandleMicrosoftCallbackCommandHandler(
            IRepositoryWrapper repository,
            ILoggerManager logger,
            IMicrosoftGraphClient graph,
            IConfiguration configuration,
            IUserContext userContext)
        {
            _repository = repository;
            _logger = logger;
            _graph = graph;
            _configuration = configuration;
            _userContext = userContext;
        }

        public async Task<string> Handle(HandleMicrosoftCallbackCommand request, CancellationToken cancellationToken)
        {
            _logger.LogInfo("Handling Microsoft authorization callback.");

            MicrosoftCallbackRequestDto dto = request.Request;
            if (string.IsNullOrWhiteSpace(dto.State))
            {
                _logger.LogError("Microsoft callback arrived without a state.");
                return Redirect(null, "microsoft=error&reason=invalid_state");
            }

            // The one-time state is what authenticates this request: it identifies the user and
            // organization that started the sign-in, it expires after 10 minutes and works once.
            string stateHash = Convert.ToBase64String(SHA256.HashData(Encoding.UTF8.GetBytes(dto.State))).TrimEnd('=').Replace('+', '-').Replace('/', '_');
            MicrosoftAuthorizationState? state = await _repository.MicrosoftAuthorizationState.FindFirstByConditionAsync(x => x.StateHash == stateHash);
            if (state == null || state.UsedAt != null || state.ExpiresAt <= DateTime.UtcNow)
            {
                _logger.LogError("Microsoft callback state is unknown, expired or already used.");
                return Redirect(state?.ReturnUrl, "microsoft=error&reason=expired_or_reused_state");
            }

            _userContext.SetCurrentUserId(state.UserId);
            state.UsedAt = DateTime.UtcNow;
            if (!string.IsNullOrWhiteSpace(dto.Error) || string.IsNullOrWhiteSpace(dto.Code))
            {
                await _repository.SaveAsync();
                _logger.LogError($"Microsoft sign-in was not completed. OrganizationId: {state.OrganizationId}, Error: {dto.Error}");
                return Redirect(state.ReturnUrl, string.IsNullOrWhiteSpace(dto.Error) ? "microsoft=error&reason=missing_code" : "microsoft=error&reason=consent_denied");
            }

            MicrosoftToken token;
            try
            {
                token = await _graph.ExchangeCodeAsync(dto.Code, cancellationToken);
            }
            catch (Exception exception) when (exception is MicrosoftGraphException or HttpRequestException)
            {
                await _repository.SaveAsync();
                _logger.LogError($"Microsoft token exchange failed. OrganizationId: {state.OrganizationId}, Error: {exception.Message}");
                return Redirect(state.ReturnUrl, "microsoft=error&reason=configuration_or_token_exchange");
            }

            // The organization has one Microsoft connection waiting for (or renewing) its sign-in.
            DocumentStorageConnection? connection = await _repository.DocumentStorageConnection.FindFirstByConditionAsync(x =>
                x.OrganizationId == state.OrganizationId && x.Provider == DocumentStorageProvider.MICROSOFT && x.IsActive);
            if (connection == null)
            {
                connection = new DocumentStorageConnection
                {
                    Id = Guid.NewGuid(),
                    OrganizationId = state.OrganizationId,
                    Provider = DocumentStorageProvider.MICROSOFT,
                    Name = Common.MICROSOFT_CONNECTION_NAME
                };
                _repository.DocumentStorageConnection.Create(connection);
            }

            // A connection that already has a validated folder stays connected; only its token is renewed.
            bool validated = connection.ConnectionStatus == StorageConnectionStatus.CONNECTED
                && !string.IsNullOrWhiteSpace(connection.DriveIdentifier) && !string.IsNullOrWhiteSpace(connection.FolderIdentifier);
            connection.ConnectionStatus = validated ? StorageConnectionStatus.CONNECTED : StorageConnectionStatus.AUTHENTICATED;
            connection.TenantIdentifier = _graph.GetTenantId();
            connection.CredentialReference = _graph.ProtectToken(token);
            connection.ConnectedAt = DateTime.UtcNow;
            AuditTrail.Add(_repository, state.OrganizationId, null, state.UserId, "DOCUMENT_STORAGE_AUTHENTICATED", "DocumentStorageConnection", connection.Id, connection.Name, "SUCCESS");
            await _repository.SaveAsync();

            _logger.LogInfo($"Microsoft connection authenticated. ConnectionId: {connection.Id}, OrganizationId: {state.OrganizationId}");
            return Redirect(state.ReturnUrl, $"microsoft=connected&connectionId={connection.Id}");
        }

        // Frontend origin (the one CORS allows) + the validated relative return path + the result.
        private string Redirect(string? returnUrl, string query)
        {
            string origin = (_configuration[Common.DEFAULT_FRONT_END_ORIGIN_LOCAL] ?? string.Empty).TrimEnd('/');
            string path = MicrosoftReturnUrl.Normalize(returnUrl);
            return $"{origin}{path}{(path.Contains('?') ? "&" : "?")}{query}";
        }
    }
}
