using Buyer.Domain.Dto;
using Buyer.Infrastructure.Contracts.IRepository;
using MediatR;
using Microsoft.EntityFrameworkCore;
using SharedKernel.ExceptionHandler;

namespace Buyer.Application.Features.Queries.Template
{
    public class GetVerificationTemplateByIdQueryHandler
        : IRequestHandler<GetVerificationTemplateByIdQuery, VerificationTemplateResponseDto>
    {
        private readonly IRepositoryWrapper _repository;

        public GetVerificationTemplateByIdQueryHandler(
            IRepositoryWrapper repository)
        {
            _repository = repository;
        }

        public async Task<VerificationTemplateResponseDto> Handle(
            GetVerificationTemplateByIdQuery request,
            CancellationToken cancellationToken)
        {
            // Buyer Template
            var buyerTemplate = await _repository.VerificationTemplate
                .FindByCondition(x => x.Id == request.TemplateId && x.IsActive)
                .FirstOrDefaultAsync(cancellationToken);

            if (buyerTemplate != null)
            {
                var questions = await _repository.VerificationTemplateQuestion
                    .FindByCondition(x => x.VerificationTemplateId == buyerTemplate.Id)
                    .OrderBy(x => x.DisplayOrder)
                    .ToListAsync(cancellationToken);

                var questionDtos = new List<VerificationTemplateQuestionDto>();

                foreach (var question in questions)
                {
                    var options = await _repository.VerificationTemplateQuestionOptionRepository
                        .FindByCondition(x => x.VerificationTemplateQuestionId == question.Id)
                        .Select(x => x.OptionText)
                        .ToListAsync(cancellationToken);

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
                    TemplateType = "Buyer",
                    Questions = questionDtos
                };
            }

            // Default Template
            var defaultTemplate = await _repository.DefaultVerificationTemplateRepository
                .FindByCondition(x => x.Id == request.TemplateId && x.IsActive)
                .FirstOrDefaultAsync(cancellationToken);

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
                    TemplateType = "Default",
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

            throw new NotFoundCustomException(
                "Template not found.",
                "Verification Template not found.");
        }
    }
}