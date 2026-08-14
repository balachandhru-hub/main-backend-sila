using Buyer.Domain.Entities;
using Buyer.Infrastructure.Contracts.IRepository;
using MediatR;
using Microsoft.EntityFrameworkCore;
using SharedKernel.LoggerServices;
using Buyer.Domain.Dto;

namespace Buyer.Application.Features.Commands.Template
{
    public class UpdateVerificationTemplateQuestionCommandHandler
        : IRequestHandler<UpdateVerificationTemplateQuestionCommand, Guid>
    {
        private readonly IRepositoryWrapper _repository;
        private readonly ILoggerManager _logger;

        public UpdateVerificationTemplateQuestionCommandHandler(
            IRepositoryWrapper repository,
            ILoggerManager logger)
        {
            _repository = repository;
            _logger = logger;
        }

        public async Task<Guid> Handle(
            UpdateVerificationTemplateQuestionCommand request,
            CancellationToken cancellationToken)
        {
            var dto = request.VerificationTemplateQuestionDto;

            _logger.LogInfo(
                $"Processing verification template question. " +
                $"QuestionId: {dto.Id}, TemplateId: {dto.VerificationTemplateId}");

      
            //  Validate Template
          
            var template = await _repository.VerificationTemplate
                .FindByCondition(x => x.Id == dto.VerificationTemplateId)
                .FirstOrDefaultAsync(cancellationToken);

            if (template == null)
            {
                _logger.LogError(
                    $"Verification template not found. " +
                    $"TemplateId: {dto.VerificationTemplateId}");

                throw new KeyNotFoundException(
                    $"Verification template not found: {dto.VerificationTemplateId}");
            }

            // CREATE NEW QUESTION

            if (!dto.Id.HasValue || dto.Id == Guid.Empty)
            {
                _logger.LogInfo(
                    "QuestionId is empty. Creating a new verification template question.");

                var newQuestion = new VerificationTemplateQuestion
                {
                    Id = Guid.NewGuid(),
                    VerificationTemplateId = dto.VerificationTemplateId,
                    Question = dto.Question,
                    QuestionType = dto.QuestionType,
                    IsRequired = dto.IsRequired,
                    DisplayOrder = dto.DisplayOrder
                };

                _repository.VerificationTemplateQuestion.Create(newQuestion);

                // Add options
                if (dto.Options != null && dto.Options.Any())
                {
                    foreach (var option in dto.Options)
                    {
                        var newOption = new VerificationTemplateQuestionOption
                        {
                            Id = Guid.NewGuid(),
                            VerificationTemplateQuestionId = newQuestion.Id,
                            OptionText = option.OptionText,
                            DisplayOrder = option.DisplayOrder
                        };

                        _repository.VerificationTemplateQuestionOptionRepository
                            .Create(newOption);
                    }
                }

                _repository.Save();

                _logger.LogInfo(
                    $"New verification template question created successfully. " +
                    $"QuestionId: {newQuestion.Id}");

                return newQuestion.Id;
            }

        
            // UPDATE EXISTING QUESTION
         
            var question = await _repository.VerificationTemplateQuestion
                .FindByCondition(x =>
                    x.Id == dto.Id.Value &&
                    x.VerificationTemplateId == dto.VerificationTemplateId)
                .FirstOrDefaultAsync(cancellationToken);

            if (question == null)
            {
                _logger.LogError(
                    $"Verification template question not found. " +
                    $"QuestionId: {dto.Id}, TemplateId: {dto.VerificationTemplateId}");

                throw new KeyNotFoundException(
                    $"Verification template question not found: {dto.Id}");
            }

            // Update question details
            question.Question = dto.Question;
            question.QuestionType = dto.QuestionType;
            question.IsRequired = dto.IsRequired;
            question.DisplayOrder = dto.DisplayOrder;

            _repository.VerificationTemplateQuestion.Update(question);

          
            //  UPDATE / ADD / DELETE OPTIONS
          
            var existingOptions = await _repository
                .VerificationTemplateQuestionOptionRepository
                .FindByCondition(x =>
                    x.VerificationTemplateQuestionId == question.Id)
                .ToListAsync(cancellationToken);

            var requestOptions = dto.Options ?? new List<VerificationTemplateQuestionOptionDto>();

          
            // Delete options which are no longer present in request
        
            foreach (var existingOption in existingOptions)
            {
                var optionExistsInRequest = requestOptions.Any(x =>
                    x.Id.HasValue &&
                    x.Id.Value == existingOption.Id);

                if (!optionExistsInRequest)
                {
                    _repository.VerificationTemplateQuestionOptionRepository
                        .Delete(existingOption);
                }
            }

        
            // Add new options / Update existing options
          
            foreach (var optionDto in requestOptions)
            {
               
                if (!optionDto.Id.HasValue || optionDto.Id == Guid.Empty)
                {
                    var newOption = new VerificationTemplateQuestionOption
                    {
                        Id = Guid.NewGuid(),
                        VerificationTemplateQuestionId = question.Id,
                        OptionText = optionDto.OptionText,
                        DisplayOrder = optionDto.DisplayOrder
                    };

                    _repository.VerificationTemplateQuestionOptionRepository
                        .Create(newOption);

                    continue;
                }

                // Existing option
                var existingOption = existingOptions.FirstOrDefault(x =>
                    x.Id == optionDto.Id.Value);

                if (existingOption != null)
                {
                    existingOption.OptionText = optionDto.OptionText;
                    existingOption.DisplayOrder = optionDto.DisplayOrder;

                    _repository.VerificationTemplateQuestionOptionRepository
                        .Update(existingOption);
                }
            }

         
            _repository.Save();

            _logger.LogInfo(
                $"Verification template question updated successfully. " +
                $"QuestionId: {question.Id}");

            return question.Id;
        }
    }
}