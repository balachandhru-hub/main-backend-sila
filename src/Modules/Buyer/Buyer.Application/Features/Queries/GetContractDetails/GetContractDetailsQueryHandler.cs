using Buyer.Domain.Dtos;
using Buyer.Infrastructure.Contracts.IRepository;
using MediatR;
using Microsoft.EntityFrameworkCore;
using SharedKernel.ExceptionHandler;
using SharedKernel.LoggerServices;

namespace Buyer.Application.Features.Queries.GetContractDetails
{
    public class GetContractDetailsQueryHandler
        : IRequestHandler<GetContractDetailsQuery, ContractDetailsResponseDto>
    {
        private readonly IRepositoryWrapper _repository;
        private readonly ILoggerManager _logger;

        public GetContractDetailsQueryHandler(
            IRepositoryWrapper repository,
            ILoggerManager logger)
        {
            _repository = repository;
            _logger = logger;
        }

        public async Task<ContractDetailsResponseDto> Handle(
            GetContractDetailsQuery request,
            CancellationToken cancellationToken)
        {
            var contract = await _repository.ContractDetails
                .FindByCondition(x =>
                    x.Id == request.Id &&
                    x.IsActive)
                .FirstOrDefaultAsync(cancellationToken);

            if (contract == null)
            {
                _logger.LogError($"Contract details not found. Id: {request.Id}");
                throw new NotFoundCustomException(
                    "Contract details not found.",
                    $"No contract details was found with Id: {request.Id}");
            }

            var attachments = await _repository.ContractAttachment
                .FindByCondition(x =>
                    x.ContractDetailsId == contract.Id &&
                    x.IsActive)
                .ToListAsync(cancellationToken);

            var assetIds = attachments.Select(x => x.AssetId).ToList();

            var assetFileNamesById = await _repository.Asset
                .FindByCondition(x => assetIds.Contains(x.Id))
                .ToDictionaryAsync(x => x.Id, x => x.FileName, cancellationToken);

            return new ContractDetailsResponseDto
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
                DateCreated = contract.DateCreated,
                Attachments = attachments
                    .Select(x => new ContractAttachmentDetailsDto
                    {
                        Id = x.Id,
                        AssetId = x.AssetId,
                        FileName = assetFileNamesById.TryGetValue(x.AssetId, out var fileName)
                            ? fileName
                            : null
                    })
                    .ToList()
            };
        }
    }
}
