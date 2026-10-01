using Buyer.Domain.Common;
using Buyer.Domain.Dtos;
using Buyer.Domain.Entities;
using Buyer.Infrastructure.Contracts.IRepository;
using MediatR;
using SharedKernel.ExceptionHandler;
using SharedKernel.LoggerServices;

namespace Buyer.Application.Features.Commands.SaveErpIntegration
{
    public class SaveErpIntegrationCommandHandler : IRequestHandler<SaveErpIntegrationCommand, Guid>
    {
        private static readonly HashSet<string> AuthTypes = new(StringComparer.OrdinalIgnoreCase)
        {
            Common.AUTH_NONE,
            Common.AUTH_BASIC,
            Common.AUTH_API_KEY,
            Common.AUTH_BEARER,
            Common.AUTH_OAUTH2_CLIENT_CREDENTIALS
        };

        private readonly IRepositoryWrapper _repository;
        private readonly ILoggerManager _logger;

        public SaveErpIntegrationCommandHandler(IRepositoryWrapper repository, ILoggerManager logger)
        {
            _repository = repository;
            _logger = logger;
        }

        public async Task<Guid> Handle(SaveErpIntegrationCommand request, CancellationToken cancellationToken)
        {
            _logger.LogInfo($"Saving ERP API configuration. OrganizationId: {request.OrganizationId}");

            ErpIntegrationWriteDto dto = request.Request;
            if (string.IsNullOrWhiteSpace(dto.ApiName)
                || string.IsNullOrWhiteSpace(dto.ErpType)
                || string.IsNullOrWhiteSpace(dto.BaseUrl)
                || string.IsNullOrWhiteSpace(dto.CreateDocumentPath)
                || string.IsNullOrWhiteSpace(dto.AuthType)
                || string.IsNullOrWhiteSpace(dto.DocumentType))
            {
                throw new BadRequestCustomException(
                    "API configuration is incomplete.",
                    "API name, system, document type, base URL, path, and authentication are required.");
            }

            if (!dto.BaseUrl.StartsWith("https://", StringComparison.OrdinalIgnoreCase)
                && !dto.BaseUrl.StartsWith("http://", StringComparison.OrdinalIgnoreCase))
            {
                throw new BadRequestCustomException("Base URL is invalid.", "Enter an absolute http or https base URL.");
            }

            if (!AuthTypes.Contains(dto.AuthType))
            {
                throw new BadRequestCustomException(
                    "Authentication type is not supported.",
                    "Use NONE, BASIC, API_KEY, BEARER, or OAUTH2_CLIENT_CREDENTIALS.");
            }

            if (dto.DocumentType != Common.ERP_DOCUMENT_PO && dto.DocumentType != Common.ERP_DOCUMENT_PR)
            {
                throw new BadRequestCustomException("Document type is invalid.", "Use PO or PR.");
            }

            BuyerBusinessProfile? buyer = _repository.BuyerBusinessProfile.FindFirstByCondition(
                x => x.OrganizationId == request.OrganizationId && x.IsActive);
            if (buyer == null)
            {
                _logger.LogError($"Buyer not found. OrganizationId: {request.OrganizationId}");
                throw new NotFoundCustomException("Buyer not found.", "The signed-in organization does not have a buyer profile.");
            }

            ErpIntegrationConfiguration? existing = dto.SupplierOrganizationId.HasValue
                ? await _repository.Wishlist.GetSupplierErpConfigurationAsync(buyer.Id, dto.SupplierOrganizationId.Value, cancellationToken)
                : await _repository.Wishlist.GetErpConfigurationAsync(buyer.Id, cancellationToken);
            if (existing == null)
            {
                ErpIntegrationConfiguration created = new ErpIntegrationConfiguration
                {
                    Id = Guid.NewGuid(),
                    BuyerOrganizationId = request.OrganizationId,
                    BuyerId = buyer.Id,
                    Version = 1
                };
                Apply(created, dto, keepSecrets: false);
                _repository.Wishlist.Add(created);
                await _repository.SaveAsync();
                _logger.LogInfo($"ERP API configuration created. ConfigurationId: {created.Id}, ErpType: {created.ErpType}");
                return created.Id;
            }

            Apply(existing, dto, keepSecrets: true);
            existing.Version += 1;
            existing.IsActive = dto.IsActive;
            await _repository.SaveAsync();
            _logger.LogInfo($"ERP API configuration updated. ConfigurationId: {existing.Id}, Version: {existing.Version}");
            return existing.Id;
        }

        private static void Apply(ErpIntegrationConfiguration target, ErpIntegrationWriteDto request, bool keepSecrets)
        {
            target.ApiName = request.ApiName.Trim();
            target.ErpType = request.ErpType.Trim();
            target.SupplierOrganizationId = request.SupplierOrganizationId;
            target.PayloadFormat = string.IsNullOrWhiteSpace(request.PayloadFormat) ? Common.PAYLOAD_JSON : request.PayloadFormat.Trim();
            target.RequestBody = request.RequestBody;
            target.DocumentType = request.DocumentType.Trim();
            target.BaseUrl = request.BaseUrl.Trim();
            target.CreateDocumentPath = request.CreateDocumentPath.Trim();
            target.HttpMethod = string.IsNullOrWhiteSpace(request.HttpMethod) ? "POST" : request.HttpMethod.Trim();
            target.AuthType = request.AuthType.Trim();
            target.TokenUrl = request.TokenUrl;
            target.Username = request.Username;
            target.ClientId = request.ClientId;
            target.Scope = request.Scope;
            target.ApiKeyHeader = request.ApiKeyHeader;
            target.HeadersJson = request.HeadersJson;
            target.TimeoutSeconds = request.TimeoutSeconds <= 0 ? 60 : request.TimeoutSeconds;
            target.MaxRetryCount = request.MaxRetryCount < 0 ? 0 : request.MaxRetryCount;
            target.IsActive = request.IsActive;
            target.Password = Secret(request.Password, target.Password, keepSecrets);
            target.ClientSecret = Secret(request.ClientSecret, target.ClientSecret, keepSecrets);
            target.ApiKey = Secret(request.ApiKey, target.ApiKey, keepSecrets);
            target.AccessToken = Secret(request.AccessToken, target.AccessToken, keepSecrets);
        }

        private static string? Secret(string? incoming, string? current, bool keepSecrets)
        {
            if (!string.IsNullOrWhiteSpace(incoming))
            {
                return incoming;
            }

            return keepSecrets ? current : incoming;
        }
    }
}
