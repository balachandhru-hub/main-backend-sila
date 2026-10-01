using Buyer.Domain.Dtos;
using Buyer.Domain.Entities;
using Buyer.Infrastructure.Contracts.IRepository;
using MediatR;
using SharedKernel.ExceptionHandler;
using SharedKernel.LoggerServices;

namespace Buyer.Application.Features.Queries.GetErpIntegration
{
    public class GetErpIntegrationQueryHandler : IRequestHandler<GetErpIntegrationQuery, List<ErpIntegrationResponseDto>>
    {
        private readonly IRepositoryWrapper _repository;
        private readonly ILoggerManager _logger;

        public GetErpIntegrationQueryHandler(IRepositoryWrapper repository, ILoggerManager logger)
        {
            _repository = repository;
            _logger = logger;
        }

        public async Task<List<ErpIntegrationResponseDto>> Handle(GetErpIntegrationQuery request, CancellationToken cancellationToken)
        {
            _logger.LogInfo($"Fetching ERP API configurations. OrganizationId: {request.OrganizationId}");

            BuyerBusinessProfile? buyer = _repository.BuyerBusinessProfile.FindFirstByCondition(
                x => x.OrganizationId == request.OrganizationId && x.IsActive);
            if (buyer == null)
            {
                _logger.LogError($"Buyer not found. OrganizationId: {request.OrganizationId}");
                throw new NotFoundCustomException("Buyer not found.", "The signed-in organization does not have a buyer profile.");
            }

            List<ErpIntegrationConfiguration> rows = await _repository.ErpIntegration.ListAsync(buyer.Id, cancellationToken);
            _logger.LogInfo($"ERP API configurations fetched. Count: {rows.Count}, BuyerId: {buyer.Id}");
            return rows.Select(Map).ToList();
        }

        private static ErpIntegrationResponseDto Map(ErpIntegrationConfiguration configuration)
        {
            return new ErpIntegrationResponseDto
            {
                Id = configuration.Id,
                ApiName = configuration.ApiName,
                Process = configuration.Process,
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
