using MediatR;
using SharedKernel.LoggerServices;
using Supplier.Domain.Dto;
using Supplier.Domain.Entities;
using Supplier.Infrastructure.Contracts.IRepository;

namespace Supplier.Application.Features.Queries.GetSupplierErpConfiguration
{
    public class GetSupplierErpConfigurationQueryHandler : IRequestHandler<GetSupplierErpConfigurationQuery, SupplierErpResponseDto?>
    {
        private readonly IRepositoryWrapper _repository;
        private readonly ILoggerManager _logger;

        public GetSupplierErpConfigurationQueryHandler(IRepositoryWrapper repository, ILoggerManager logger)
        {
            _repository = repository;
            _logger = logger;
        }

        public async Task<SupplierErpResponseDto?> Handle(GetSupplierErpConfigurationQuery request, CancellationToken cancellationToken)
        {
            _logger.LogInfo($"Fetching supplier ERP API configuration. SupplierOrganizationId: {request.SupplierOrganizationId}");

            SupplierErpIntegrationConfiguration? configuration = await _repository.SupplierErp.GetByOrganizationAsync(request.SupplierOrganizationId, cancellationToken);
            if (configuration == null)
            {
                _logger.LogInfo($"No supplier ERP API configuration stored. SupplierOrganizationId: {request.SupplierOrganizationId}");
                return null;
            }

            return new SupplierErpResponseDto
            {
                Id = configuration.Id,
                ErpType = configuration.ErpType,
                PayloadFormat = configuration.PayloadFormat,
                BaseUrl = configuration.BaseUrl,
                AuthPath = configuration.AuthPath,
                OrderPath = configuration.OrderPath,
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
                DefaultShipTo = configuration.DefaultShipTo,
                OrderDateFormat = configuration.OrderDateFormat,
                HeadersJson = configuration.HeadersJson,
                TimeoutSeconds = configuration.TimeoutSeconds,
                MaxRetryCount = configuration.MaxRetryCount,
                Version = configuration.Version,
                IsActive = configuration.IsActive
            };
        }
    }
}
