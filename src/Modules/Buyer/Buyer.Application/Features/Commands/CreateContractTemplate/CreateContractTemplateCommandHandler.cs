using Buyer.Application.Features.Assets.Commands;
using Buyer.Domain.Entities;
using Buyer.Infrastructure.Contracts.IRepository;
using MediatR;
using SharedKernel.ExceptionHandler;
using SharedKernel.LoggerServices;

namespace Buyer.Application.Features.Commands.CreateContractTemplate
{
    public class CreateContractTemplateCommandHandler
        : IRequestHandler<CreateContractTemplateCommand, Guid>
    {
        private readonly IRepositoryWrapper _repository;
        private readonly IMediator _mediator;
        private readonly ILoggerManager _logger;

        public CreateContractTemplateCommandHandler(
            IRepositoryWrapper repository,
            IMediator mediator,
            ILoggerManager logger)
        {
            _repository = repository;
            _mediator = mediator;
            _logger = logger;
        }

        public async Task<Guid> Handle(
            CreateContractTemplateCommand request,
            CancellationToken cancellationToken)
        {
            var dto = request.Request;

            _logger.LogInfo(
                $"Creating contract template attachment for BuyerId: {request.BuyerId}, SegmentId: {dto.SegmentId}");

            bool existing = _repository.ContractTemplate
                .FindByCondition(x =>
                    x.BuyerId == request.BuyerId &&
                    x.SegmentId == dto.SegmentId &&
                    x.IsActive)
                .Any();

            if (existing)
            {
                _logger.LogError(
                    $"Duplicate contract template. SegmentId: {dto.SegmentId} already has an attachment for BuyerId: {request.BuyerId}");
                throw new ConflictCustomException(
                    "Duplicate segment attachment.",
                    "An attachment has already been uploaded for this segment.");
            }

            Guid assetId = await _mediator.Send(
                new UploadAssetCommand(dto.Attachment), cancellationToken);

            var contractTemplate = new ContractTemplate
            {
                Id = Guid.NewGuid(),
                SegmentId = dto.SegmentId,
                BuyerId = request.BuyerId,
                AssetId = assetId
            };

            _repository.ContractTemplate.Create(contractTemplate);
            await _repository.SaveAsync();

            _logger.LogInfo(
                $"Contract template created. Id: {contractTemplate.Id}, SegmentId: {dto.SegmentId}");

            return contractTemplate.Id;
        }
    }
}
