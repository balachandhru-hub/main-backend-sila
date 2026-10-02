using Buyer.Domain.Common;
using Buyer.Domain.Dtos;
using Buyer.Domain.Entities;
using Buyer.Infrastructure.Contracts.IRepository;
using MediatR;
using SharedKernel.ExceptionHandler;
using SharedKernel.LoggerServices;

namespace Buyer.Application.Features.Commands.CreateProperty
{
    public class CreatePropertyCommandHandler : IRequestHandler<CreatePropertyCommand, Guid>
    {
        private const int CodeMaximumLength = 50;

        private readonly IRepositoryWrapper _repository;
        private readonly ILoggerManager _logger;

        public CreatePropertyCommandHandler(IRepositoryWrapper repository, ILoggerManager logger)
        {
            _repository = repository;
            _logger = logger;
        }

        public async Task<Guid> Handle(CreatePropertyCommand request, CancellationToken cancellationToken)
        {
            _logger.LogInfo($"Creating property. OrganizationId: {request.OrganizationId}, UserId: {request.UserId}");

            ValidateFields(_logger, request.Request);

            BuyerBusinessProfile? buyer = _repository.BuyerBusinessProfile.FindFirstByCondition(
                x => x.OrganizationId == request.OrganizationId && x.IsActive);
            if (buyer == null)
            {
                _logger.LogError($"Buyer not found. OrganizationId: {request.OrganizationId}");
                throw new NotFoundCustomException("Buyer not found.", "The signed-in organization does not have a buyer profile.");
            }

            string plantCode = request.Request.PlantCode.Trim();
            BuyerProperty? duplicate = await _repository.BuyerProperty.FindFirstByConditionAsync(
                x => x.BuyerId == buyer.Id && x.PlantCode == plantCode);
            if (duplicate != null)
            {
                _logger.LogError($"Plant code already exists. PlantCode: {plantCode}, BuyerId: {buyer.Id}");
                throw new ConflictCustomException("Plant code already exists.", $"A property with the plant code {plantCode} already exists.");
            }

            await ValidateApprovalFlowAsync(_repository, _logger, request.Request.MasterApprovalFlowId, buyer.Id);

            BuyerProperty property = new BuyerProperty
            {
                Id = Guid.NewGuid(),
                BuyerId = buyer.Id,
                CompanyCode = request.Request.CompanyCode.Trim(),
                PlantCode = plantCode,
                PropertyName = request.Request.PropertyName.Trim(),
                MasterApprovalFlowId = request.Request.MasterApprovalFlowId
            };
            _repository.BuyerProperty.Create(property);
            await _repository.SaveAsync();

            _logger.LogInfo($"Property created. PropertyId: {property.Id}, PlantCode: {property.PlantCode}, BuyerId: {buyer.Id}");
            return property.Id;
        }

        internal static void ValidateFields(ILoggerManager logger, PropertyWriteDto property)
        {
            if (string.IsNullOrWhiteSpace(property.CompanyCode))
            {
                logger.LogError("Company code is missing.");
                throw new BadRequestCustomException("Company code is required.", "Enter the company code of the property.");
            }

            if (string.IsNullOrWhiteSpace(property.PlantCode))
            {
                logger.LogError("Plant code is missing.");
                throw new BadRequestCustomException("Plant code is required.", "Enter the plant code of the property.");
            }

            if (string.IsNullOrWhiteSpace(property.PropertyName))
            {
                logger.LogError("Property name is missing.");
                throw new BadRequestCustomException("Property name is required.", "Enter a property name.");
            }

            if (property.CompanyCode.Trim().Length > CodeMaximumLength || property.PlantCode.Trim().Length > CodeMaximumLength)
            {
                logger.LogError($"Company code or plant code is longer than {CodeMaximumLength} characters.");
                throw new BadRequestCustomException(
                    "Code is too long.",
                    $"The company code and the plant code can have {CodeMaximumLength} characters at most.");
            }
        }

        // A property's approval flow must be an active WEEKLY_BUCKET flow of the same buyer.
        internal static async Task ValidateApprovalFlowAsync(
            IRepositoryWrapper repository,
            ILoggerManager logger,
            Guid? masterApprovalFlowId,
            Guid buyerId)
        {
            if (masterApprovalFlowId == null || masterApprovalFlowId == Guid.Empty)
            {
                return;
            }

            MasterApprovalFlow? flow = await repository.MasterApprovalFlow.FindFirstByConditionAsync(
                x => x.Id == masterApprovalFlowId && x.BuyerId == buyerId && x.IsActive);
            if (flow == null)
            {
                logger.LogError($"Approval flow not found. ApprovalFlowId: {masterApprovalFlowId}, BuyerId: {buyerId}");
                throw new NotFoundCustomException("Approval flow not found.", "The approval flow does not belong to this buyer organization.");
            }

            if (!string.Equals(flow.Type, Common.WEEKLY_BUCKET_APPROVAL_TYPE, StringComparison.OrdinalIgnoreCase))
            {
                logger.LogError($"Approval flow type is not WEEKLY_BUCKET. ApprovalFlowId: {flow.Id}, Type: {flow.Type}");
                throw new BadRequestCustomException("Approval flow type is not Weekly Bucket.", "Select an approval configuration of type WEEKLY_BUCKET.");
            }
        }
    }
}
