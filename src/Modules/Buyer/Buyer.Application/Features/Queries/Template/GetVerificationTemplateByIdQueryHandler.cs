using Buyer.Domain.Common;
using Buyer.Domain.Dto;
using Buyer.Infrastructure.Contracts.IRepository;
using MediatR;
using Microsoft.EntityFrameworkCore;
using SharedKernel.ExceptionHandler;
using SharedKernel.LoggerServices;

namespace Buyer.Application.Features.Queries.Template
{
    public class GetVerificationTemplateByIdQueryHandler
        : IRequestHandler<GetVerificationTemplateByIdQuery, VerificationTemplateResponseDto>
    {
        private readonly IRepositoryWrapper _repository;
        private readonly ILoggerManager _logger;

        public GetVerificationTemplateByIdQueryHandler(
            IRepositoryWrapper repository,
            ILoggerManager logger)
        {
            _repository = repository;
            _logger = logger;
        }

        public async Task<VerificationTemplateResponseDto> Handle(
            GetVerificationTemplateByIdQuery request,
            CancellationToken cancellationToken)
        {
            _logger.LogInfo($"Fetching verification template by ID: {request.TemplateId}");
            
            var buyerTemplate = await _repository.VerificationTemplate
                .FindByCondition(x => x.Id == request.TemplateId && x.IsActive)
                .FirstOrDefaultAsync(cancellationToken);
                _logger.LogInfo($"Fetched buyer template for TemplateId: {request.TemplateId}");

            if (buyerTemplate != null)
            {
                var questions = await _repository.VerificationTemplateQuestion
                    .FindByCondition(x => x.VerificationTemplateId == buyerTemplate.Id)
                    .OrderBy(x => x.DisplayOrder)
                    .ToListAsync(cancellationToken);
                    _logger.LogInfo($"Fetched {questions.Count} questions for TemplateId: {buyerTemplate.Id}");

                var questionDtos = new List<VerificationTemplateQuestionDto>();

                foreach (var question in questions)
                {
                    var options = await _repository.VerificationTemplateQuestionOptionRepository
                        .FindByCondition(x => x.VerificationTemplateQuestionId == question.Id)
                        .Select(x => x.OptionText)
                        .ToListAsync(cancellationToken);
                        _logger.LogInfo($"Fetched {options.Count} options for QuestionId: {question.Id}");

                    questionDtos.Add(new VerificationTemplateQuestionDto
                    {
                        QuestionId = question.Id,
                        Question = question.Question,
                        QuestionType = question.QuestionType,
                        DisplayOrder = question.DisplayOrder,
                        IsRequired = question.IsRequired,
                        Options = options
                    });
                }

                return new VerificationTemplateResponseDto
                {
                    TemplateId = buyerTemplate.Id,
                    TemplateCode = buyerTemplate.TemplateCode,
                    TemplateName = buyerTemplate.TemplateName,
                    TemplateType = Common.BUYER,
                    Questions = questionDtos
                };
            }

            // Default Template
            var defaultTemplate = await _repository.DefaultVerificationTemplateRepository
                .FindByCondition(x => x.Id == request.TemplateId && x.IsActive)
                .FirstOrDefaultAsync(cancellationToken);
                _logger.LogInfo($"Fetched default template for TemplateId: {request.TemplateId}");

            if (defaultTemplate != null)
            {
                var questions = await _repository.DefaultVerificationTemplateQuestionRepository
                    .FindByCondition(x => x.DefaultVerificationTemplateId == defaultTemplate.Id)
                    .OrderBy(x => x.DisplayOrder)
                    .ToListAsync(cancellationToken);

                return new VerificationTemplateResponseDto
                {
                    TemplateId = defaultTemplate.Id,
                    TemplateCode = defaultTemplate.TemplateCode,
                    TemplateName = defaultTemplate.TemplateName,
                    TemplateType = Common.DEFAULT,
                    Questions = questions.Select(x => new VerificationTemplateQuestionDto
                    {
                        QuestionId = x.Id,
                        Question = x.Question,
                        QuestionKey = x.QuestionKey,
                        QuestionType = x.QuestionType,
                        DisplayOrder = x.DisplayOrder
                    }).ToList()
                };
            }
            _logger.LogError($"Verification template not found for TemplateId: {request.TemplateId}");

            throw new NotFoundCustomException(
                "Template not found.",
                "Verification Template not found.");
        }
    }
}