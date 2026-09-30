using Buyer.Domain.Common;
using Buyer.Infrastructure.Contracts.IRepository;
using MediatR;
using Microsoft.EntityFrameworkCore;
using SharedKernel.ExceptionHandler;
using SharedKernel.LoggerServices;

namespace Buyer.Application.Features.Commands.ApproveRejectPredefinedContract
{
    public class ApproveRejectPredefinedContractCommandHandler
        : IRequestHandler<ApproveRejectPredefinedContractCommand, Guid>
    {
        private readonly IRepositoryWrapper _repositoryWrapper;
        private readonly ILoggerManager _logger;

        public ApproveRejectPredefinedContractCommandHandler(
            IRepositoryWrapper repository,
            ILoggerManager logger)
        {
            _repositoryWrapper = repository;
            _logger = logger;
        }

        public async Task<Guid> Handle(
            ApproveRejectPredefinedContractCommand request,
            CancellationToken cancellationToken)
        {
            _logger.LogInfo(
                $"Processing contract approval. " +
                $"ContractId: {request.ContractId}, " +
                $"UserId: {request.UserId}");

            // ---------------------------------------------------------
            // 1. Validate Status
            // ---------------------------------------------------------

            if (string.IsNullOrWhiteSpace(request.Approval.Status))
            {
                _logger.LogError(
                    $"Approval status missing. " +
                    $"ContractId: {request.ContractId}, " +
                    $"UserId: {request.UserId}");
                throw new BadRequestCustomException(
                    "Approval status is required.",
                    "Please provide APPROVE or REJECT.");
            }

            if (request.Approval.Status != Common.APPROVED &&
                request.Approval.Status != Common.REJECTED)
            {
                _logger.LogError(
                    $"Invalid approval status. " +
                    $"ContractId: {request.ContractId}, " +
                    $"UserId: {request.UserId}, " +
                    $"Status: {request.Approval.Status}");
                throw new BadRequestCustomException(
                    "Invalid approval status.",
                    "Status must be APPROVE or REJECT.");
            }

            // ---------------------------------------------------------
            // 2. Get Contract
            // ---------------------------------------------------------

            var contract =
                await _repositoryWrapper.PredefinedContract
                    .FindFirstByConditionAsync(x =>
                        x.Id == request.ContractId &&
                        x.IsActive);

            if (contract == null)
            {
                _logger.LogError(
                    $"Contract not found. ContractId: {request.ContractId}");
                throw new NotFoundCustomException(
                    "Contract not found.",
                    $"No contract was found with ContractId: {request.ContractId}");
            }

            // ---------------------------------------------------------
            // 3. Get Contract Approval Flow
            // ---------------------------------------------------------

            var contractApprovalFlow =
                await _repositoryWrapper.PredefinedContractApprovalFlow
                    .FindFirstByConditionAsync(x =>
                        x.ContractId == contract.Id &&
                        x.IsActive);

            if (contractApprovalFlow == null)
            {
                _logger.LogError(
                    $"Approval flow not found for contract. " +
                    $"ContractId: {request.ContractId}");
                throw new NotFoundCustomException(
                    "Approval flow not found.",
                    "No approval flow is configured for this contract.");
            }

            // ---------------------------------------------------------
            // 4. Get Contract Approval Users
            // ---------------------------------------------------------

            var approvalUsers =
                await _repositoryWrapper.PredefinedContractApprovalUserMapping
                    .FindByCondition(x =>
                        x.ContractApprovalFlowId == contractApprovalFlow.Id &&
                        x.IsActive)
                    .OrderBy(x => x.Order)
                    .ToListAsync(cancellationToken);

            if (!approvalUsers.Any())
            {
                _logger.LogError(
                    $"Approval users not found for contract. " +
                    $"ContractId: {request.ContractId}");
                throw new NotFoundCustomException(
                    "Approval users not found.",
                    "No approval users are configured for this contract.");
            }

            // ---------------------------------------------------------
            // 5. Find Current Logged-In User
            // ---------------------------------------------------------

            var currentApproval =
                approvalUsers.FirstOrDefault(x =>
                    x.UserId == request.UserId);

            if (currentApproval == null)
            {
                _logger.LogError(
                    $"Current user is not part of the approval flow. " +
                    $"ContractId: {request.ContractId}, " +
                    $"UserId: {request.UserId}");
                throw new ForBiddenCustomException(
                     "You are not authorized to approve this contract.",
                     "The current user is not part of the approval flow.");
            }

            // ---------------------------------------------------------
            // 6. Check Whether Current User Already Approved/Rejected
            // ---------------------------------------------------------

            if (currentApproval.Status == Common.APPROVED ||
                currentApproval.Status == Common.REJECTED)
            {
                _logger.LogError(
                    $"Current user has already completed approval. " +
                    $"ContractId: {request.ContractId}, " +
                    $"UserId: {request.UserId}, " +
                    $"Status: {currentApproval.Status}");
                throw new PreConditionFailedCustomException(
                    "Approval already completed.",
                    "You have already approved or rejected this contract.");
            }

            // ---------------------------------------------------------
            // 7. Check Previous Approval Level
            // ---------------------------------------------------------

            var previousApprovals =
                approvalUsers
                    .Where(x => x.Order < currentApproval.Order)
                    .ToList();

            bool previousLevelsApproved =
                previousApprovals.All(x =>
                    x.Status == Common.APPROVED);

            if (!previousLevelsApproved)
            {
                _logger.LogError(
                    $"Previous approval level is pending. " +
                    $"ContractId: {request.ContractId}, " +
                    $"UserId: {request.UserId}");
                throw new PreConditionFailedCustomException(
                    "Previous approval is pending.",
                    "The previous approval level must be approved first.");
            }

            // ---------------------------------------------------------
            // 8. Update Current Approval
            // ---------------------------------------------------------

            currentApproval.Status =
                request.Approval.Status;

            currentApproval.Comment =
                request.Approval.Comment;

            _repositoryWrapper
                .PredefinedContractApprovalUserMapping
                .Update(currentApproval);

            // ---------------------------------------------------------
            // 9. Find Last Approval Level
            // ---------------------------------------------------------

            int lastApprovalOrder =
                approvalUsers.Max(x => x.Order);

            // ---------------------------------------------------------
            // 10. If Rejected
            // ---------------------------------------------------------

            if (request.Approval.Status == Common.REJECTED)
            {
                // If the last approver rejects,
                // the approval process is completed.
                if (currentApproval.Order == lastApprovalOrder)
                {
                    contract.Status = Common.CONTRACT_COMPLETED_STATUS;
                }
                else
                {
                    contract.Status = Common.CONTRACT_REJECTED_STATUS;
                }

                _repositoryWrapper.PredefinedContract.Update(contract);

                await _repositoryWrapper.SaveAsync();

                _logger.LogInfo(
                    $"Contract rejected. " +
                    $"ContractId: {contract.Id}, " +
                    $"UserId: {request.UserId}, " +
                    $"ContractStatus: {contract.Status}");

                return contract.Id;
            }

            // ---------------------------------------------------------
            // 11. If Current User Is Last Approver
            // ---------------------------------------------------------

            if (currentApproval.Order == lastApprovalOrder)
            {
                // -----------------------------------------------------
                // 12. Verify All Approval Levels Are Approved
                // -----------------------------------------------------

                bool allApproved =
                    approvalUsers.All(x =>
                        x.Status == Common.APPROVED);

                if (!allApproved)
                {
                    _logger.LogError(
                        $"Not all approval levels are approved. " +
                        $"ContractId: {request.ContractId}, " +
                        $"UserId: {request.UserId}");

                    throw new PreConditionFailedCustomException(
                        "Approval flow is incomplete.",
                        "All approval levels must be approved before completing the contract.");
                }

                contract.Status = Common.COMPLETE;

                _repositoryWrapper.PredefinedContract.Update(contract);
            }
            else
            {
                // -----------------------------------------------------
                // 13. Approval Is Still In Progress
                // -----------------------------------------------------

                contract.Status = Common.CONTRACT_IN_PROCESS_STATUS;

                _repositoryWrapper.PredefinedContract.Update(contract);
            }

            // ---------------------------------------------------------
            // 14. Save Changes
            // ---------------------------------------------------------

            await _repositoryWrapper.SaveAsync();

            _logger.LogInfo(
                $"Contract approval processed successfully. " +
                $"ContractId: {contract.Id}, " +
                $"UserId: {request.UserId}, " +
                $"Status: {request.Approval.Status}, " +
                $"ContractStatus: {contract.Status}");

            return contract.Id;
        }
    }
}
