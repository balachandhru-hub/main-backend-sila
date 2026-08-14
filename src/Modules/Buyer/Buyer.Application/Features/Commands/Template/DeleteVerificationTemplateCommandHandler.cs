using Buyer.Infrastructure.Contracts.IRepository;
using MediatR;
using Microsoft.EntityFrameworkCore;
using SharedKernel.LoggerServices;

namespace Buyer.Application.Features.Commands.Template
{
    public class DeleteVerificationTemplateCommandHandler
        : IRequestHandler<DeleteVerificationTemplateCommand, bool>
    {
        private readonly IRepositoryWrapper _repository;
        private readonly ILoggerManager _logger;

        public DeleteVerificationTemplateCommandHandler(
            IRepositoryWrapper repository,
            ILoggerManager logger)
        {
            _repository = repository;
            _logger = logger;
        }

        public async Task<bool> Handle(
            DeleteVerificationTemplateCommand request,
            CancellationToken cancellationToken)
        {
            var templateId = request.TemplateId;

            _logger.LogInfo(
                $"Deleting verification template. TemplateId: {templateId}");

         

            var template = await _repository.VerificationTemplate
                .FindByCondition(x => x.Id == templateId)
                .FirstOrDefaultAsync(cancellationToken);

            if (template == null)
            {
                _logger.LogError(
                    $"Verification template not found. TemplateId: {templateId}");

                throw new KeyNotFoundException(
                    $"Verification template not found: {templateId}");
            }

          

            var questions = await _repository.VerificationTemplateQuestion
                .FindByCondition(x =>
                    x.VerificationTemplateId == templateId)
                .ToListAsync(cancellationToken);

            _logger.LogInfo(
                $"Found {questions.Count} questions for template. " +
                $"TemplateId: {templateId}");


            foreach (var question in questions)
            {
                var options = await _repository
                    .VerificationTemplateQuestionOptionRepository
                    .FindByCondition(x =>
                        x.VerificationTemplateQuestionId == question.Id)
                    .ToListAsync(cancellationToken);

                foreach (var option in options)
                {
                    _repository.VerificationTemplateQuestionOptionRepository
                        .Delete(option);
                }
            }

            foreach (var question in questions)
            {
                _repository.VerificationTemplateQuestion
                    .Delete(question);
            }

            _repository.VerificationTemplate.Delete(template);

            _repository.Save();

            _logger.LogInfo(
                $"Verification template and all related questions/options " +
                $"deleted successfully. TemplateId: {templateId}");

            return true;
        }
    }
}