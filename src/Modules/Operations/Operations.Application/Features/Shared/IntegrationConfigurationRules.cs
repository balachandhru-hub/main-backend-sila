using System.Text.Json;
using System.Text.RegularExpressions;
using Microsoft.EntityFrameworkCore;
using Operations.Application.Services.Integration;
using Operations.Domain.Dtos;
using Operations.Domain.Entities;
using Operations.Domain.Enums;
using Operations.Infrastructure.Contracts.IRepository;
using SharedKernel.ExceptionHandler;
using SharedKernel.LoggerServices;

namespace Operations.Application.Features.Shared
{
    /// <summary>
    /// Validation and field mapping of an integration configuration, shared by the create and
    /// update commands, plus the lookup every integration use case starts with.
    /// </summary>
    internal static class IntegrationConfigurationRules
    {
        private static readonly Regex SafePath = new Regex(@"^[A-Za-z0-9_./$-]+$", RegexOptions.Compiled);
        private static readonly JsonSerializerOptions JsonOptions = new JsonSerializerOptions(JsonSerializerDefaults.Web);

        public static async Task<ApiIntegrationConfiguration> GetTrackedAsync(IRepositoryWrapper repository, ILoggerManager logger, Guid configurationId, Guid organizationId)
        {
            ApiIntegrationConfiguration? configuration = await repository.ApiIntegrationConfiguration.FindFirstByConditionAsync(
                x => x.Id == configurationId && x.OrganizationId == organizationId && x.IsActive);
            if (configuration == null)
            {
                logger.LogError($"Integration configuration not found. ConfigurationId: {configurationId}, OrganizationId: {organizationId}");
                throw new NotFoundCustomException("Integration not found.", "The integration configuration does not exist in your organization.");
            }

            return configuration;
        }

        public static async Task ApplyAsync(
            IRepositoryWrapper repository,
            ILoggerManager logger,
            IIntegrationCredentialProtector credentials,
            ApiIntegrationConfiguration configuration,
            IntegrationConfigurationInputDto input,
            CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(input.Name) || string.IsNullOrWhiteSpace(input.EntityCode))
            {
                logger.LogError($"Integration name or entity code is missing. OrganizationId: {configuration.OrganizationId}");
                throw new BadRequestCustomException("Name and entity code are required.", "Enter the name and the entity code of the integration.");
            }

            if (!string.IsNullOrWhiteSpace(input.ResourcePath) && !SafePath.IsMatch(input.ResourcePath))
            {
                logger.LogError($"Integration resource path is invalid. OrganizationId: {configuration.OrganizationId}");
                throw new BadRequestCustomException("Invalid resource path.", "The resource path contains unsupported characters.");
            }

            if (input.ProcessType is not (IntegrationProcessType.GET_PO or IntegrationProcessType.GET_SUPPLIER or IntegrationProcessType.POST_GRN))
            {
                logger.LogError($"Integration process is not enabled. ProcessType: {input.ProcessType}");
                throw new BadRequestCustomException("Process is not enabled.", "This integration process is not enabled in the current release.");
            }

            if (!Uri.TryCreate(input.BaseUrl, UriKind.Absolute, out Uri? baseUri) || (baseUri.Scheme != Uri.UriSchemeHttp && baseUri.Scheme != Uri.UriSchemeHttps))
            {
                logger.LogError($"Integration base URL is invalid. OrganizationId: {configuration.OrganizationId}");
                throw new BadRequestCustomException("Invalid base URL.", "Base URL must be an absolute HTTPS or HTTP URL.");
            }

            string entityCode = input.EntityCode.Trim();
            bool duplicate = await repository.ApiIntegrationConfiguration
                .FindByCondition(x => x.Id != configuration.Id && x.OrganizationId == configuration.OrganizationId
                    && x.EntityCode == entityCode && x.ProcessType == input.ProcessType)
                .AnyAsync(cancellationToken);
            if (duplicate)
            {
                logger.LogError($"Integration already exists. EntityCode: {entityCode}, ProcessType: {input.ProcessType}, OrganizationId: {configuration.OrganizationId}");
                throw new ConflictCustomException("Integration already exists.", "An integration already exists for this organization, entity, and process.");
            }

            await OperationsScope.EnsureUnitAsync(repository, logger, configuration.OrganizationId, input.OrganizationUnitId, cancellationToken);

            configuration.OrganizationUnitId = input.OrganizationUnitId;
            configuration.EntityCode = entityCode;
            configuration.Name = input.Name.Trim();
            configuration.ProcessType = input.ProcessType;
            configuration.Protocol = input.Protocol;
            configuration.BaseUrl = baseUri.ToString().TrimEnd('/');
            configuration.ResourcePath = input.ResourcePath?.Trim('/');
            configuration.AuthenticationType = input.AuthenticationType;
            configuration.Username = input.Username?.Trim();

            // A credential that is not sent again keeps its stored (encrypted) value.
            configuration.ProtectedPassword = credentials.Protect(input.Password) ?? configuration.ProtectedPassword;
            configuration.ProtectedClientId = credentials.Protect(input.ClientId) ?? configuration.ProtectedClientId;
            configuration.ProtectedClientSecret = credentials.Protect(input.ClientSecret) ?? configuration.ProtectedClientSecret;
            configuration.ProtectedBearerToken = credentials.Protect(input.BearerToken) ?? configuration.ProtectedBearerToken;
            configuration.TokenEndpoint = input.TokenEndpoint;
            configuration.TokenScope = input.TokenScope;
            configuration.TokenHeadersJson = input.TokenHeaders == null ? configuration.TokenHeadersJson : JsonSerializer.Serialize(input.TokenHeaders, JsonOptions);
            configuration.TokenBodyJson = input.TokenBody == null ? configuration.TokenBodyJson : JsonSerializer.Serialize(input.TokenBody, JsonOptions);
            configuration.TimeoutSeconds = input.TimeoutSeconds;
            configuration.RetryCount = input.RetryCount;
            configuration.PageSize = input.PageSize;
            configuration.WatermarkField = input.WatermarkField;
            configuration.ScheduleCron = input.ScheduleCron;
        }
    }
}
