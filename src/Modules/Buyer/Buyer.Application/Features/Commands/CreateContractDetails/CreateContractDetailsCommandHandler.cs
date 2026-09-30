using Buyer.Application.Features.Assets.Commands;
using Buyer.Domain.Entities;
using Buyer.Infrastructure.Contracts.IRepository;
using MediatR;
using Microsoft.EntityFrameworkCore;
using SharedKernel.ExceptionHandler;
using SharedKernel.LoggerServices;

namespace Buyer.Application.Features.Commands.CreateContractDetails
{
    public class CreateContractDetailsCommandHandler
        : IRequestHandler<CreateContractDetailsCommand, Guid>
    {
        private readonly IRepositoryWrapper _repository;
        private readonly IMediator _mediator;
        private readonly ILoggerManager _logger;

        public CreateContractDetailsCommandHandler(
            IRepositoryWrapper repository,
            IMediator mediator,
            ILoggerManager logger)
        {
            _repository = repository;
            _mediator = mediator;
            _logger = logger;
        }

        public async Task<Guid> Handle(
            CreateContractDetailsCommand request,
            CancellationToken cancellationToken)
        {
            var dto = request.Request;

            _logger.LogInfo(
                $"Creating contract details for PredefinedContractId: {dto.PredefinedContractId}, BuyerId: {request.BuyerId}");

            var predefinedContract = await _repository.PredefinedContract
                .FindByCondition(x =>
                    x.Id == dto.PredefinedContractId &&
                    x.BuyerId == request.BuyerId &&
                    x.IsActive)
                .FirstOrDefaultAsync(cancellationToken);

            if (predefinedContract == null)
            {
                _logger.LogError($"Contract not found. PredefinedContractId: {dto.PredefinedContractId}");
                throw new NotFoundCustomException(
                    "Contract not found.",
                    $"No contract was found with Id: {dto.PredefinedContractId}");
            }

            var contractDetails = new ContractDetails
            {
                Id = Guid.NewGuid(),
                PredefinedContractId = predefinedContract.Id,
                ContractNumber = predefinedContract.ContractNumber,
                ContractName = predefinedContract.ContractName,
                RFQId = predefinedContract.RFQId,
                BuyerId = predefinedContract.BuyerId,
                SupplierId = predefinedContract.SupplierId,
                StartDate = predefinedContract.StartDate,
                EndDate = predefinedContract.EndDate,
                Amount = predefinedContract.Amount,
                Status = predefinedContract.Status,
                ContractStatus = predefinedContract.ContractStatus
            };

            _repository.ContractDetails.Create(contractDetails);

            Guid assetId = await _mediator.Send(
                new UploadAssetCommand(dto.Attachment.Asset), cancellationToken);

            _repository.ContractAttachment.Create(new ContractAttachment
            {
                Id = Guid.NewGuid(),
                ContractDetailsId = contractDetails.Id,
                AssetId = assetId,

            });

            await _repository.SaveAsync();

            _logger.LogInfo(
                $"Contract details created. Id: {contractDetails.Id}, PredefinedContractId: {dto.PredefinedContractId}");

            return contractDetails.Id;
        }
    }
}
