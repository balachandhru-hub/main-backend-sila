using Buyer.Application.Features.Commands.CreateErpIntegration;
using Buyer.Domain.Dtos;
using Buyer.Domain.Entities;
using Buyer.Infrastructure.Contracts.IRepository;
using MediatR;
using SharedKernel.ExceptionHandler;
using SharedKernel.LoggerServices;

namespace Buyer.Application.Features.Commands.UpdateErpIntegration
{
    public class UpdateErpIntegrationCommandHandler : IRequestHandler<UpdateErpIntegrationCommand, Guid>
    {
        private readonly IRepositoryWrapper _repository;
        private readonly ILoggerManager _logger;

        public UpdateErpIntegrationCommandHandler(IRepositoryWrapper repository, ILoggerManager logger)
        {
            _repository = repository;
            _logger = logger;
        }

        public async Task<Guid> Handle(UpdateErpIntegrationCommand request, CancellationToken cancellationToken)
        {
            _logger.LogInfo(
                $"Updating ERP API configuration. ConfigurationId: {request.ConfigurationId}, OrganizationId: {request.OrganizationId}");

            CreateErpIntegrationCommandHandler.Validate(request.Request);
            BuyerBusinessProfile? buyer = _repository.BuyerBusinessProfile.FindFirstByCondition(
                x => x.OrganizationId == request.OrganizationId && x.IsActive);
            if (buyer == null)
            {
                _logger.LogError($"Buyer not found. OrganizationId: {request.OrganizationId}");
                throw new NotFoundCustomException("Buyer not found.", "The signed-in organization does not have a buyer profile.");
            }

            ErpIntegrationConfiguration? existing = await _repository.ErpIntegration.GetTrackedAsync(
                buyer.Id,
                request.ConfigurationId,
                cancellationToken);
            if (existing == null)
            {
                _logger.LogError($"ERP API configuration not found. ConfigurationId: {request.ConfigurationId}, BuyerId: {buyer.Id}");
                throw new NotFoundCustomException("API configuration not found.", "No API configuration exists for this id.");
            }

            ErpIntegrationConfiguration? duplicate = await _repository.ErpIntegration.FindForOperationAsync(
                buyer.Id,
                request.Request.Process,
                cancellationToken);
            if (duplicate != null && duplicate.Id != existing.Id)
            {
                throw new BadRequestCustomException(
                    "This function already has an API.",
                    $"{request.Request.Process.Trim().ToUpperInvariant()} can use only one system. Update that API instead of adding another.");
            }

            CreateErpIntegrationCommandHandler.Apply(existing, request.Request, keepSecrets: true);
            existing.Version += 1;
            existing.IsActive = request.Request.IsActive;
            await _repository.SaveAsync();

            _logger.LogInfo($"ERP API configuration updated. ConfigurationId: {existing.Id}, Version: {existing.Version}");
            return existing.Id;
        }
    }
}
