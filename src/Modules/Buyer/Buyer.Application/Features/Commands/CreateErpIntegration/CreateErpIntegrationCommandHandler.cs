using Buyer.Domain.Common;
using Buyer.Domain.Dtos;
using Buyer.Domain.Entities;
using Buyer.Infrastructure.Contracts.IRepository;
using MediatR;
using SharedKernel.ExceptionHandler;
using SharedKernel.LoggerServices;

namespace Buyer.Application.Features.Commands.CreateErpIntegration
{
    public class CreateErpIntegrationCommandHandler : IRequestHandler<CreateErpIntegrationCommand, Guid>
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

        public CreateErpIntegrationCommandHandler(IRepositoryWrapper repository, ILoggerManager logger)
        {
            _repository = repository;
            _logger = logger;
        }

        public async Task<Guid> Handle(CreateErpIntegrationCommand request, CancellationToken cancellationToken)
        {
            _logger.LogInfo($"Creating ERP API configuration. OrganizationId: {request.OrganizationId}");
            ErpIntegrationWriteDto dto = request.Request;
            Validate(dto);

            BuyerBusinessProfile buyer = GetBuyer(request.OrganizationId);
            string operation = dto.Process.Trim().ToUpperInvariant();
            ErpIntegrationConfiguration? duplicate = await _repository.ErpIntegration.FindForOperationAsync(buyer.Id, operation, cancellationToken);
            if (duplicate != null)
            {
                throw new BadRequestCustomException(
                    "This function already has an API.",
                    $"{operation} can use only one system. Update the existing API instead of adding another.");
            }

            ErpIntegrationConfiguration created = new ErpIntegrationConfiguration
            {
                Id = Guid.NewGuid(),
                BuyerOrganizationId = request.OrganizationId,
                BuyerId = buyer.Id,
                Version = 1
            };
            Apply(created, dto, keepSecrets: false);
            _repository.ErpIntegration.Create(created);
            await _repository.SaveAsync();

            _logger.LogInfo($"ERP API configuration created. ConfigurationId: {created.Id}, Process: {created.Process}, ErpType: {created.ErpType}");
            return created.Id;
        }

        internal static void Validate(ErpIntegrationWriteDto dto)
        {
            if (string.IsNullOrWhiteSpace(dto.ApiName)
                || string.IsNullOrWhiteSpace(dto.Process)
                || string.IsNullOrWhiteSpace(dto.ErpType)
                || string.IsNullOrWhiteSpace(dto.BaseUrl)
                || string.IsNullOrWhiteSpace(dto.CreateDocumentPath)
                || string.IsNullOrWhiteSpace(dto.AuthType))
            {
                throw new BadRequestCustomException(
                    "API configuration is incomplete.",
                    "API name, API type, system, base URL, path, and authentication are required.");
            }

            string payloadFormat = string.IsNullOrWhiteSpace(dto.PayloadFormat) ? Common.PAYLOAD_JSON : dto.PayloadFormat.Trim();
            if (!string.Equals(payloadFormat, Common.PAYLOAD_JSON, StringComparison.OrdinalIgnoreCase)
                && !string.Equals(payloadFormat, Common.PAYLOAD_SOAP, StringComparison.OrdinalIgnoreCase)
                && !string.Equals(payloadFormat, Common.PAYLOAD_CXML, StringComparison.OrdinalIgnoreCase))
            {
                throw new BadRequestCustomException("Body format is invalid.", "Use JSON, SOAP, or CXML.");
            }

            if (!string.Equals(payloadFormat, Common.PAYLOAD_JSON, StringComparison.OrdinalIgnoreCase)
                && string.IsNullOrWhiteSpace(dto.RequestBody))
            {
                throw new BadRequestCustomException(
                    "Request body is required.",
                    "SOAP and cXML calls send the body configured here. Paste the envelope or cXML for this API.");
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

            // Only the purchase order API creates a PO or PR; the other API types have no document type.
            bool isPurchaseOrder = string.Equals(dto.Process.Trim(), Common.ERP_OPERATION_PO_CREATE, StringComparison.OrdinalIgnoreCase);
            if (isPurchaseOrder && dto.DocumentType != Common.ERP_DOCUMENT_PO && dto.DocumentType != Common.ERP_DOCUMENT_PR)
            {
                throw new BadRequestCustomException("Document type is invalid.", "Use PO or PR.");
            }
        }

        internal static void Apply(ErpIntegrationConfiguration target, ErpIntegrationWriteDto request, bool keepSecrets)
        {
            target.ApiName = request.ApiName.Trim();
            target.Process = request.Process.Trim().ToUpperInvariant();
            target.ErpType = request.ErpType.Trim();
            target.SupplierOrganizationId = request.SupplierOrganizationId;
            target.PayloadFormat = string.IsNullOrWhiteSpace(request.PayloadFormat) ? Common.PAYLOAD_JSON : request.PayloadFormat.Trim();
            target.RequestBody = request.RequestBody;
            target.DocumentType = request.DocumentType?.Trim() ?? string.Empty;
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

        private BuyerBusinessProfile GetBuyer(Guid organizationId)
        {
            BuyerBusinessProfile? buyer = _repository.BuyerBusinessProfile.FindFirstByCondition(
                x => x.OrganizationId == organizationId && x.IsActive);
            if (buyer == null)
            {
                _logger.LogError($"Buyer not found. OrganizationId: {organizationId}");
                throw new NotFoundCustomException("Buyer not found.", "The signed-in organization does not have a buyer profile.");
            }

            return buyer;
        }
    }
}
