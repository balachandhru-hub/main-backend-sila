
using Buyer.Domain.Dto;
using Buyer.Infrastructure.Contracts.IRepository;
using MediatR;
using SharedKernel.ExceptionHandler;
using SharedKernel.LoggerServices;
using Buyer.Application.Features.Queries.GetRFQQuestions;
using SharedKernel.Dto;

namespace Buyer.Application.Features.Queries.GetQuestionOptions
{
    public class GetRFQQuestionsQueryHandler
        : IRequestHandler<GetRFQQuestionsQuery, List<RFQQuestionResponseDto>>
    {
        private readonly IRepositoryWrapper _repository;
        private readonly ILoggerManager _logger;

        public GetRFQQuestionsQueryHandler(IRepositoryWrapper repository, ILoggerManager logger)
        {
            _repository = repository;
            _logger = logger;
        }

        public async Task<List<RFQQuestionResponseDto>> Handle(
            GetRFQQuestionsQuery request,
            CancellationToken cancellationToken)
        {
            _logger.LogInfo($"Fetching RFQ Questions for RFQ Id: {request.RFQId}");
            var questions = _repository.RFQQuestion
                .FindByCondition(x => x.RFQId == request.RFQId && x.IsActive)
                .OrderBy(x => x.DisplayOrder)
                .ToList();
            _logger.LogInfo($"Found {questions.Count} questions for RFQ Id: {request.RFQId}");
            var questionIds = questions
                .Select(x => x.Id)
                .ToList();
            _logger.LogInfo($"Fetching RFQ Question Options for Question Ids: {string.Join(", ", questionIds)}");
            var options = _repository.RFQQuestionOption
                .FindByCondition(x => questionIds.Contains(x.RFQQuestionId) && x.IsActive)
                .ToList();
            var attachmentMappings = _repository.RFQQuestionAttachmentMapping
    .FindByCondition(x => questionIds.Contains(x.RFQQuestionId) && x.IsActive)
    .ToList();

            var assetIds = attachmentMappings
                .Select(x => x.AssetId)
                .Distinct()
                .ToList();
            _logger.LogInfo($"Fetching Assets for Asset Ids: {string.Join(", ", assetIds)}");
            var assets = _repository.Asset
                .FindByCondition(x => assetIds.Contains(x.Id) && x.IsActive)
                .ToList();

            return questions.Select(q => new RFQQuestionResponseDto
            {

                QuestionId = q.Id,
                Question = q.Question,
                QuestionType = q.QuestionType,
                IsRequired = q.IsRequired,
                DisplayOrder = q.DisplayOrder,

                Options = options
                    .Where(o => o.RFQQuestionId == q.Id)
                    .OrderBy(o => o.DisplayOrder)
                    .Select(o => new RFQQuestionOptionDto
                    {
                        OptionId = o.Id,
                        OptionText = o.OptionText,
                        DisplayOrder = o.DisplayOrder
                    })
                    .ToList(),
                Attachments = attachmentMappings
        .Where(a => a.RFQQuestionId == q.Id)
        .Join(
            assets,
            mapping => mapping.AssetId,
            asset => asset.Id,
            (mapping, asset) => new AssetDto
            {
                Id = asset.Id,
                AssetType = asset.AssetType?.ToString(),
                AssetName = asset.AssetName,
                FileType = asset.FileType.ToString(),
                FileName = asset.FileName
            })
        .ToList()

            }).ToList();
        }
    }
}