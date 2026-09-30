using SharedKernel.ExceptionHandler;
using Supplier.Domain.Dto;
using Supplier.Domain.Entities;
using Supplier.Infrastructure.Contracts.IRepository;

namespace Supplier.Application.Services
{
    public interface ISupplierPurchaseOrderService
    {
        Task<SupplierErpResponseDto?> GetConfigurationAsync(Guid supplierOrganizationId, CancellationToken cancellationToken);
        Task<Guid> SaveConfigurationAsync(Guid supplierOrganizationId, SupplierErpWriteDto request, CancellationToken cancellationToken);
    }

    /// <summary>
    /// Stores this supplier's ERP endpoint. The buyer job calls that URL directly.
    /// There is no shared purchase-order path, because each supplier ERP is different.
    /// </summary>
    public class SupplierPurchaseOrderService : ISupplierPurchaseOrderService
    {
        private readonly IRepositoryWrapper _repository;

        public SupplierPurchaseOrderService(IRepositoryWrapper repository)
        {
            _repository = repository;
        }

        public async Task<SupplierErpResponseDto?> GetConfigurationAsync(Guid supplierOrganizationId, CancellationToken cancellationToken)
        {
            SupplierErpIntegrationConfiguration? configuration = await _repository.SupplierErp.GetByOrganizationAsync(supplierOrganizationId, cancellationToken);
            if (configuration == null)
            {
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

        public async Task<Guid> SaveConfigurationAsync(Guid supplierOrganizationId, SupplierErpWriteDto request, CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(request.ErpType) || string.IsNullOrWhiteSpace(request.BaseUrl) || string.IsNullOrWhiteSpace(request.OrderPath) || string.IsNullOrWhiteSpace(request.AuthType))
            {
                throw new BadRequestCustomException("Supplier ERP configuration is incomplete.", "ERP type, base URL, order path, and authentication type are required.");
            }

            if (!request.BaseUrl.StartsWith("http://", StringComparison.OrdinalIgnoreCase) && !request.BaseUrl.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
            {
                throw new BadRequestCustomException("Base URL is invalid.", "Enter an absolute http or https base URL.");
            }

            SupplierErpIntegrationConfiguration? existing = await _repository.SupplierErp.GetByOrganizationAsync(supplierOrganizationId, cancellationToken);
            if (existing == null)
            {
                existing = new SupplierErpIntegrationConfiguration
                {
                    Id = Guid.NewGuid(),
                    SupplierOrganizationId = supplierOrganizationId,
                    Version = 1
                };
                Apply(existing, request, false);
                _repository.SupplierErp.Create(existing);
            }
            else
            {
                Apply(existing, request, true);
                existing.Version += 1;
            }

            await _repository.SaveAsync();
            return existing.Id;
        }

        private static void Apply(SupplierErpIntegrationConfiguration target, SupplierErpWriteDto request, bool keepSecrets)
        {
            target.ErpType = request.ErpType.Trim();
            target.PayloadFormat = string.IsNullOrWhiteSpace(request.PayloadFormat) ? "JSON" : request.PayloadFormat.Trim();
            target.BaseUrl = request.BaseUrl.Trim();
            target.AuthPath = request.AuthPath;
            target.OrderPath = request.OrderPath.Trim();
            target.HttpMethod = string.IsNullOrWhiteSpace(request.HttpMethod) ? "POST" : request.HttpMethod.Trim();
            target.AuthType = request.AuthType.Trim();
            target.TokenUrl = request.TokenUrl;
            target.Username = request.Username;
            target.ClientId = request.ClientId;
            target.Scope = request.Scope;
            target.ApiKeyHeader = request.ApiKeyHeader;
            target.DefaultShipTo = request.DefaultShipTo;
            target.OrderDateFormat = string.IsNullOrWhiteSpace(request.OrderDateFormat) ? "dd/MM/yyyy" : request.OrderDateFormat;
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
