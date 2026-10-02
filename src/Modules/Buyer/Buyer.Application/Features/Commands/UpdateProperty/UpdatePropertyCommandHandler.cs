using Buyer.Application.Features.Commands.CreateProperty;
using Buyer.Domain.Entities;
using Buyer.Infrastructure.Contracts.IRepository;
using MediatR;
using SharedKernel.ExceptionHandler;
using SharedKernel.LoggerServices;

namespace Buyer.Application.Features.Commands.UpdateProperty
{
    public class UpdatePropertyCommandHandler : IRequestHandler<UpdatePropertyCommand, Unit>
    {
        private readonly IRepositoryWrapper _repository;
        private readonly ILoggerManager _logger;

        public UpdatePropertyCommandHandler(IRepositoryWrapper repository, ILoggerManager logger)
        {
            _repository = repository;
            _logger = logger;
        }

        public async Task<Unit> Handle(UpdatePropertyCommand request, CancellationToken cancellationToken)
        {
            _logger.LogInfo(
                $"Updating property. PropertyId: {request.PropertyId}, OrganizationId: {request.OrganizationId}, UserId: {request.UserId}");

            CreatePropertyCommandHandler.ValidateFields(_logger, request.Request);

            BuyerBusinessProfile? buyer = _repository.BuyerBusinessProfile.FindFirstByCondition(
                x => x.OrganizationId == request.OrganizationId && x.IsActive);
            if (buyer == null)
            {
                _logger.LogError($"Buyer not found. OrganizationId: {request.OrganizationId}");
                throw new NotFoundCustomException("Buyer not found.", "The signed-in organization does not have a buyer profile.");
            }

            BuyerProperty? property = await _repository.WeeklyBucket.GetPropertyAsync(request.PropertyId, buyer.Id, cancellationToken);
            if (property == null)
            {
                _logger.LogError($"Property not found. PropertyId: {request.PropertyId}, BuyerId: {buyer.Id}");
                throw new NotFoundCustomException("Property not found.", "No property exists for this buyer organization.");
            }

            string plantCode = request.Request.PlantCode.Trim();
            BuyerProperty? duplicate = await _repository.BuyerProperty.FindFirstByConditionAsync(
                x => x.BuyerId == buyer.Id && x.PlantCode == plantCode && x.Id != property.Id);
            if (duplicate != null)
            {
                _logger.LogError($"Plant code already exists. PlantCode: {plantCode}, BuyerId: {buyer.Id}");
                throw new ConflictCustomException("Plant code already exists.", $"A property with the plant code {plantCode} already exists.");
            }

            await CreatePropertyCommandHandler.ValidateApprovalFlowAsync(
                _repository, _logger, request.Request.MasterApprovalFlowId, buyer.Id);

            // A bucket keeps the plant and company code it was created with; only later buckets use the new codes.
            property.CompanyCode = request.Request.CompanyCode.Trim();
            property.PlantCode = plantCode;
            property.PropertyName = request.Request.PropertyName.Trim();
            property.MasterApprovalFlowId = request.Request.MasterApprovalFlowId;
            _repository.BuyerProperty.Update(property);
            await _repository.SaveAsync();

            _logger.LogInfo($"Property updated. PropertyId: {property.Id}, BuyerId: {buyer.Id}");
            return Unit.Value;
        }
    }
}
