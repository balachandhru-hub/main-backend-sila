using MediatR;
using Supplier.Application.Features.Queries.SupplierAnswers;
using Supplier.Domain.Dto;
using Supplier.Infrastructure.Contracts.IRepository;
using SharedKernel.Dto;
using SharedKernel.LoggerServices;

namespace Supplier.Application.Features.Queries.SupplierAnswers
{
    public class GetSupplierRFQAnswerQueryHandler
        : IRequestHandler<GetSupplierRFQAnswerQuery, SupplierRFQAnswerResponseDto>
    {
        private readonly IRepositoryWrapper _repository;
        private readonly ILoggerManager _logger;

        public GetSupplierRFQAnswerQueryHandler(
            IRepositoryWrapper repository,
            ILoggerManager logger)
        {
            _repository = repository;
            _logger = logger;
        }

        public async Task<SupplierRFQAnswerResponseDto> Handle(
            GetSupplierRFQAnswerQuery request,
            CancellationToken cancellationToken)
        {
            _logger.LogInfo($"Fetching supplier answers for BuyerRFQId : {request.BuyerRFQId}");

            var supplierRFQ = _repository.SupplierRFQ
            .FindFirstByCondition(x =>
                x.BuyerRFQId == request.BuyerRFQId &&
                x.IsActive);

            if (supplierRFQ == null)
            {
                _logger.LogError($"Supplier RFQ not found for BuyerRFQId: {request.BuyerRFQId}");
                throw new Exception("Supplier RFQ not found.");
            }
            _logger.LogInfo($"Fetching answers for SupplierRFQId: {supplierRFQ.Id}");
            var answers = _repository.RFQQuestionAnswer
                .FindByCondition(x =>
                    x.SupplierRFQId == supplierRFQ.Id &&
                    x.IsActive)
                .ToList();

            var response = new SupplierRFQAnswerResponseDto
            {
                
                SupplierRFQId = supplierRFQ.Id
            };

            foreach (var answer in answers)
            {
                _logger.LogInfo($"Processing answer for RFQQuestionId: {answer.RFQQuestionId}");
                var dto = new SupplierQuestionAnswerDto
                {
                    RFQQuestionId = answer.RFQQuestionId,
                    Answer = answer.Answer,
                    QuestionOptionId = answer.QuestionOptionId
                };

                
                dto.QuestionOptionIds = _repository.RFQQuestionAnswerOption
                    .FindByCondition(x =>
                        x.SupplierRFQQuestionAnswerId == answer.Id &&
                        x.IsActive)
                    .Select(x => x.RFQQuestionOptionId)
                    .ToList();

                
                if (answer.AssetId.HasValue)
                {
                    _logger.LogInfo($"Fetching asset for AssetId: {answer.AssetId.Value}");
                    var asset = _repository.Asset
                        .FindFirstByCondition(x =>
                            x.Id == answer.AssetId.Value &&
                            x.IsActive);

                    if (asset != null)
                    {
                        _logger.LogInfo($"Asset found for AssetId: {answer.AssetId.Value}, preparing AssetDto");
                        dto.Attachment = new AssetDto
                        {
                            Id = asset.Id,
                            AssetType = asset.AssetType?.ToString(),
                            AssetName = asset.AssetName,
                            FileType = asset.FileType.ToString(),
                            FileName = asset.FileName
                        };
                    }
                }

                response.Answers.Add(dto);
            }

            return await Task.FromResult(response);
        }
    }
}