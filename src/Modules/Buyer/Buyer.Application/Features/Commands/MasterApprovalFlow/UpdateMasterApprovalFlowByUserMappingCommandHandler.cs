using Buyer.Infrastructure.Contracts.IRepository;
using MediatR;
using SharedKernel.ExceptionHandler;
using SharedKernel.LoggerServices;

namespace Buyer.Application.Features.Commands.MasterApprovalFlows
{
    public class UpdateMasterApprovalFlowByUserMappingCommandHandler
        : IRequestHandler<UpdateMasterApprovalFlowByUserMappingCommand, Guid>
    {
        private readonly IRepositoryWrapper _repositoryWrapper;
        private readonly ILoggerManager _logger;

        public UpdateMasterApprovalFlowByUserMappingCommandHandler(
            IRepositoryWrapper repository,
            ILoggerManager logger)
        {
            _repositoryWrapper = repository;
            _logger = logger;
        }

        public async Task<Guid> Handle(
            UpdateMasterApprovalFlowByUserMappingCommand request,
            CancellationToken cancellationToken)
        {
            _logger.LogInfo(
                $"Updating Master Approval Flow via ApprovalFlowUserMappingId: {request.ApprovalFlowUserMappingId}");

            var userMapping = await _repositoryWrapper.ApprovalFlowUserMapping
                .FindFirstByConditionAsync(x =>
                    x.Id == request.ApprovalFlowUserMappingId &&
                    x.IsActive);

            if (userMapping == null)
            {
                _logger.LogError(
                    $"Approval flow user mapping not found for Id: {request.ApprovalFlowUserMappingId}");
                throw new NotFoundCustomException(
                    "Approval flow user mapping not found.",
                    $"No approval flow user mapping exists with Id: {request.ApprovalFlowUserMappingId}.");
            }

            var approvalFlow = await _repositoryWrapper.MasterApprovalFlow
                .FindFirstByConditionAsync(x =>
                    x.Id == userMapping.ApprovalFlowId &&
                    x.IsActive);

            if (approvalFlow == null)
            {
                _logger.LogError(
                    $"Master approval flow not found for Id: {userMapping.ApprovalFlowId}");
                throw new NotFoundCustomException(
                    "Master approval flow not found.",
                    $"No master approval flow exists with Id: {userMapping.ApprovalFlowId}.");
            }

          

            if (request.ApprovalFlow.ApprovalName != null)
            {
                _logger.LogInfo(
                    $"Updating ApprovalName for Master Approval Flow Id: {approvalFlow.Id}");
                approvalFlow.ApprovalName = request.ApprovalFlow.ApprovalName;
            }
            if (request.ApprovalFlow.Order != 0)
            {
                _logger.LogInfo(
                    $"Updating Order for Approval Flow User Mapping Id: {userMapping.Id}");
                userMapping.Order = request.ApprovalFlow.Order;

                _repositoryWrapper.ApprovalFlowUserMapping.Update(userMapping);
            }

            _repositoryWrapper.MasterApprovalFlow.Update(approvalFlow);

            await _repositoryWrapper.SaveAsync();

            _logger.LogInfo(
                $"Master Approval Flow updated successfully: {approvalFlow.Id}");

            return approvalFlow.Id;
        }
    }
}
