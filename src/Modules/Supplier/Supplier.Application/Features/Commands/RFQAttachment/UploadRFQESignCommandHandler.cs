using MediatR;
using Microsoft.EntityFrameworkCore;
using SharedKernel.ExceptionHandler;
using SharedKernel.LoggerServices;
using Supplier.Application.Features.Commands.Asset;
using Supplier.Domain.Common;
using Supplier.Domain.Entities;
using Supplier.Infrastructure.Contracts.IRepository;

namespace Supplier.Application.Features.Commands.RFQAttachment
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
                $"Uploading E-Sign document for BuyerRFQId: {request.RFQId}");

            var supplier = _repository.SupplierBusinessProfile
                .FindFirstByCondition(x =>
                    x.OrganizationId == request.OrganizationId &&
                    x.IsActive);

            if (supplier == null)
            {
                _logger.LogError(
                    $"Supplier not found for OrganizationId: {request.OrganizationId}");
                throw new PreConditionFailedCustomException(
                    "Supplier not found.",
                    $"Supplier not found for the given organization : {request.OrganizationId}");
            }

            // Scoped by SupplierId as well as BuyerRFQId: one BuyerRFQ fans
            // out to one SupplierRFQ per invited supplier, so the caller
            // must only ever be able to attach a document to their own RFQ.
            var supplierRFQ = await _repository.SupplierRFQ
                .FindByCondition(x =>
                    x.BuyerRFQId == request.RFQId &&
                    x.SupplierId == supplier.Id)
                .FirstOrDefaultAsync(cancellationToken);

            if (supplierRFQ == null)
            {
                _logger.LogError(
                    $"RFQ not found for BuyerRFQId: {request.RFQId} and SupplierId: {supplier.Id}");
                throw new NotFoundCustomException(
                    "RFQ not found.",
                    $"No RFQ exists with BuyerRFQId: {request.RFQId} for this supplier.");
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

            // One SupplierRFQ has at most one E-Sign mapping: same
            // SupplierRFQId -> update the existing row's asset instead of
            // inserting a duplicate.
            var mapping = await _repository.RFQAttachmentMapping
                .FindByCondition(x =>
                    x.SupplierRFQId == supplierRFQ.Id &&
                    x.Type == Common.ESIGN &&
                    x.IsActive)
                .FirstOrDefaultAsync(cancellationToken);

            if (mapping == null)
            {
                mapping = new RFQAttachmentMapping
                {
                    Id = Guid.NewGuid(),
                    BuyerRFQId = supplierRFQ.BuyerRFQId,
                    SupplierRFQId = supplierRFQ.Id,
                    AssetId = assetId,
                    Type = Common.ESIGN,
                    SupplierId = supplier.Id
                };

                await _repository.RFQAttachmentMapping.CreateAsync(mapping);

                _logger.LogInfo(
                    $"E-Sign mapping created for BuyerRFQId: {request.RFQId}, " +
                    $"SupplierId: {supplier.Id}. AssetId: {assetId}");
            }
            else
            {
                mapping.AssetId = assetId;
                _repository.RFQAttachmentMapping.Update(mapping);

                _logger.LogInfo(
                    $"E-Sign mapping updated for BuyerRFQId: {request.RFQId}, " +
                    $"SupplierId: {supplier.Id}. AssetId: {assetId}");
            }

            await _repository.SaveAsync();

            return mapping.Id;
        }
    }
}
