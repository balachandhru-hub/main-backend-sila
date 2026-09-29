using Buyer.Application.Features.Assets.Commands;
using Buyer.Domain.Common;
using Buyer.Domain.Dto;
using Buyer.Domain.Entities;
using Buyer.Infrastructure.Contracts.IRepository;
using MediatR;
using Microsoft.EntityFrameworkCore;
using SharedKernel.ExceptionHandler;
using SharedKernel.LoggerServices;

namespace Buyer.Application.Features.Commands.CreatePredefinedContract
{
    public class CreatePredefinedContractCommandHandler
        : IRequestHandler<CreatePredefinedContractCommand, Guid>
    {
        private readonly IRepositoryWrapper _repository;
        private readonly IMediator _mediator;
        private readonly ILoggerManager _logger;

        public CreatePredefinedContractCommandHandler(
            IRepositoryWrapper repository,
            IMediator mediator,
            ILoggerManager logger)
        {
            _repository = repository;
            _mediator = mediator;
            _logger = logger;
        }

        public async Task<Guid> Handle(
            CreatePredefinedContractCommand request,
            CancellationToken cancellationToken)
        {
            var dto = request.Request;

            if (dto.EndDate <= dto.StartDate)
            {
                throw new BadRequestCustomException(
                    "Invalid contract dates.",
                    "Contract end date must be after the start date.");
            }

            _logger.LogInfo($"Creating contract for RFQId: {dto.RFQId}");

            var rfq = await _repository.RFQ
                .FindByCondition(x =>
                    x.Id == dto.RFQId &&
                    x.BuyerId == request.BuyerId &&
                    x.IsActive)
                .FirstOrDefaultAsync(cancellationToken);

            if (rfq == null)
            {
                _logger.LogError($"RFQ not found. RFQId: {dto.RFQId}");
                throw new NotFoundCustomException(
                    "RFQ not found.",
                    $"No RFQ was found with RFQId: {dto.RFQId}");
            }

            if (!dto.SupplierId.HasValue || dto.SupplierId.Value == Guid.Empty)
            {
                throw new BadRequestCustomException(
                    "Supplier is required.",
                    "Please provide a valid supplierId for the contract.");
            }

            Guid supplierId = dto.SupplierId.Value;

            // Same RFQ + same supplier => update the existing contract.
            // Id and ContractNumber are never regenerated.
            var contract = await _repository.PredefinedContract
                .FindFirstByConditionAsync(x =>
                    x.RFQId == rfq.Id &&
                    x.SupplierId == supplierId &&
                    x.IsActive);

            bool isUpdate = contract != null;

            if (isUpdate)
            {
                _logger.LogInfo(
                    $"Contract already exists for RFQId: {dto.RFQId}, SupplierId: {supplierId}. " +
                    $"Updating ContractId: {contract!.Id}, ContractNumber: {contract.ContractNumber}");

                contract.ContractName = dto.ContractName;
                contract.StartDate = dto.StartDate;
                contract.EndDate = dto.EndDate;
                contract.Amount = dto.Amount;

                // The amount may have changed, so the old approval flow no longer applies.
                await RemoveExistingApprovalFlowAsync(contract.Id, cancellationToken);
            }
            else
            {
                long nextNumber = await _repository.PredefinedContract
                    .GetNextContractNumberAsync(cancellationToken);

                contract = new PredefinedContract
                {
                    Id = Guid.NewGuid(),
                    RFQId = rfq.Id,
                    ContractName = dto.ContractName,
                    BuyerId = request.BuyerId,
                    SupplierId = supplierId,
                    StartDate = dto.StartDate,
                    EndDate = dto.EndDate,
                    Amount = dto.Amount,
                    ContractNumber = $"CN{nextNumber:D8}"
                };
            }

            bool requiresApproval = await _repository.PredefinedContract.CreateApprovalFlowForContractAsync(
                contract.Id,
                request.BuyerId,
                contract.Amount,
                cancellationToken);

            // No approver in the flow means there is nothing to wait on, so the
            // contract is complete immediately; otherwise it starts out OPEN
            // until the approval flow finishes.
            contract.Status = requiresApproval
                ? Common.CONTRACT_OPEN_STATUS
                : Common.COMPLETE;

            contract.ContractStatus = Common.CONTRACT_CREATED_STATUS;

            if (isUpdate)
            {
                _repository.PredefinedContract.Update(contract);
            }
            else
            {
                _repository.PredefinedContract.Create(contract);
            }

            if (dto.Attachments != null && dto.Attachments.Any())
            {
                _logger.LogInfo(
                    $"Uploading {dto.Attachments.Count} attachment(s) for ContractId: {contract.Id}");

                foreach (var attachment in dto.Attachments)
                {
                    Guid assetId = await _mediator.Send(
                        new UploadAssetCommand(attachment), cancellationToken);

                    _repository.PredefinedContractAttachment.Create(new PredefinedContractAttachment
                    {
                        Id = Guid.NewGuid(),
                        ContractId = contract.Id,
                        AssetId = assetId,
                        Type = Common.CONTRACT_ATTACHMENT,
                        Title = attachment.FileName ?? string.Empty
                    });
                }
            }

            await _repository.SaveAsync();

            _logger.LogInfo(
                $"Contract {(isUpdate ? "updated" : "created")} for RFQId: {dto.RFQId}. " +
                $"ContractId: {contract.Id}, ContractNumber: {contract.ContractNumber}");

            return contract.Id;
        }

        private async Task RemoveExistingApprovalFlowAsync(
            Guid contractId,
            CancellationToken cancellationToken)
        {
            var approvalFlows = await _repository.PredefinedContractApprovalFlow
                .FindByCondition(x => x.ContractId == contractId)
                .ToListAsync(cancellationToken);

            if (!approvalFlows.Any())
            {
                return;
            }

            var flowIds = approvalFlows.Select(x => x.Id).ToList();

            var approvalUsers = await _repository.PredefinedContractApprovalUserMapping
                .FindByCondition(x => flowIds.Contains(x.ContractApprovalFlowId))
                .ToListAsync(cancellationToken);

            _repository.PredefinedContractApprovalUserMapping.DeleteRange(approvalUsers);
            _repository.PredefinedContractApprovalFlow.DeleteRange(approvalFlows);
        }
    }
}
