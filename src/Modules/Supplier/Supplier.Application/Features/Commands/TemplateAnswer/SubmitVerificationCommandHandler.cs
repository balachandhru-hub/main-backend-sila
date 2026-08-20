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
            var verificationRequestId =
                request.Verification.VerificationRequestId;

            _logger.LogInfo(
                $"Supplier Verification Started. RequestId : {verificationRequestId}");

            // 1. Get verification request from Buyer
            var verificationRequest =
                await _buyerApiClient.GetSupplierVerificationRequestDetail(
                    verificationRequestId,
                    cancellationToken);

            _logger.LogInfo(
                $"Supplier Verification Request fetched successfully. " +
                $"RequestId : {verificationRequestId}");

            if (verificationRequest == null)
            {
                _logger.LogError(
                    $"Supplier Verification Request not found. " +
                    $"RequestId : {verificationRequestId}");

                throw new NotFoundCustomException(
                    "Supplier Verification Request not found.",
                    "Invalid Supplier Verification Request.");
            }

            // 2. Check already submitted
            if (verificationRequest.Status.Equals(
                    Common.SUBMITTED,
                    StringComparison.OrdinalIgnoreCase))
            {
                _logger.LogError(
                    $"Verification already submitted. " +
                    $"RequestId : {verificationRequestId}");

                throw new BadRequestCustomException(
                    "Verification already submitted.",
                    "You cannot modify submitted verification.");
            }

            // 3. Save answers only if answers are provided
            if (request.Verification.Answers != null &&
                request.Verification.Answers.Any())
            {
                foreach (var answer in request.Verification.Answers)
                {
                    Guid? assetId = null;

                    // Upload attachment if provided
                    if (answer.Attachment != null)
                    {
                        assetId = await _mediator.Send(
                            new UploadAssetCommand(answer.Attachment),
                            cancellationToken);

                        _logger.LogInfo(
                            $"Asset uploaded successfully. " +
                            $"QuestionId : {answer.VerificationTemplateQuestionId}, " +
                            $"RequestId : {verificationRequestId}, " +
                            $"AssetId : {assetId}");
                    }

                    // Check existing answer
                    var existingAnswer =
                        await _repository.SupplierVerificationAnswer
                            .FindByCondition(x =>
                                x.SupplierVerificationRequestId ==
                                    verificationRequestId &&
                                x.VerificationTemplateQuestionId ==
                                    answer.VerificationTemplateQuestionId)
                            .FirstOrDefaultAsync(cancellationToken);

                    if (existingAnswer == null)
                    {
                        // Create new answer
                        existingAnswer = new SupplierVerificationAnswer
                        {
                            Id = Guid.NewGuid(),
                            SupplierVerificationRequestId =
                                verificationRequestId,
                            SupplierId = request.Verification.SupplierId,
                            TemplateId = answer.TemplateId,
                            VerificationTemplateQuestionId =
                                answer.VerificationTemplateQuestionId,
                            VerificationTemplateQuestionOptionId =
                                answer.VerificationTemplateQuestionOptionId,
                            Answer = answer.Answer,
                            AssetId = assetId,
                            AnsweredOn = DateTime.UtcNow
                        };

                        await _repository.SupplierVerificationAnswer
                            .CreateAsync(existingAnswer);
                    }
                    else
                    {
                        // Update existing answer
                        existingAnswer.Answer = answer.Answer;

                        existingAnswer.TemplateId =
                            answer.TemplateId;

                        existingAnswer.VerificationTemplateQuestionOptionId =
                            answer.VerificationTemplateQuestionOptionId;

                        if (assetId != null)
                        {
                            existingAnswer.AssetId = assetId;
                        }

                        existingAnswer.AnsweredOn = DateTime.UtcNow;

                        _repository.SupplierVerificationAnswer
                            .Update(existingAnswer);
                    }
                }

                await _repository.SaveAsync();

                _logger.LogInfo(
                    $"Verification answers saved successfully. " +
                    $"RequestId : {verificationRequestId}");
            }
            else
            {
                // Default template / no questions
                _logger.LogInfo(
                    $"No verification answers provided. " +
                    $"Submitting verification without answers. " +
                    $"RequestId : {verificationRequestId}");
            }

            // 4. Update verification request status
            if (request.Verification.Status.Equals(
                    Common.DRAFT,
                    StringComparison.OrdinalIgnoreCase))
            {
                await _buyerApiClient.UpdateVerificationRequestStatus(
                    verificationRequestId,
                    Common.DRAFT,
                    cancellationToken);

                _logger.LogInfo(
                    $"Verification saved as Draft. " +
                    $"RequestId : {verificationRequestId}");
            }
            else
            {
                await _buyerApiClient.UpdateVerificationRequestStatus(
                    verificationRequestId,
                    Common.SUBMITTED,
                    cancellationToken);

                _logger.LogInfo(
                    $"Verification submitted successfully. " +
                    $"RequestId : {verificationRequestId}");
            }

            return true;
        }
    }
}