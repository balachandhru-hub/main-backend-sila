using Buyer.Domain.Dto;
using Buyer.Domain.Dtos;
using Buyer.Infrastructure.Contracts.IRepository;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Buyer.Application.Features.Queries.GetAllContracts
{
    public class GetAllContractsQueryHandler
        : IRequestHandler<GetAllContractsQuery, List<ContractResponseDto>>
    {
        private readonly IRepositoryWrapper _repository;

        public GetAllContractsQueryHandler(IRepositoryWrapper repository)
        {
            _repository = repository;
        }

        public async Task<List<ContractResponseDto>> Handle(
            GetAllContractsQuery request,
            CancellationToken cancellationToken)
        {
            var contracts = await _repository.Contract
                .FindByCondition(x => x.IsActive && x.BuyerId == request.BuyerId)
                .Include(x => x.RFQ)
                .OrderByDescending(x => x.DateCreated)
                .Skip(request.Index)
                .Take(request.Limit)
                .Select(contract => new ContractResponseDto
                {
                    Id = contract.Id,
                    ContractNumber = contract.ContractNumber,
                    ContractName = contract.ContractName,
                    RFQId = contract.RFQId,
                    RFQNumber = contract.RFQ != null ? contract.RFQ.RFQNumber : null,
                    RFQTitle = contract.RFQ != null ? contract.RFQ.Title : null,
                    StartDate = contract.StartDate,
                    EndDate = contract.EndDate,
                    Amount = contract.Amount,
                    DateCreated = contract.DateCreated
                })
                .ToListAsync(cancellationToken);

            var contractIds = contracts.Select(x => x.Id).ToList();

            var attachments = await _repository.ContractAttachment
                .FindByCondition(x =>
                    contractIds.Contains(x.ContractId) &&
                    x.IsActive)
                .ToListAsync(cancellationToken);

            var assetIds = attachments.Select(x => x.AssetId).ToList();

            var assetFileNamesById = await _repository.Asset
                .FindByCondition(x => assetIds.Contains(x.Id))
                .ToDictionaryAsync(x => x.Id, x => x.FileName, cancellationToken);

            var attachmentsByContractId = attachments
                .GroupBy(x => x.ContractId)
                .ToDictionary(
                    g => g.Key,
                    g => g.Select(x => new ContractAttachmentDto
                    {
                        Id = x.Id,
                        AssetId = x.AssetId,
                        Type = x.Type,
                        FileName = assetFileNamesById.TryGetValue(x.AssetId, out var fileName)
                            ? fileName
                            : null
                    }).ToList());

            var approvalFlows = await _repository.ContractApprovalFlow
                .FindByCondition(x =>
                    contractIds.Contains(x.ContractId) &&
                    x.IsActive)
                .ToListAsync(cancellationToken);

            var approvalFlowsByContractId = approvalFlows
                .GroupBy(x => x.ContractId)
                .ToDictionary(
                    g => g.Key,
                    g => g.Select(x => new ContractApprovalFlowDto
                    {
                        Id = x.Id,
                        ApprovalCode = x.ApprovalCode,
                        ApprovalName = x.ApprovalName,
                        ContractId = x.ContractId,
                        Type = x.Type,
                        TotalAmount = x.TotalAmount,
                        Currency = x.Currency
                    }).ToList());

            foreach (var contract in contracts)
            {
                contract.Attachments = attachmentsByContractId.TryGetValue(contract.Id, out var atts)
                    ? atts
                    : new List<ContractAttachmentDto>();

                contract.ApprovalFlows = approvalFlowsByContractId.TryGetValue(contract.Id, out var flows)
                    ? flows
                    : new List<ContractApprovalFlowDto>();
            }

            return contracts;
        }
    }
}
