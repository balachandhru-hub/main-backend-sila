using MediatR;
using Microsoft.EntityFrameworkCore;
using SharedKernel.ExceptionHandler;
using SharedKernel.LoggerServices;
using Supplier.Application.Contracts;
using Supplier.Application.Features.Commands.Asset;
using Supplier.Application.Features.Commands.SubmitVerification;
using Supplier.Domain.Common;
using Supplier.Domain.Entities;
using Supplier.Infrastructure.Contracts.IRepository;

namespace Supplier.Application.Features.Commands.Verification
{
    public class SubmitVerificationCommandHandler
        : IRequestHandler<SubmitVerificationCommand, bool>
    {
        private readonly IRepositoryWrapper _repository;
        private readonly ILoggerManager _logger;
        private readonly IBuyerApiClient _buyerApiClient;
        private readonly IMediator _mediator;

        public SubmitVerificationCommandHandler(
            IRepositoryWrapper repository,
            ILoggerManager logger,
            IBuyerApiClient buyerApiClient,
            IMediator mediator)
        {
            _repository = repository;
            _logger = logger;
            _buyerApiClient = buyerApiClient;
            _mediator = mediator;
        }

        public async Task<bool> Handle(
            SubmitVerificationCommand request,
            CancellationToken cancellationToken)
        {
            _logger.LogInfo($"Supplier Verification Started. RequestId : {request.Verification.VerificationRequestId}");

            var verificationRequest =
                await _buyerApiClient.GetSupplierVerificationRequestDetail(
                    request.Verification.VerificationRequestId,
                    cancellationToken);

            if (verificationRequest == null)
            {
                throw new NotFoundCustomException(
                    "Supplier Verification Request not found.",
                    "Invalid Supplier Verification Request.");
            }

            if (verificationRequest.Status.Equals(Common.SUBMITTED, StringComparison.OrdinalIgnoreCase))
            {
                throw new BadRequestCustomException(
                    "Verification already submitted.",
                    "You cannot modify submitted verification.");
            }

            foreach (var answer in request.Verification.Answers)
            {
                Guid? assetId = null;

                if (answer.Attachment != null)
                {
                    assetId = await _mediator.Send(
                        new UploadAssetCommand(answer.Attachment),
                        cancellationToken);
                }

                var existingAnswer = await _repository.SupplierVerificationAnswer
                    .FindByCondition(x =>
                        x.SupplierVerificationRequestId == request.Verification.VerificationRequestId &&
                        x.VerificationTemplateQuestionId == answer.VerificationTemplateQuestionId)
                    .FirstOrDefaultAsync(cancellationToken);

                if (existingAnswer == null)
                {
                    existingAnswer = new SupplierVerificationAnswer
                    {
                        Id = Guid.NewGuid(),
                        SupplierVerificationRequestId = request.Verification.VerificationRequestId,
                        SupplierId = request.Verification.SupplierId,
                        TemplateId = answer.TemplateId,
                        VerificationTemplateQuestionId = answer.VerificationTemplateQuestionId,
                        VerificationTemplateQuestionOptionId = answer.VerificationTemplateQuestionOptionId,
                        Answer = answer.Answer,
                        AssetId = assetId,
                        AnsweredOn = DateTime.UtcNow
                    };

                    await _repository.SupplierVerificationAnswer.CreateAsync(existingAnswer);
                }
                else
                {
                    existingAnswer.Answer = answer.Answer;
                    existingAnswer.TemplateId = answer.TemplateId;
                    existingAnswer.VerificationTemplateQuestionOptionId =
                        answer.VerificationTemplateQuestionOptionId;

                    if (assetId != null)
                    {
                        existingAnswer.AssetId = assetId;
                    }

                    existingAnswer.AnsweredOn = DateTime.UtcNow;

                    _repository.SupplierVerificationAnswer.Update(existingAnswer);
                }
            }

            await _repository.SaveAsync();

            _logger.LogInfo("Verification answers saved successfully.");

            // Update Buyer Status
            if (request.Verification.Status.Equals(Common.DRAFT, StringComparison.OrdinalIgnoreCase))
            {
                await _buyerApiClient.UpdateVerificationRequestStatus(
                    request.Verification.VerificationRequestId,
                    Common.DRAFT,
                    cancellationToken);

                _logger.LogInfo("Verification saved as Draft.");
            }
            else
            {
                await _buyerApiClient.UpdateVerificationRequestStatus(
                    request.Verification.VerificationRequestId,
                    Common.SUBMITTED,
                    cancellationToken);

                _logger.LogInfo("Verification submitted successfully.");
            }

            return true;
        }
    }
}