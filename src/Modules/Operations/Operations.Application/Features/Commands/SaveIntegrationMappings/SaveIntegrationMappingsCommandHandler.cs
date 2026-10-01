using MediatR;
using Microsoft.EntityFrameworkCore;
using Operations.Application.Features.Shared;
using Operations.Application.Services.Integration;
using Operations.Domain.Common;
using Operations.Domain.Dtos;
using Operations.Domain.Entities;
using Operations.Domain.Enums;
using Operations.Infrastructure.Contracts.IRepository;
using SharedKernel.ExceptionHandler;
using SharedKernel.LoggerServices;

namespace Operations.Application.Features.Commands.SaveIntegrationMappings
{
    public class SaveIntegrationMappingsCommandHandler : IRequestHandler<SaveIntegrationMappingsCommand, List<IntegrationMappingResponseDto>>
    {
        private readonly IRepositoryWrapper _repository;
        private readonly ILoggerManager _logger;

        public SaveIntegrationMappingsCommandHandler(IRepositoryWrapper repository, ILoggerManager logger)
        {
            _repository = repository;
            _logger = logger;
        }

        public async Task<List<IntegrationMappingResponseDto>> Handle(SaveIntegrationMappingsCommand request, CancellationToken cancellationToken)
        {
            _logger.LogInfo($"Saving integration mappings. ConfigurationId: {request.ConfigurationId}, Count: {request.Mappings.Count}, OrganizationId: {request.OrganizationId}, UserId: {request.UserId}");

            ApiIntegrationConfiguration configuration = await IntegrationConfigurationRules.GetTrackedAsync(_repository, _logger, request.ConfigurationId, request.OrganizationId);
            List<FieldMappingInputDto> inputs = request.Mappings;
            bool invalid = inputs.Any(input => string.IsNullOrWhiteSpace(input.SourceField) || string.IsNullOrWhiteSpace(input.TargetField)
                || !IntegrationTargetFieldRegistry.Contains(input.TargetField.Trim())
                || input.SourceField.Trim().Contains(' ') || input.SourceField.Length > 250);
            if (invalid)
            {
                _logger.LogError($"Integration mapping is invalid. ConfigurationId: {configuration.Id}");
                throw new BadRequestCustomException("Invalid field mapping.", "Every mapping must use a registered target field and a valid source field.");
            }

            bool Has(string target) => inputs.Any(input => input.TargetField.Trim().Equals(target, StringComparison.OrdinalIgnoreCase));
            if (configuration.ProcessType == IntegrationProcessType.GET_PO
                && (!Has("PurchaseOrder.PoNumber") || !Has("PurchaseOrder.SupplierCode") || !Has("PurchaseOrder.SupplierName") || !Has("PurchaseOrder.Currency")))
            {
                _logger.LogError($"Required purchase order mappings are missing. ConfigurationId: {configuration.Id}");
                throw new BadRequestCustomException("Required mapping is missing.", "PO number, supplier code, supplier name and currency must be mapped.");
            }

            if (configuration.ProcessType == IntegrationProcessType.GET_SUPPLIER && (!Has("Supplier.SupplierCode") || !Has("Supplier.Name")))
            {
                _logger.LogError($"Required supplier mappings are missing. ConfigurationId: {configuration.Id}");
                throw new BadRequestCustomException("Required mapping is missing.", "Supplier code and supplier name mappings are required for supplier imports.");
            }

            if (inputs.GroupBy(input => input.TargetField.Trim(), StringComparer.OrdinalIgnoreCase).Any(group => group.Count() > 1))
            {
                _logger.LogError($"A target field is mapped twice. ConfigurationId: {configuration.Id}");
                throw new BadRequestCustomException("Invalid field mapping.", "A target field can be mapped only once.");
            }

            List<ApiFieldMapping> existing = await _repository.ApiFieldMapping.GetTrackedByConfigurationAsync(configuration.Id, cancellationToken);
            _repository.ApiFieldMapping.DeleteRange(existing);
            List<ApiFieldMapping> mappings = inputs.Select(input => new ApiFieldMapping
            {
                Id = Guid.NewGuid(),
                ConfigurationId = configuration.Id,
                SourceField = input.SourceField.Trim(),
                TargetField = input.TargetField.Trim(),

                // "NONE" (or anything unknown) means the value is taken as it is.
                Transformation = input.Transformation?.Trim().ToUpperInvariant() switch
                {
                    "TRIM" => "TRIM",
                    "UPPER" => "UPPER",
                    "LOWER" => "LOWER",
                    _ => null
                },
                NullPolicy = input.NullPolicy,
                DefaultValue = input.DefaultValue,
                IsValidated = true
            }).ToList();
            _repository.ApiFieldMapping.CreateRange(mappings);
            await _repository.SaveAsync();

            _logger.LogInfo($"Integration mappings saved. ConfigurationId: {configuration.Id}, Count: {mappings.Count}");
            return mappings.OrderBy(item => item.TargetField).Select(ResponseBuilder.Mapping).ToList();
        }
    }
}
