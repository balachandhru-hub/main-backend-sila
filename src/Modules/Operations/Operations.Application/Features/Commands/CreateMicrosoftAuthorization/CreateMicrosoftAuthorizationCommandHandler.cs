using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Operations.Application.Features.Shared;
using Operations.Application.Services.Graph;
using Operations.Domain.Common;
using Operations.Domain.Dtos;
using Operations.Domain.Entities;
using Operations.Domain.Enums;
using Operations.Infrastructure.Contracts.IRepository;
using SharedKernel.ExceptionHandler;
using SharedKernel.LoggerServices;

namespace Operations.Application.Features.Commands.CreateMicrosoftAuthorization
{
    public class CreateMicrosoftAuthorizationCommandHandler : IRequestHandler<CreateMicrosoftAuthorizationCommand, MicrosoftConnectResponseDto>
    {
        private readonly IRepositoryWrapper _repository;
        private readonly ILoggerManager _logger;
        private readonly IMicrosoftGraphClient _graph;

        public CreateMicrosoftAuthorizationCommandHandler(
            IRepositoryWrapper repository,
            ILoggerManager logger,
            IMicrosoftGraphClient graph)
        {
            _repository = repository;
            _logger = logger;
            _graph = graph;
        }

        public async Task<MicrosoftConnectResponseDto> Handle(CreateMicrosoftAuthorizationCommand request, CancellationToken cancellationToken)
        {
            _logger.LogInfo($"Starting Microsoft authorization. OrganizationId: {request.OrganizationId}, UserId: {request.UserId}");

            string draftJson = SanitizeDraft(request.Request.Draft);
            if (draftJson.Length > 20000)
            {
                _logger.LogError($"Microsoft authorization draft is too large. OrganizationId: {request.OrganizationId}");
                throw new BadRequestCustomException("Draft is too large.", "The storage setup draft is too large to continue.");
            }

            // Only the hash of the state is stored; the state itself travels through Microsoft and back.
            string state = Base64Url(RandomNumberGenerator.GetBytes(32));
            string authorizationUrl;
            try
            {
                authorizationUrl = _graph.BuildAuthorizationUrl(state);
            }
            catch (MicrosoftGraphException exception)
            {
                _logger.LogError($"Microsoft authorization could not be started. Code: {exception.Code}");
                throw MicrosoftStorageWorkflow.ToCustomException(exception);
            }

            MicrosoftAuthorizationState authorizationState = new MicrosoftAuthorizationState
            {
                Id = Guid.NewGuid(),
                StateHash = Base64Url(SHA256.HashData(Encoding.UTF8.GetBytes(state))),
                UserId = request.UserId,
                OrganizationId = request.OrganizationId,
                ReturnUrl = MicrosoftReturnUrl.Normalize(request.Request.ReturnUrl),
                DraftJson = draftJson,
                ExpiresAt = DateTime.UtcNow.AddMinutes(10)
            };
            _repository.MicrosoftAuthorizationState.Create(authorizationState);
            await _repository.SaveAsync();

            _logger.LogInfo($"Microsoft authorization started. StateId: {authorizationState.Id}, OrganizationId: {request.OrganizationId}");
            return new MicrosoftConnectResponseDto
            {
                AuthorizationUrl = authorizationUrl,
                State = state,
                DraftId = authorizationState.Id
            };
        }

        private static string Base64Url(byte[] bytes)
        {
            return Convert.ToBase64String(bytes).TrimEnd('=').Replace('+', '-').Replace('/', '_');
        }

        // The draft of the setup screen is kept while the browser is at Microsoft; anything that
        // looks like a password, token or secret is dropped before it is stored.
        private static string SanitizeDraft(JsonElement draft)
        {
            if (draft.ValueKind is JsonValueKind.Undefined or JsonValueKind.Null)
            {
                return "{}";
            }

            using MemoryStream stream = new MemoryStream();
            using (Utf8JsonWriter writer = new Utf8JsonWriter(stream))
            {
                WriteSanitized(writer, draft);
            }

            return Encoding.UTF8.GetString(stream.ToArray());
        }

        private static void WriteSanitized(Utf8JsonWriter writer, JsonElement element)
        {
            switch (element.ValueKind)
            {
                case JsonValueKind.Object:
                    writer.WriteStartObject();
                    foreach (JsonProperty property in element.EnumerateObject())
                    {
                        if (property.Name.Contains("password", StringComparison.OrdinalIgnoreCase)
                            || property.Name.Contains("token", StringComparison.OrdinalIgnoreCase)
                            || property.Name.Contains("secret", StringComparison.OrdinalIgnoreCase))
                        {
                            continue;
                        }

                        writer.WritePropertyName(property.Name);
                        WriteSanitized(writer, property.Value);
                    }

                    writer.WriteEndObject();
                    break;
                case JsonValueKind.Array:
                    writer.WriteStartArray();
                    foreach (JsonElement item in element.EnumerateArray())
                    {
                        WriteSanitized(writer, item);
                    }

                    writer.WriteEndArray();
                    break;
                default:
                    element.WriteTo(writer);
                    break;
            }
        }
    }
}
