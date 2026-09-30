using Buyer.Domain.Dto;
using Buyer.Domain.Entities;
using Buyer.Infrastructure.Contracts.IRepository;
using MediatR;
using Microsoft.EntityFrameworkCore;
using SharedKernel.LoggerServices;

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


            //  CHECK TEMPLATE

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
                    "QuestionId is empty. Creating new question.");

                var newQuestion = new VerificationTemplateQuestion
                {
                    Id = Guid.NewGuid(),
                    VerificationTemplateId = dto.VerificationTemplateId,
                    Question = dto.Question,
                    QuestionType = dto.QuestionType,
                    IsRequired = dto.IsRequired,
                    DisplayOrder = dto.DisplayOrder
                };

                _repository.VerificationTemplateQuestion
                    .Create(newQuestion);

                // Add options if provided
                if (dto.Options != null && dto.Options.Any())
                {
                    foreach (var optionDto in dto.Options)
                    {
                        var newOption =
                            new VerificationTemplateQuestionOption
                            {
                                Id = Guid.NewGuid(),
                                VerificationTemplateQuestionId =
                                    newQuestion.Id,
                                OptionText = optionDto.OptionText,
                                DisplayOrder = optionDto.DisplayOrder
                            };

                        _repository.VerificationTemplateQuestionOptionRepository
                            .Create(newOption);
                    }
                }

                _repository.Save();

                _logger.LogInfo(
                    $"New question created successfully. " +
                    $"QuestionId: {newQuestion.Id}");

                return newQuestion.Id;
            }

            //  GET EXISTING QUESTION

            var question =
                await _repository.VerificationTemplateQuestion
                    .FindByCondition(x =>
                        x.Id == dto.Id.Value &&
                        x.VerificationTemplateId == dto.VerificationTemplateId)
                    .FirstOrDefaultAsync(cancellationToken);

            if (question == null)
            {
                _logger.LogError(
                    $"Verification template question not found. " +
                    $"QuestionId: {dto.Id}, " +
                    $"TemplateId: {dto.VerificationTemplateId}");

                throw new KeyNotFoundException(
                    $"Verification template question not found: {dto.Id}");
            }


            // DELETE QUESTION


            if (dto.IsDeleted)
            {
                // Find options belonging to this question
                var optionsToDelete =
                    await _repository
                        .VerificationTemplateQuestionOptionRepository
                        .FindByCondition(x =>
                            x.VerificationTemplateQuestionId == question.Id)
                        .ToListAsync(cancellationToken);

                // Delete options only when they exist
                if (optionsToDelete.Any())
                {
                    foreach (var option in optionsToDelete)
                    {
                        _repository
                            .VerificationTemplateQuestionOptionRepository
                            .Delete(option);
                    }
                }


                _repository.VerificationTemplateQuestion
                    .Delete(question);

                _repository.Save();

                _logger.LogInfo(
                    $"Verification template question deleted successfully. " +
                    $"QuestionId: {question.Id}");

                return question.Id;
            }


            // UPDATE QUESTION

            question.Question = dto.Question;
            question.QuestionType = dto.QuestionType;
            question.IsRequired = dto.IsRequired;
            question.DisplayOrder = dto.DisplayOrder;

            _repository.VerificationTemplateQuestion
                .Update(question);


            // GET EXISTING OPTIONS
            var existingOptions =
                await _repository
                    .VerificationTemplateQuestionOptionRepository
                    .FindByCondition(x =>
                        x.VerificationTemplateQuestionId == question.Id)
                    .ToListAsync(cancellationToken);

            var requestOptions =
                dto.Options ??
                new List<VerificationTemplateQuestionOptionDto>();


            //  DELETE OPTIONS NOT PRESENT IN REQUEST


            foreach (var existingOption in existingOptions)
            {
                var optionExistsInRequest = requestOptions.Any(x =>
                    x.Id.HasValue &&
                    x.Id.Value == existingOption.Id);

                if (!optionExistsInRequest)
                {
                    _logger.LogInfo(
                        $"Deleting option. " +
                        $"OptionId: {existingOption.Id}");

                    _repository
                        .VerificationTemplateQuestionOptionRepository
                        .Delete(existingOption);
                }
            }


            //  ADD NEW OPTIONS / UPDATE EXISTING OPTIONS

            foreach (var optionDto in requestOptions)
            {


                if (!optionDto.Id.HasValue ||
                    optionDto.Id == Guid.Empty)
                {
                    var newOption =
                        new VerificationTemplateQuestionOption
                        {
                            Id = Guid.NewGuid(),
                            VerificationTemplateQuestionId = question.Id,
                            OptionText = optionDto.OptionText,
                            DisplayOrder = optionDto.DisplayOrder
                        };

                    _repository
                        .VerificationTemplateQuestionOptionRepository
                        .Create(newOption);

                    _logger.LogInfo(
                        $"New option added. " +
                        $"QuestionId: {question.Id}");

                    continue;
                }


                // UPDATE EXISTING OPTION


                var existingOption =
                    existingOptions.FirstOrDefault(x =>
                        x.Id == optionDto.Id.Value);

                if (existingOption != null)
                {
                    existingOption.OptionText =
                        optionDto.OptionText;

                    existingOption.DisplayOrder =
                        optionDto.DisplayOrder;

                    _repository
                        .VerificationTemplateQuestionOptionRepository
                        .Update(existingOption);

                    _logger.LogInfo(
                        $"Option updated. " +
                        $"OptionId: {existingOption.Id}");
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