using MediatR;
using Microsoft.EntityFrameworkCore;
using SharedKernel.ExceptionHandler;
using SharedKernel.LoggerServices;
using Supplier.Domain.Dto;
using Supplier.Infrastructure.Contracts.IRepository;

namespace Supplier.Application.Features.Queries
{
    public class GetQuestionsAnswersForSupplierQueryHandler
        : IRequestHandler<GetQuestionsAnswersForSupplierQuery,
            GetQuestionsAnswersForSupplierDto>
    {
        private readonly IRepositoryWrapper _repository;
        private readonly ILoggerManager _logger;

        public GetQuestionsAnswersForSupplierQueryHandler(
            IRepositoryWrapper repository,
            ILoggerManager logger)
        {
            _repository = repository;
            _logger = logger;
        }

        public async Task<GetQuestionsAnswersForSupplierDto> Handle(
            GetQuestionsAnswersForSupplierQuery request,
            CancellationToken cancellationToken)
        {
            _logger.LogInfo($"Fetching verification answers. RequestId : {request.SupplierVerificationRequestId}");

            var answers = await _repository.SupplierVerificationAnswer
                .FindByCondition(x =>
                    x.SupplierVerificationRequestId == request.SupplierVerificationRequestId &&
                    x.IsActive)
                .Select(x => new QuestionAnswerDto
                {
                    VerificationTemplateQuestionId = x.VerificationTemplateQuestionId,
                    TemplateId = x.TemplateId,
                    Answer = x.Answer,
                    AssetId = x.AssetId,
                    VerificationTemplateQuestionOptionId = x.VerificationTemplateQuestionOptionId
                })
                .ToListAsync(cancellationToken);

            if (!answers.Any())
            {
                throw new NotFoundCustomException(
                    "Verification answers not found.",
                    "No answers available for this verification request.");
            }

            return new GetQuestionsAnswersForSupplierDto
            {
                SupplierVerificationRequestId = request.SupplierVerificationRequestId,
                Questions = answers
            };
        }
    }
}