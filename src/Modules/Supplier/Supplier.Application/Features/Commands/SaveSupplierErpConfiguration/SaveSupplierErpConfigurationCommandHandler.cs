using MediatR;
using SharedKernel.ExceptionHandler;
using SharedKernel.LoggerServices;
using Supplier.Domain.Dto;
using Supplier.Domain.Entities;
using Supplier.Infrastructure.Contracts.IRepository;

namespace Supplier.Application.Features.Commands.SaveSupplierErpConfiguration
{
    public class SaveSupplierErpConfigurationCommandHandler : IRequestHandler<SaveSupplierErpConfigurationCommand, Guid>
    {
        private readonly IRepositoryWrapper _repository;
        private readonly ILoggerManager _logger;

        public SaveSupplierErpConfigurationCommandHandler(IRepositoryWrapper repository, ILoggerManager logger)
        {
            _repository = repository;
            _logger = logger;
        }

        public async Task<Guid> Handle(SaveSupplierErpConfigurationCommand request, CancellationToken cancellationToken)
        {
            _logger.LogInfo($"Saving supplier ERP API configuration. SupplierOrganizationId: {request.SupplierOrganizationId}");

            SupplierErpWriteDto dto = request.Request;
            if (string.IsNullOrWhiteSpace(dto.ErpType) || string.IsNullOrWhiteSpace(dto.BaseUrl) || string.IsNullOrWhiteSpace(dto.OrderPath) || string.IsNullOrWhiteSpace(dto.AuthType))
            {
                throw new BadRequestCustomException("Supplier ERP configuration is incomplete.", "System, base URL, path, and authentication are required.");
            }

            string payloadFormat = string.IsNullOrWhiteSpace(dto.PayloadFormat) ? "JSON" : dto.PayloadFormat.Trim();
            if (!payloadFormat.Equals("JSON", StringComparison.OrdinalIgnoreCase)
                && !payloadFormat.Equals("SOAP", StringComparison.OrdinalIgnoreCase)
                && !payloadFormat.Equals("CXML", StringComparison.OrdinalIgnoreCase))
            {
                throw new BadRequestCustomException("Body format is invalid.", "Use JSON, SOAP, or CXML.");
            }

            if (!payloadFormat.Equals("JSON", StringComparison.OrdinalIgnoreCase) && string.IsNullOrWhiteSpace(dto.RequestBody))
            {
                throw new BadRequestCustomException("Request body is required.", "SOAP and cXML calls send the body saved on this API.");
            }

            if (!dto.BaseUrl.StartsWith("http://", StringComparison.OrdinalIgnoreCase) && !dto.BaseUrl.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
            {
                throw new BadRequestCustomException("Base URL is invalid.", "Enter an absolute http or https base URL.");
            }

            SupplierErpIntegrationConfiguration? existing = await _repository.SupplierErp.GetByOrganizationAsync(request.SupplierOrganizationId, cancellationToken);
            if (existing == null)
            {
                existing = new SupplierErpIntegrationConfiguration
                {
                    Id = Guid.NewGuid(),
                    SupplierOrganizationId = request.SupplierOrganizationId,
                    Version = 1
                };
                Apply(existing, dto, false);
                _repository.SupplierErp.Create(existing);
            }
            else
            {
                Apply(existing, dto, true);
                existing.Version += 1;
            }

            await _repository.SaveAsync();
            _logger.LogInfo($"Supplier ERP API configuration saved. ConfigurationId: {existing.Id}, ErpType: {existing.ErpType}");
            return existing.Id;
        }

        private static void Apply(SupplierErpIntegrationConfiguration target, SupplierErpWriteDto request, bool keepSecrets)
        {
            target.Process = string.IsNullOrWhiteSpace(request.Process) ? "PO_CREATE" : request.Process.Trim().ToUpperInvariant();
            target.ErpType = request.ErpType.Trim();
            target.PayloadFormat = string.IsNullOrWhiteSpace(request.PayloadFormat) ? "JSON" : request.PayloadFormat.Trim();
            target.RequestBody = request.RequestBody;
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
