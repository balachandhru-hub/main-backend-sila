using Buyer.Domain.Dtos;
using Buyer.Infrastructure.Contracts.IRepository;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Buyer.Application.Features.Queries.GetAllContractDetails
{
    public class GetAllContractDetailsQueryHandler
        : IRequestHandler<GetAllContractDetailsQuery, List<ContractDetailsResponseDto>>
    {
        private readonly IRepositoryWrapper _repository;

        public GetAllContractDetailsQueryHandler(IRepositoryWrapper repository)
        {
            _repository = repository;
        }

        public async Task<List<ContractDetailsResponseDto>> Handle(
            GetAllContractDetailsQuery request,
            CancellationToken cancellationToken)
        {
            var contractDetailsList = await _repository.ContractDetails
                .FindByCondition(x => x.IsActive && x.BuyerId == request.BuyerId)
                .OrderByDescending(x => x.DateCreated)
                .Skip(request.Index)
                .Take(request.Limit)
                .Select(contract => new ContractDetailsResponseDto
                {
                    Id = contract.Id,
                    PredefinedContractId = contract.PredefinedContractId,
                    ContractNumber = contract.ContractNumber,
                    ContractName = contract.ContractName,
                    RFQId = contract.RFQId,
                    BuyerId = contract.BuyerId,
                    SupplierId = contract.SupplierId,
                    StartDate = contract.StartDate,
                    EndDate = contract.EndDate,
                    Amount = contract.Amount,
                    Status = contract.Status,
                    ContractStatus = contract.ContractStatus,
                    DateCreated = contract.DateCreated
                })
                .ToListAsync(cancellationToken);

            var contractDetailsIds = contractDetailsList.Select(x => x.Id).ToList();

            var attachments = await _repository.ContractAttachment
                .FindByCondition(x =>
                    contractDetailsIds.Contains(x.ContractDetailsId) &&
                    x.IsActive)
                .ToListAsync(cancellationToken);

            var assetIds = attachments.Select(x => x.AssetId).ToList();

            var assetFileNamesById = await _repository.Asset
                .FindByCondition(x => assetIds.Contains(x.Id))
                .ToDictionaryAsync(x => x.Id, x => x.FileName, cancellationToken);

            var attachmentsByContractDetailsId = attachments
                .GroupBy(x => x.ContractDetailsId)
                .ToDictionary(
                    g => g.Key,
                    g => g.Select(x => new ContractAttachmentDetailsDto
                    {
                        Id = x.Id,
                        AssetId = x.AssetId,
                        FileName = assetFileNamesById.TryGetValue(x.AssetId, out var fileName)
                            ? fileName
                            : null
                    }).ToList());

            foreach (var contractDetails in contractDetailsList)
            {
                contractDetails.Attachments = attachmentsByContractDetailsId.TryGetValue(contractDetails.Id, out var atts)
                    ? atts
                    : new List<ContractAttachmentDetailsDto>();
            }

            return contractDetailsList;
        }
    }
}
