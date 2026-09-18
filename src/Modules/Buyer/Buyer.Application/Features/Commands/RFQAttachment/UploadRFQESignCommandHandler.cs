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
    public class UploadRFQESignCommandHandler
        : IRequestHandler<UploadRFQESignCommand, Guid>
    {
        private readonly IRepositoryWrapper _repository;
        private readonly IMediator _mediator;
        private readonly ILoggerManager _logger;

        public UploadRFQESignCommandHandler(
            IRepositoryWrapper repository,
            IMediator mediator,
            ILoggerManager logger)
        {
            _repository = repository;
            _mediator = mediator;
            _logger = logger;
        }

        public async Task<Guid> Handle(
            UploadRFQESignCommand request,
            CancellationToken cancellationToken)
        {
            _logger.LogInfo(
                $"Uploading E-Sign document for RFQId: {request.RFQId}");

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

            if (request.Document == null)
            {
                throw new BadRequestCustomException(
                    "Document is required.",
                    "Please provide the document to upload.");
            }

            Guid assetId = await _mediator.Send(
                new UploadAssetCommand(request.Document),
                cancellationToken);

            // One RFQ has at most one E-Sign mapping: same RFQId -> update
            // the existing row's asset instead of inserting a duplicate.
            var mapping = await _repository.RFQAttachmentMapping
                .FindByCondition(x =>
                    x.RFQId == rfq.Id &&
                    x.Type == Common.ESIGN &&
                    x.IsActive)
                .FirstOrDefaultAsync(cancellationToken);

            if (mapping == null)
            {
                mapping = new RFQAttachmentMapping
                {
                    Id = Guid.NewGuid(),
                    RFQId = rfq.Id,
                    AssetId = assetId,
                    Type = Common.ESIGN
                };

                await _repository.RFQAttachmentMapping.CreateAsync(mapping);

                _logger.LogInfo(
                    $"E-Sign mapping created for RFQId: {request.RFQId}. AssetId: {assetId}");
            }
            else
            {
                mapping.AssetId = assetId;
                _repository.RFQAttachmentMapping.Update(mapping);

                _logger.LogInfo(
                    $"E-Sign mapping updated for RFQId: {request.RFQId}. AssetId: {assetId}");
            }

            await _repository.SaveAsync();

            return mapping.Id;
        }
    }
}
