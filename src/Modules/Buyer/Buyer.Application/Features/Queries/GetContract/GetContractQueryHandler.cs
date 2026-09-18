using Buyer.Domain.Dto;
using Buyer.Domain.Dtos;
using Buyer.Infrastructure.Contracts.IRepository;
using MediatR;
using Microsoft.EntityFrameworkCore;
using SharedKernel.ExceptionHandler;
using SharedKernel.LoggerServices;

namespace Buyer.Application.Features.Queries.GetContract
{
    public class GetContractQueryHandler
        : IRequestHandler<GetContractQuery, ContractResponseDto>
    {
        private readonly IRepositoryWrapper _repository;
        private readonly ILoggerManager _logger;

        public GetContractQueryHandler(
            IRepositoryWrapper repository,
            ILoggerManager logger)
        {
            _repository = repository;
            _logger = logger;
        }

        public async Task<ContractResponseDto> Handle(
            GetContractQuery request,
            CancellationToken cancellationToken)
        {
            var contract = await _repository.Contract
                .FindByCondition(x =>
                    x.Id == request.ContractId &&
                    x.IsActive)
                .Include(x => x.RFQ)
                .FirstOrDefaultAsync(cancellationToken);

            if (contract == null)
            {
                _logger.LogError($"Contract not found. ContractId: {request.ContractId}");
                throw new NotFoundCustomException(
                    "Contract not found.",
                    $"No contract was found with ContractId: {request.ContractId}");
            }

            var attachments = await _repository.ContractAttachment
                .FindByCondition(x =>
                    x.ContractId == contract.Id &&
                    x.IsActive)
                .ToListAsync(cancellationToken);

            var assetIds = attachments.Select(x => x.AssetId).ToList();

            var assetFileNamesById = await _repository.Asset
                .FindByCondition(x => assetIds.Contains(x.Id))
                .ToDictionaryAsync(x => x.Id, x => x.FileName, cancellationToken);

            var approvalFlows = await _repository.ContractApprovalFlow
                .FindByCondition(x =>
                    x.ContractId == contract.Id &&
                    x.IsActive)
                .ToListAsync(cancellationToken);

            return new ContractResponseDto
            {
                Id = contract.Id,
                ContractNumber = contract.ContractNumber,
                ContractName = contract.ContractName,
                RFQId = contract.RFQId,
                RFQNumber = contract.RFQ?.RFQNumber,
                RFQTitle = contract.RFQ?.Title,
                StartDate = contract.StartDate,
                EndDate = contract.EndDate,
                Amount = contract.Amount,
                DateCreated = contract.DateCreated,
                Attachments = attachments
                    .Select(x => new ContractAttachmentDto
                    {
                        Id = x.Id,
                        AssetId = x.AssetId,
                        Type = x.Type,
                        FileName = assetFileNamesById.TryGetValue(x.AssetId, out var fileName)
                            ? fileName
                            : null
                    })
                    .ToList(),
                ApprovalFlows = approvalFlows
                    .Select(x => new ContractApprovalFlowDto
                    {
                        Id = x.Id,
                        ApprovalCode = x.ApprovalCode,
                        ApprovalName = x.ApprovalName,
                        ContractId = x.ContractId,
                        Type = x.Type,
                        TotalAmount = x.TotalAmount,
                        Currency = x.Currency
                    })
                    .ToList()
            };
        }
    }
}
