using MediatR;
using Supplier.Application.Features.Commands.Asset;
using Supplier.Domain.Entities;
using Supplier.Infrastructure.Contracts.IRepository;
using SharedKernel.LoggerServices;
using Microsoft.AspNetCore.Http.HttpResults;
using SharedKernel.ExceptionHandler;
using Supplier.Domain.Common;

namespace Supplier.Application.Features.Commands.SupplierAnswers
{
    public class SaveSupplierRFQAnswerCommandHandler
        : IRequestHandler<SaveSupplierRFQAnswerCommand, bool>
    {
        private readonly IRepositoryWrapper _repository;
        private readonly ILoggerManager _logger;
        private readonly IMediator _mediator;

        public SaveSupplierRFQAnswerCommandHandler(
            IRepositoryWrapper repository,
            ILoggerManager logger,
            IMediator mediator)
        {
            _repository = repository;
            _logger = logger;
            _mediator = mediator;
        }

        public async Task<bool> Handle(
            SaveSupplierRFQAnswerCommand request,
            CancellationToken cancellationToken)
        {
            _logger.LogInfo(
                $"Saving supplier answers for SupplierRFQ : {request.Answer.SupplierRFQId}");

            var supplierRFQ = _repository.SupplierRFQ
                .FindFirstByCondition(x =>
                    x.BuyerRFQId  == request.Answer.SupplierRFQId &&
                    x.IsActive);

            if (supplierRFQ == null)
            {
                _logger.LogError($"Supplier RFQ not found for SupplierRFQId: {request.Answer.SupplierRFQId}");
                throw new NotFoundCustomException(
                    "Supplier RFQ not found.",
                    $"Supplier RFQ with Id {request.Answer.SupplierRFQId} does not exist.");
            }
                

            foreach (var answer in request.Answer.Answers)
            {
                _logger.LogInfo($"Processing answer for RFQQuestionId: {answer.RFQQuestionId} in SupplierRFQ: {request.Answer.SupplierRFQId}");
                var existingAnswer = _repository.RFQQuestionAnswer
                    .FindFirstByCondition(x =>
                        x.SupplierRFQId == supplierRFQ.Id &&
                        x.RFQQuestionId == answer.RFQQuestionId &&
                        x.IsActive);

                if (existingAnswer == null)
                {
                _logger.LogInfo($"Creating new answer for QuestionId: {answer.RFQQuestionId} in SupplierRFQ: {request.Answer.SupplierRFQId}");
                    existingAnswer = new SupplierRFQQuestionAnswer
                    {
                        Id = Guid.NewGuid(),
                        SupplierRFQId = supplierRFQ.Id,
                        BuyerRFQId = supplierRFQ.BuyerRFQId,
                        RFQQuestionId = answer.RFQQuestionId,
                        RFQNumber = supplierRFQ.RFQNumber,
                        SupplierId = supplierRFQ.SupplierId
                    };

                    await _repository.RFQQuestionAnswer.CreateAsync(existingAnswer);
                }

                existingAnswer.Answer = answer.Answer;
                existingAnswer.QuestionOptionId = answer.QuestionOptionId;
                existingAnswer.AnsweredOn = DateTime.UtcNow;

                if (answer.Attachment != null)
                {
                    _logger.LogInfo($"Uploading attachment for QuestionId: {answer.RFQQuestionId} in SupplierRFQ: {request.Answer.SupplierRFQId}");
                    Guid assetId = await _mediator.Send(
                        new UploadAssetCommand(answer.Attachment));

                    existingAnswer.AssetId = assetId;
                }

               _logger.LogInfo($"Updating answer for QuestionId: {answer.RFQQuestionId} in SupplierRFQ: {request.Answer.SupplierRFQId}");
                var existingOptions = _repository.RFQQuestionAnswerOption
                    .FindByCondition(x =>
                        x.SupplierRFQQuestionAnswerId == existingAnswer.Id &&
                        x.IsActive)
                    .ToList();

                foreach (var option in existingOptions)
                {
                    _logger.LogInfo($"Deactivating existing option with Id: {option.Id} for QuestionId: {answer.RFQQuestionId} in SupplierRFQ: {request.Answer.SupplierRFQId}");
                    option.IsActive = false;
                }

                
                var selectedOptionIds = new List<Guid>();

               
                if (answer.QuestionOptionId.HasValue)
                {
                    _logger.LogInfo($"Adding selected option Id: {answer.QuestionOptionId.Value} for QuestionId: {answer.RFQQuestionId} in SupplierRFQ: {request.Answer.SupplierRFQId}");
                    selectedOptionIds.Add(answer.QuestionOptionId.Value);
                }

             
                if (answer.QuestionOptionIds != null &&
                    answer.QuestionOptionIds.Any())
                {
                    _logger.LogInfo($"Adding selected option Ids: {string.Join(", ", answer.QuestionOptionIds)} for QuestionId: {answer.RFQQuestionId} in SupplierRFQ: {request.Answer.SupplierRFQId}");
                    selectedOptionIds.AddRange(answer.QuestionOptionIds);
                }

              
                foreach (var optionId in selectedOptionIds.Distinct())
                {
                    _logger.LogInfo($"Creating new option for QuestionId: {answer.RFQQuestionId} in SupplierRFQ: {request.Answer.SupplierRFQId}");
                    await _repository.RFQQuestionAnswerOption.CreateAsync(
                        new SupplierRFQAnswerOption
                        {
                            Id = Guid.NewGuid(),
                            SupplierRFQQuestionAnswerId = existingAnswer.Id,
                            RFQQuestionOptionId = optionId
                        });
                }
            }
       
            await _repository.SaveAsync();

            _logger.LogInfo(
                $"Supplier answers saved successfully for SupplierRFQ : {request.Answer.SupplierRFQId}");

            return true;
        }
    }
}