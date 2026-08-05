using Buyer.Domain.Entities;
using Buyer.Infrastructure.Contracts.IRepository;
using MediatR;
using Microsoft.EntityFrameworkCore;
using SharedKernel.LoggerServices;

namespace Buyer.Application.Features.Commands.Template
{
    public class CreateVerificationTemplateQuestionCommandHandler
        : IRequestHandler<CreateVerificationTemplateQuestionCommand, Guid>
    {
        private readonly IRepositoryWrapper _repository;
        private readonly ILoggerManager _logger;

        public CreateVerificationTemplateQuestionCommandHandler(
            IRepositoryWrapper repository,
            ILoggerManager logger)
        {
            _repository = repository;
            _logger = logger;
        }

        public async Task<Guid> Handle(
            CreateVerificationTemplateQuestionCommand request,
            CancellationToken cancellationToken)
        {
            _logger.LogInfo("Creating verification template question.");

            var dto = request.VerificationTemplateQuestionDto;

            var template = await _repository.VerificationTemplate
                .FindByCondition(x => x.Id == dto.VerificationTemplateId)
                .FirstOrDefaultAsync(cancellationToken);
                _logger.LogInfo($"Verification template fetched successfully. TemplateId : {dto.VerificationTemplateId}");

           

            var question = new VerificationTemplateQuestion
            {
                Id = Guid.NewGuid(),
                VerificationTemplateId = dto.VerificationTemplateId,
                Question = dto.Question,
                QuestionType = dto.QuestionType,
                IsRequired = dto.IsRequired,
                DisplayOrder = dto.DisplayOrder
               
            };

            _repository.VerificationTemplateQuestion.Create(question);

            // Save Options
            if (dto.Options != null && dto.Options.Any())
            {
                foreach (var option in dto.Options)
                {
                    _repository.VerificationTemplateQuestionOptionRepository.Create(
                        new VerificationTemplateQuestionOption
                        {
                            Id = Guid.NewGuid(),
                            VerificationTemplateQuestionId = question.Id,
                            OptionText = option,
                            DisplayOrder =dto.DisplayOrder
                        });
                }
            }

            _repository.Save();

            _logger.LogInfo($"Question created successfully. QuestionId : {question.Id}");

            return question.Id;
        }
    }
}