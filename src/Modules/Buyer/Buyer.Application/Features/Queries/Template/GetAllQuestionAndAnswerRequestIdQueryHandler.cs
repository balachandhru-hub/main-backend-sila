using Buyer.Application.Contracts;
using Buyer.Domain.Common;
using Buyer.Domain.Dto;
using Buyer.Infrastructure.Contracts.IRepository;
using MediatR;
using Microsoft.EntityFrameworkCore;
using SharedKernel.ExceptionHandler;
using SharedKernel.LoggerServices;

namespace Buyer.Application.Features.Queries.SupplierVerificationRequest
{
    public class GetSupplierVerificationRequestDetailbyRequestIdQueryHandler
        : IRequestHandler<
            GetSupplierVerificationRequestDetailbyRequestIdQuery,
            SupplierVerificationRequestDetailQuestinandAnswerDto>
    {
        private readonly IRepositoryWrapper _repository;
        private readonly ILoggerManager _logger;
        private readonly ISupplierApiClient _supplierApiClient;

        public GetSupplierVerificationRequestDetailbyRequestIdQueryHandler(
            IRepositoryWrapper repository,
            ILoggerManager logger,
            ISupplierApiClient supplierApiClient)
        {
            _repository = repository;
            _logger = logger;
            _supplierApiClient = supplierApiClient;
        }

        public async Task<SupplierVerificationRequestDetailQuestinandAnswerDto> Handle(
            GetSupplierVerificationRequestDetailbyRequestIdQuery request,
            CancellationToken cancellationToken)
        {
            _logger.LogInfo(
                $"Fetching Supplier Verification Request : {request.RequestId}");



            var result = await _repository.SupplierVerificationRequest
                .FindByCondition(x =>
                    x.Id == request.RequestId &&
                    x.IsActive)
                .Select(x => new SupplierVerificationRequestDetailQuestinandAnswerDto
                {
                    RequestId = x.Id,
                    RFQId = x.RFQId,
                    RFQNumber = x.RFQNumber,
                    BuyerId = x.BuyerOrganizationId,
                    SupplierOrganizationId = x.SupplierOrganizationId,
                    TemplateId = x.RFQVerificationTemplateId,
                    Status = x.Status,
                    Remarks = x.Remarks,
                    DueDate = x.DueDate
                })
                .FirstOrDefaultAsync(cancellationToken);

            if (result == null)
            {
                _logger.LogError(
                    $"Supplier Verification Request not found. " +
                    $"RequestId : {request.RequestId}");

                throw new NotFoundCustomException(
                    "Supplier Verification Request not found.",
                    "Invalid Request Id.");
            }



            if (request.RoleId == Common.BUYER_ROLE_ID)
            {
                var supplier = await _supplierApiClient.GetSupplierById(
                    result.SupplierOrganizationId,
                    cancellationToken);

                if (supplier != null)
                {
                    result.SNID = supplier.BusinessProfile?.SNID;
                    result.OrganizationName =
                        supplier.BusinessProfile?.OrganizationName;
                    result.Description =
                        supplier.BusinessProfile?.Description;
                }
            }
            else if (request.RoleId == Common.SUPPLIER_ROLE_ID)
            {
                var buyer = await _repository.BuyerBusinessProfile
                    .FindByCondition(x =>
                        x.OrganizationId == result.BuyerId)
                    .Select(x => new
                    {
                        x.SNID,
                        x.OrganizationName,
                        x.Description
                    })
                    .FirstOrDefaultAsync(cancellationToken);

                if (buyer != null)
                {
                    result.SNID = buyer.SNID;
                    result.OrganizationName = buyer.OrganizationName;
                    result.Description = buyer.Description;
                }
            }


            var questions = await _repository.VerificationTemplateQuestion
                .FindByCondition(x =>
                    x.VerificationTemplateId == result.TemplateId &&
                    x.IsActive)
                .OrderBy(x => x.DisplayOrder)
                .ToListAsync(cancellationToken);

            result.Questions =
                new List<SupplierVerificationQuestionAndAnswerDto>();

            if (questions.Any())
            {
                // -----------------------------------------------------
                // Custom Verification Template Questions
                // -----------------------------------------------------

                foreach (var question in questions)
                {
                    var questionDto =
                        new SupplierVerificationQuestionAndAnswerDto
                        {
                            VerificationTemplateQuestionId = question.Id,
                            Question = question.Question,
                            QuestionType = question.QuestionType,
                            IsRequired = question.IsRequired,
                            DisplayOrder = question.DisplayOrder
                        };

                    questionDto.Options = await _repository
                        .VerificationTemplateQuestionOptionRepository
                        .FindByCondition(x =>
                            x.VerificationTemplateQuestionId == question.Id)
                        .OrderBy(x => x.DisplayOrder)
                        .Select(x => new SupplierVerificationQuestionOptionDto
                        {
                            Id = x.Id,
                            OptionText = x.OptionText,
                            DisplayOrder = x.DisplayOrder
                        })
                        .ToListAsync(cancellationToken);

                    result.Questions.Add(questionDto);
                }
            }
            else
            {


                _logger.LogInfo(
                    $"No custom template questions found for TemplateId : " +
                    $"{result.TemplateId}. Checking default template questions.");

                var defaultQuestions =
                    await _repository.DefaultVerificationTemplateQuestionRepository
                        .FindByCondition(x =>
                            x.DefaultVerificationTemplateId == result.TemplateId &&
                            x.IsActive)
                        .OrderBy(x => x.DisplayOrder)
                        .ToListAsync(cancellationToken);

                foreach (var question in defaultQuestions)
                {
                    var questionDto =
                        new SupplierVerificationQuestionAndAnswerDto
                        {
                            VerificationTemplateQuestionId = question.Id,
                            Question = question.Question,
                            QuestionType = question.QuestionType,
                            IsRequired = false,
                            DisplayOrder = question.DisplayOrder
                        };


                    questionDto.Options =
                        new List<SupplierVerificationQuestionOptionDto>();

                    result.Questions.Add(questionDto);
                }

                _logger.LogInfo(
                    $"Default template questions fetched successfully. " +
                    $"TemplateId : {result.TemplateId}, " +
                    $"QuestionCount : {defaultQuestions.Count}");
            }





            bool showAnswers = false;

            if (request.RoleId == Common.SUPPLIER_ROLE_ID)
            {
                showAnswers =
                    result.Status.Equals(
                        Common.SUBMITTED,
                        StringComparison.OrdinalIgnoreCase)
                    ||
                    result.Status.Equals(
                        Common.DEFAULT,
                        StringComparison.OrdinalIgnoreCase);
            }
            else if (request.RoleId == Common.BUYER_ROLE_ID)
            {
                showAnswers =
                    !result.Status.Equals(
                        Common.DRAFT,
                        StringComparison.OrdinalIgnoreCase)
                    &&
                    !result.Status.Equals(
                        Common.PENDING,
                        StringComparison.OrdinalIgnoreCase)
                    &&
                    !result.Status.Equals(
                        Common.DEFAULT,
                        StringComparison.OrdinalIgnoreCase);
            }



            if (showAnswers)
            {
                try
                {
                    var supplierAnswers =
                        await _supplierApiClient.GetQuestionsAnswersForSupplier(
                            result.RequestId,
                            cancellationToken);

                    if (supplierAnswers?.Questions != null)
                    {
                        foreach (var question in result.Questions)
                        {
                            var answer =
                                supplierAnswers.Questions.FirstOrDefault(x =>
                                    x.VerificationTemplateQuestionId ==
                                    question.VerificationTemplateQuestionId);

                            if (answer != null)
                            {
                                question.Answer = answer.Answer;
                                question.AssetId = answer.AssetId;

                                question.VerificationTemplateQuestionOptionId =
                                    answer.VerificationTemplateQuestionOptionId;
                            }
                        }
                    }
                }
                catch (BadRequestCustomException)
                {

                    _logger.LogInfo(
                        $"No supplier answers available for RequestId : " +
                        $"{result.RequestId}. Returning questions only.");
                }
            }



            _logger.LogInfo(
                $"Supplier Verification Request fetched successfully : " +
                $"{request.RequestId}");

            return result;
        }
    }
}