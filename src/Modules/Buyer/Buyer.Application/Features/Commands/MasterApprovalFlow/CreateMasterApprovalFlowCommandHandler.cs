using Buyer.Domain.Entities;
using Buyer.Infrastructure.Contracts.IRepository;
using MediatR;
using SharedKernel.ExceptionHandler;
using SharedKernel.LoggerServices;

namespace Buyer.Application.Features.Commands.MasterApprovalFlows
{
    public class CreateMasterApprovalFlowCommandHandler
        : IRequestHandler<CreateMasterApprovalFlowCommand, Guid>
    {
        private readonly IRepositoryWrapper _repository;
        private readonly ILoggerManager _logger;

        public CreateMasterApprovalFlowCommandHandler(
            IRepositoryWrapper repository,
            ILoggerManager logger)
        {
            _repository = repository;
            _logger = logger;
        }

        public async Task<Guid> Handle(
            CreateMasterApprovalFlowCommand request,
            CancellationToken cancellationToken)
        {
            _logger.LogInfo(
                $"Creating Master Approval Flow. " +
                $"OrganizationId: {request.OrganizationId}");

            var dto = request.ApprovalFlow;

            // Get Buyer only through OrganizationId
            var buyer =
                await _repository.BuyerBusinessProfile
                    .FindFirstByConditionAsync(x =>
                        x.OrganizationId == request.OrganizationId &&
                        x.IsActive);

            if (buyer == null)
            {
                throw new NotFoundCustomException(
                    "Buyer not found.",
                    "Buyer profile not found for the organization.");
            }

            // At least one approval user is required
            if (dto.Users == null || !dto.Users.Any())
            {
                throw new PreConditionFailedCustomException(
                    "Approval flow users required.",
                    "At least one approval flow user is required.");
            }

            // Check duplicate approval code for this buyer
            var existingApprovalFlow =
                await _repository.MasterApprovalFlow
                    .FindFirstByConditionAsync(x =>
                        x.ApprovalCode == dto.ApprovalCode &&
                        x.BuyerId == buyer.Id &&
                        x.IsActive);

            if (existingApprovalFlow != null)
            {
                throw new PreConditionFailedCustomException(
                    "Approval code already exists.",
                    $"Approval code '{dto.ApprovalCode}' already exists for this buyer.");
            }

            // Create Master Approval Flow
            var approvalFlow = new MasterApprovalFlow
            {
                Id = Guid.NewGuid(),
                ApprovalCode = dto.ApprovalCode,
                ApprovalName = dto.ApprovalName,
                BuyerId = buyer.Id,
                Type = dto.Type,
                TotalAmount = dto.TotalAmount,
                Currency = dto.Currency
            };

            await _repository.MasterApprovalFlow
                .CreateAsync(approvalFlow);

            // Create Approval Flow User Mappings
            var approvalFlowUserMappings =
                dto.Users
                    .Select(user =>
                        new ApprovalFlowUserMapping
                        {
                            Id = Guid.NewGuid(),
                            ApprovalFlowId = approvalFlow.Id,
                            UserId = user.UserId,
                            Order = user.Order
                        })
                    .ToList();

            if (approvalFlowUserMappings.Any())
            {
                await _repository.ApprovalFlowUserMapping
                    .CreateRangeAsync(approvalFlowUserMappings);
            }

            // Save everything
            await _repository.SaveAsync();

            _logger.LogInfo(
                $"Master Approval Flow created successfully. " +
                $"ApprovalFlowId: {approvalFlow.Id}, " +
                $"BuyerId: {buyer.Id}");

            return approvalFlow.Id;
        }
    }
}