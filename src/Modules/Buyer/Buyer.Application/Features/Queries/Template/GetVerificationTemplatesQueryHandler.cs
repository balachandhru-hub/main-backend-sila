using Buyer.Domain.Common;
using Buyer.Domain.Dto;
using Buyer.Infrastructure.Contracts.IRepository;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Buyer.Application.Features.Queries.Template
{
    public class GetVerificationTemplatesQueryHandler
        : IRequestHandler<GetVerificationTemplatesQuery, List<VerificationTemplateResponseDto>>
    {
        private readonly IRepositoryWrapper _repository;

        public GetVerificationTemplatesQueryHandler(
            IRepositoryWrapper repository)
        {
            _repository = repository;
        }

        public async Task<List<VerificationTemplateResponseDto>> Handle(
            GetVerificationTemplatesQuery request,
            CancellationToken cancellationToken)
        {
            var result = new List<VerificationTemplateResponseDto>();
               var buyerId = await _repository.BuyerBusinessProfile
                .FindByCondition(x => x.OrganizationId == request.OrganizationId)
                .Select(x => x.Id)
                .FirstOrDefaultAsync(cancellationToken);

            if (buyerId == Guid.Empty)
            {
                throw new KeyNotFoundException(
                    "Buyer not found for the given OrganizationId.");
            }

            // Default Templates
            var defaultTemplates = await _repository.DefaultVerificationTemplateRepository
                .FindByCondition(x => x.IsActive)
                .OrderBy(x => x.TemplateCode)
                .ToListAsync(cancellationToken);

            foreach (var template in defaultTemplates)
            {
                var questions = await _repository.DefaultVerificationTemplateQuestionRepository
                    .FindByCondition(x => x.DefaultVerificationTemplateId == template.Id)
                    .OrderBy(x => x.DisplayOrder)
                    .ToListAsync(cancellationToken);

                result.Add(new VerificationTemplateResponseDto
                {
                    TemplateId = template.Id,
                    TemplateCode = template.TemplateCode,
                    TemplateName = template.TemplateName,
                    TemplateType = Common.DEFAULT,
                    Questions = questions.Select(x => new VerificationTemplateQuestionDto
                    {
                        QuestionId = x.Id,
                        Question = x.Question,
                        QuestionKey = x.QuestionKey,
                        QuestionType = x.QuestionType,
                        DisplayOrder = x.DisplayOrder
                    }).ToList()
                });
            }

            // Buyer Templates
            var buyerTemplates = await _repository.VerificationTemplate
                .FindByCondition(x => x.BuyerId == buyerId && x.IsActive)
                .OrderBy(x => x.TemplateCode)
                .ToListAsync(cancellationToken);

            foreach (var template in buyerTemplates)
            {
                var questions = await _repository.VerificationTemplateQuestion
                    .FindByCondition(x => x.VerificationTemplateId == template.Id)
                    .OrderBy(x => x.DisplayOrder)
                    .ToListAsync(cancellationToken);

                result.Add(new VerificationTemplateResponseDto
                {
                    TemplateId = template.Id,
                    TemplateCode = template.TemplateCode,
                    TemplateName = template.TemplateName,
                    TemplateType = Common.BUYER,
                    Questions = questions.Select(x => new VerificationTemplateQuestionDto
                    {
                        QuestionId = x.Id,
                        Question = x.Question,
                        QuestionType = x.QuestionType,
                        DisplayOrder = x.DisplayOrder
                    }).ToList()
                });
            }

            return result;
        }
    }
}