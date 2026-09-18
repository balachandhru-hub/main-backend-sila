using Buyer.Application.Features.Assets.Commands;
using Buyer.Domain.Common;
using Buyer.Domain.Entities;
using Buyer.Infrastructure.Contracts.IRepository;
using MediatR;
using Microsoft.EntityFrameworkCore;
using SharedKernel.ExceptionHandler;
using SharedKernel.LoggerServices;

namespace Buyer.Application.Features.Commands.CreateContract
{
    public class CreateContractCommandHandler
        : IRequestHandler<CreateContractCommand, Guid>
    {
        private readonly IRepositoryWrapper _repository;
        private readonly IMediator _mediator;
        private readonly ILoggerManager _logger;

        public CreateContractCommandHandler(
            IRepositoryWrapper repository,
            IMediator mediator,
            ILoggerManager logger)
        {
            _repository = repository;
            _mediator = mediator;
            _logger = logger;
        }

        public async Task<Guid> Handle(
            CreateContractCommand request,
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
                    x.IsActive)
                .FirstOrDefaultAsync(cancellationToken);

            if (rfq == null)
            {
                _logger.LogError($"RFQ not found. RFQId: {dto.RFQId}");
                throw new NotFoundCustomException(
                    "RFQ not found.",
                    $"No RFQ was found with RFQId: {dto.RFQId}");
            }

            long nextNumber = await _repository.Contract
                .GetNextContractNumberAsync(cancellationToken);

            var contract = new Contract
            {
                Id = Guid.NewGuid(),
                RFQId = rfq.Id,
                ContractName = dto.ContractName,
                BuyerId = request.BuyerId,
                SupplierId = request.SupplierId,
                StartDate = dto.StartDate,
                EndDate = dto.EndDate,
                Amount = dto.Amount,
                ContractNumber = $"CN{nextNumber:D8}"
            };

            bool requiresApproval = await _repository.Contract.CreateApprovalFlowForContractAsync(
                contract.Id,
                request.BuyerId,
                contract.Amount,
                cancellationToken);

            contract.Status = requiresApproval
                ? Common.CONTRACT_IN_PROCESS_STATUS
                : Common.CONTRACT_COMPLETED_STATUS;

            _repository.Contract.Create(contract);

            if (dto.Attachments != null && dto.Attachments.Any())
            {
                _logger.LogInfo(
                    $"Uploading {dto.Attachments.Count} attachment(s) for ContractId: {contract.Id}");

                foreach (var attachment in dto.Attachments)
                {
                    Guid assetId = await _mediator.Send(
                        new UploadAssetCommand(attachment), cancellationToken);

                    _repository.ContractAttachment.Create(new ContractAttachment
                    {
                        Id = Guid.NewGuid(),
                        ContractId = contract.Id,
                        AssetId = assetId,
                        Type = Common.CONTRACT_ATTACHMENT
                    });
                }
            }

            await _repository.SaveAsync();

            _logger.LogInfo(
                $"Contract created for RFQId: {dto.RFQId}. " +
                $"ContractId: {contract.Id}, ContractNumber: {contract.ContractNumber}");

            return contract.Id;
        }
    }
}
