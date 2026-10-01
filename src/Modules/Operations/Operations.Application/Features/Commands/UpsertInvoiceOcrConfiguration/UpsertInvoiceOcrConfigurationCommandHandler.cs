using MediatR;
using Microsoft.EntityFrameworkCore;
using Operations.Application.Features.Shared;
using Operations.Domain.Common;
using Operations.Domain.Dtos;
using Operations.Domain.Entities;
using Operations.Domain.Enums;
using Operations.Infrastructure.Contracts.IRepository;
using SharedKernel.ExceptionHandler;
using SharedKernel.LoggerServices;

namespace Operations.Application.Features.Commands.UpsertInvoiceOcrConfiguration
{
    public class UpsertInvoiceOcrConfigurationCommandHandler : IRequestHandler<UpsertInvoiceOcrConfigurationCommand, InvoiceOcrConfigurationResponseDto>
    {
        private readonly IRepositoryWrapper _repository;
        private readonly ILoggerManager _logger;

        public UpsertInvoiceOcrConfigurationCommandHandler(IRepositoryWrapper repository, ILoggerManager logger)
        {
            _repository = repository;
            _logger = logger;
        }

        public async Task<InvoiceOcrConfigurationResponseDto> Handle(UpsertInvoiceOcrConfigurationCommand request, CancellationToken cancellationToken)
        {
            _logger.LogInfo($"Saving invoice OCR configuration. OrganizationId: {request.OrganizationId}, UserId: {request.UserId}");

            UpsertInvoiceOcrConfigurationRequestDto dto = request.Request;
            if (string.IsNullOrWhiteSpace(dto.BackendProvider))
            {
                _logger.LogError($"Backend provider is missing. OrganizationId: {request.OrganizationId}");
                throw new BadRequestCustomException("Backend provider is required.", "Enter the backend OCR provider.");
            }

            InvoiceOcrConfiguration? configuration = await _repository.InvoiceOcrConfiguration.FindFirstByConditionAsync(
                x => x.OrganizationId == request.OrganizationId);
            if (configuration == null)
            {
                configuration = new InvoiceOcrConfiguration
                {
                    Id = Guid.NewGuid(),
                    OrganizationId = request.OrganizationId,
                    Version = 0
                };
                _repository.InvoiceOcrConfiguration.Create(configuration);
            }

            configuration.MobileBasicOcrEnabled = dto.MobileBasicOcrEnabled;
            configuration.AutomaticBackendFallbackEnabled = dto.AutomaticBackendFallbackEnabled;
            configuration.MinimumMobileConfidence = dto.MinimumMobileConfidence;
            configuration.RequireSupplierName = dto.RequireSupplierName;
            configuration.RequireInvoiceNumber = dto.RequireInvoiceNumber;
            configuration.RequirePurchaseOrderNumber = dto.RequirePurchaseOrderNumber;
            configuration.RequireInvoiceAmount = dto.RequireInvoiceAmount;
            configuration.RequireInvoiceDate = dto.RequireInvoiceDate;
            configuration.RequireCurrency = dto.RequireCurrency;
            configuration.RequireSupplierTrn = dto.RequireSupplierTrn;
            configuration.BackendProvider = dto.BackendProvider.Trim().ToUpperInvariant();
            configuration.AlwaysBackendOnReread = dto.AlwaysBackendOnReread;
            configuration.DetailedLineExtractionEnabled = dto.DetailedLineExtractionEnabled;
            configuration.SupplierMasterValidationEnabled = dto.SupplierMasterValidationEnabled;
            configuration.PurchaseOrderValidationEnabled = dto.PurchaseOrderValidationEnabled;
            configuration.FinancialReconciliationEnabled = dto.FinancialReconciliationEnabled;
            configuration.AmountTolerance = dto.AmountTolerance;
            configuration.BackendTimeoutSeconds = dto.BackendTimeoutSeconds;
            configuration.BackendRetryCount = dto.BackendRetryCount;
            configuration.ReuseCachedOcr = dto.ReuseCachedOcr;
            configuration.Version++;
            AuditTrail.Add(_repository, request.OrganizationId, null, request.UserId, "INVOICE_OCR_CONFIGURATION_UPDATED", "InvoiceOcrConfiguration", configuration.Id, configuration.BackendProvider);
            await _repository.SaveAsync();

            _logger.LogInfo($"Invoice OCR configuration saved. OrganizationId: {request.OrganizationId}, Version: {configuration.Version}");
            return ResponseBuilder.OcrConfiguration(configuration);
        }
    }
}
