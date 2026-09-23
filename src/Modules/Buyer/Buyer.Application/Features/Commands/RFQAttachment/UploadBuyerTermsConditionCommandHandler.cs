using Buyer.Application.Features.Assets.Commands;
using Buyer.Domain.Common;
using Buyer.Domain.Entities;
using Buyer.Infrastructure.Contracts.IRepository;
using MediatR;
using Microsoft.EntityFrameworkCore;
using SharedKernel.ExceptionHandler;
using SharedKernel.LoggerServices;

namespace Buyer.Application.Features.Commands.RFQAttachment
{
    public class UploadBuyerTermsConditionCommandHandler
        : IRequestHandler<UploadBuyerTermsConditionCommand, Guid>
    {
        private readonly IRepositoryWrapper _repository;
        private readonly IMediator _mediator;
        private readonly ILoggerManager _logger;

        public UploadBuyerTermsConditionCommandHandler(
            IRepositoryWrapper repository,
            IMediator mediator,
            ILoggerManager logger)
        {
            _repository = repository;
            _mediator = mediator;
            _logger = logger;
        }

        public async Task<Guid> Handle(
            UploadBuyerTermsConditionCommand request,
            CancellationToken cancellationToken)
        {
            _logger.LogInfo(
                $"Uploading Buyer Terms and Condition for RFQId: {request.RFQId}, " +
                $"BuyerId: {request.BuyerId}, IsSingletonAsset: {request.IsSingletonAsset}");

            var rfq = await _repository.RFQ
                .FindByCondition(x => x.Id == request.RFQId)
                .FirstOrDefaultAsync(cancellationToken);

            if (rfq == null)
            {
                _logger.LogError($"RFQ not found for RFQId: {request.RFQId}");
                throw new NotFoundCustomException(
                    "RFQ not found.",
                    $"No RFQ exists with Id: {request.RFQId}.");
            }

            if (rfq.BuyerId != request.BuyerId)
            {
                _logger.LogError(
                    $"RFQId: {request.RFQId} does not belong to BuyerId: {request.BuyerId}.");
                throw new BadRequestCustomException(
                    "This RFQ does not belong to the given buyer.",
                    $"RFQId: {request.RFQId} belongs to BuyerId: {rfq.BuyerId}.");
            }

            if (request.Document == null)
            {
                throw new BadRequestCustomException(
                    "Document is required.",
                    "Please provide the document to upload.");
            }

            Guid assetId = await _mediator.Send(
                new UploadAssetCommand(request.Document),
                cancellationToken);

            if (request.IsSingletonAsset)
            {
                var existingMappings = await _repository.RFQAttachmentMapping
                    .FindByCondition(x =>
                        x.RFQId == request.RFQId &&
                        x.Type == Common.TERMS_CONDITION &&
                        x.IsActive)
                    .ToListAsync(cancellationToken);

                foreach (var existingMapping in existingMappings)
                {
                    existingMapping.IsActive = false;
                    _repository.RFQAttachmentMapping.Update(existingMapping);
                }

                _logger.LogInfo(
                    $"Disabled {existingMappings.Count} existing Buyer Terms and Condition " +
                    $"attachment(s) for RFQId: {request.RFQId}.");
            }

            var mapping = new RFQAttachmentMapping
            {
                Id = Guid.NewGuid(),
                RFQId = rfq.Id,
                AssetId = assetId,
                Type = Common.TERMS_CONDITION
            };

            await _repository.RFQAttachmentMapping.CreateAsync(mapping);
            await _repository.SaveAsync();

            _logger.LogInfo(
                $"Buyer Terms and Condition uploaded successfully for RFQId: {request.RFQId}. " +
                $"MappingId: {mapping.Id}, AssetId: {assetId}");

            return mapping.Id;
        }
    }
}
