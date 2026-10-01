using Buyer.Domain.Dtos;
using Buyer.Domain.Entities;
using Buyer.Infrastructure.Contracts.IRepository;
using MediatR;
using SharedKernel.ExceptionHandler;
using SharedKernel.LoggerServices;

namespace Buyer.Application.Features.Queries.GetErpIntegration
{
    public class GetErpIntegrationQueryHandler : IRequestHandler<GetErpIntegrationQuery, ErpIntegrationResponseDto?>
    {
        private readonly IRepositoryWrapper _repository;
        private readonly ILoggerManager _logger;

        public GetErpIntegrationQueryHandler(IRepositoryWrapper repository, ILoggerManager logger)
        {
            _repository = repository;
            _logger = logger;
        }

        public async Task<ErpIntegrationResponseDto?> Handle(GetErpIntegrationQuery request, CancellationToken cancellationToken)
        {
            _logger.LogInfo($"Fetching ERP API configuration. OrganizationId: {request.OrganizationId}");

            BuyerBusinessProfile? buyer = _repository.BuyerBusinessProfile.FindFirstByCondition(
                x => x.OrganizationId == request.OrganizationId && x.IsActive);
            if (buyer == null)
            {
                _logger.LogError($"Buyer not found. OrganizationId: {request.OrganizationId}");
                throw new NotFoundCustomException("Buyer not found.", "The signed-in organization does not have a buyer profile.");
            }

            ErpIntegrationConfiguration? configuration = await _repository.Wishlist.GetErpConfigurationAsync(buyer.Id, cancellationToken);
            if (configuration == null)
            {
                _logger.LogInfo($"No ERP API configuration stored. BuyerId: {buyer.Id}");
                return null;
            }

            return new ErpIntegrationResponseDto
            {
                Id = configuration.Id,
                ApiName = configuration.ApiName,
                ErpType = configuration.ErpType,
                SupplierOrganizationId = configuration.SupplierOrganizationId,
                PayloadFormat = configuration.PayloadFormat,
                RequestBody = configuration.RequestBody,
                DocumentType = configuration.DocumentType,
                BaseUrl = configuration.BaseUrl,
                CreateDocumentPath = configuration.CreateDocumentPath,
                HttpMethod = configuration.HttpMethod,
                AuthType = configuration.AuthType,
                TokenUrl = configuration.TokenUrl,
                Username = configuration.Username,
                HasPassword = !string.IsNullOrWhiteSpace(configuration.Password),
                ClientId = configuration.ClientId,
                HasClientSecret = !string.IsNullOrWhiteSpace(configuration.ClientSecret),
                Scope = configuration.Scope,
                ApiKeyHeader = configuration.ApiKeyHeader,
                HasApiKey = !string.IsNullOrWhiteSpace(configuration.ApiKey),
                HasAccessToken = !string.IsNullOrWhiteSpace(configuration.AccessToken),
                HeadersJson = configuration.HeadersJson,
                TimeoutSeconds = configuration.TimeoutSeconds,
                MaxRetryCount = configuration.MaxRetryCount,
                Version = configuration.Version,
                IsActive = configuration.IsActive
            };
        }
    }
}
