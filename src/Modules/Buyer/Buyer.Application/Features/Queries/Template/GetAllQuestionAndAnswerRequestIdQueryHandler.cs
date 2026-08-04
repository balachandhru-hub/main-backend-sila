using System.Security.Claims;
using Buyer.Application.Contracts;
using Buyer.Domain.Common;
using Buyer.Domain.Dto;
using Buyer.Infrastructure.Contracts.IRepository;
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using SharedKernel.ExceptionHandler;
using SharedKernel.LoggerServices;

namespace Buyer.Application.Features.Queries.SupplierVerificationRequest
{
    public class GetSupplierVerificationRequestDetailbyRequestIdQueryHandler
        : IRequestHandler<GetSupplierVerificationRequestDetailbyRequestIdQuery, SupplierVerificationRequestDetailQuestinandAnswerDto>
    {
        private readonly IRepositoryWrapper _repository;
        private readonly ILoggerManager _logger;
        private readonly ISupplierApiClient _supplierApiClient;
        private readonly IHttpContextAccessor _httpContextAccessor;

        public GetSupplierVerificationRequestDetailbyRequestIdQueryHandler(
            IRepositoryWrapper repository,
            ILoggerManager logger,
            ISupplierApiClient supplierApiClient,
            IHttpContextAccessor httpContextAccessor)
        {
            _repository = repository;
            _logger = logger;
            _supplierApiClient = supplierApiClient;
            _httpContextAccessor = httpContextAccessor;
        }

        public async Task<SupplierVerificationRequestDetailQuestinandAnswerDto> Handle(
            GetSupplierVerificationRequestDetailbyRequestIdQuery request,
            CancellationToken cancellationToken)
        {
            _logger.LogInfo($"Fetching Supplier Verification Request : {request.RequestId}");

            var result = await _repository.SupplierVerificationRequest
                .FindByCondition(x => x.Id == request.RequestId && x.IsActive)
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
                throw new NotFoundCustomException(
                    "Supplier Verification Request not found.",
                    "Invalid Request Id.");
            }

            var questions = await _repository.VerificationTemplateQuestion
                .FindByCondition(x => x.VerificationTemplateId == result.TemplateId)
                .OrderBy(x => x.DisplayOrder)
                .ToListAsync(cancellationToken);

            result.Questions = new List<SupplierVerificationQuestionAndAnswerDto>();

            foreach (var question in questions)
            {
                var questionDto = new SupplierVerificationQuestionAndAnswerDto
                {
                    VerificationTemplateQuestionId = question.Id,
                    Question = question.Question,
                    QuestionType = question.QuestionType,
                    IsRequired = question.IsRequired,
                    DisplayOrder = question.DisplayOrder
                };

                questionDto.Options = await _repository
                    .VerificationTemplateQuestionOptionRepository
                    .FindByCondition(x => x.VerificationTemplateQuestionId == question.Id)
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


            var roleIdClaim = _httpContextAccessor.HttpContext?.User?
    .FindFirst("roleId")?.Value;

            Guid.TryParse(roleIdClaim, out Guid roleId);

            bool showAnswers = false;


            if (roleId == Common.SUPPLIER_ROLE_ID)
            {
                showAnswers =
                    result.Status.Equals(Common.SUBMITTED, StringComparison.OrdinalIgnoreCase) ||
                    result.Status.Equals(Common.DEFAULT, StringComparison.OrdinalIgnoreCase);
            }

            else if (roleId == Common.BUYER_ROLE_ID)
            {
                showAnswers =
                    !result.Status.Equals(Common.DRAFT, StringComparison.OrdinalIgnoreCase) &&
                    !result.Status.Equals(Common.PENDING, StringComparison.OrdinalIgnoreCase) &&
                    !result.Status.Equals(Common.DEFAULT, StringComparison.OrdinalIgnoreCase);
            }

            if (showAnswers)
            {
                var supplierAnswers =
                    await _supplierApiClient.GetQuestionsAnswersForSupplier(
                        result.RequestId,
                        cancellationToken);

                if (supplierAnswers?.Questions != null)
                {
                    foreach (var question in result.Questions)
                    {
                        var answer = supplierAnswers.Questions.FirstOrDefault(x =>
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

            _logger.LogInfo($"Supplier Verification Request fetched successfully : {request.RequestId}");

            return result;
        }
    }
}