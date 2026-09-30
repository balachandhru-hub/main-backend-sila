using MediatR;
using Microsoft.EntityFrameworkCore;
using Supplier.Application.Features.Queries.SupplierAnswers;
using Supplier.Domain.Dto;
using Supplier.Infrastructure.Contracts.IRepository;
using SharedKernel.Dto;
using SharedKernel.LoggerServices;
using SharedKernel.ExceptionHandler;

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
            _logger.LogInfo($"Fetching Supplier RFQ(s) for BuyerRFQId : {request.BuyerRFQId}");

            var supplierRFQs = await _repository.SupplierRFQ
                .FindByCondition(x =>
                    x.BuyerRFQId == request.BuyerRFQId &&
                    x.IsActive)
                .ToListAsync(cancellationToken);

            if (!supplierRFQs.Any())
            {
                _logger.LogError($"No Supplier RFQ found for BuyerRFQId: {request.BuyerRFQId}");

                throw new NotFoundCustomException(
                    "Supplier RFQ not found.",
                    $"Supplier RFQ with BuyerRFQId {request.BuyerRFQId} does not exist.");
            }

            var supplierRFQIds = supplierRFQs.Select(x => x.Id).ToList();


            var allAnswers = await _repository.RFQQuestionAnswer
                .FindByCondition(x =>
                    supplierRFQIds.Contains(x.SupplierRFQId) &&
                    x.IsActive)
                .ToListAsync(cancellationToken);

            if (!allAnswers.Any())
            {
                _logger.LogInfo($"No answers found for BuyerRFQId: {request.BuyerRFQId}");
                throw new NotFoundCustomException(
                    "Supplier RFQ answers not found.",
                    $"No answers found for Supplier RFQs with BuyerRFQId {request.BuyerRFQId}.");
            }

            var response = new SupplierRFQAnswerResponseDto();

          
            var answersBySupplier = allAnswers.GroupBy(x => x.SupplierId);

            foreach (var supplierGroup in answersBySupplier)
            {
                var supplierId = supplierGroup.Key;
                var supplierRFQId = supplierGroup.First().SupplierRFQId;

                _logger.LogInfo($"Building answer group for SupplierId: {supplierId}");

                var supplierProfile = await _repository.SupplierBusinessProfile
                    .FindByCondition(x =>
                        x.Id == supplierId &&
                        x.IsActive)
                    .FirstOrDefaultAsync(cancellationToken);

                var group = new SupplierAnswerGroupDto
                {
                    SupplierRFQId = supplierRFQId,
                    SupplierId = supplierId,
                    SupplierName = supplierProfile?.OrganizationName
                };

                foreach (var answer in supplierGroup)
                {
                    _logger.LogInfo($"Processing answer for RFQQuestionId: {answer.RFQQuestionId} from SupplierId: {supplierId}");
                    var dto = new SupplierQuestionAnswerDto
                    {
                        RFQQuestionId = answer.RFQQuestionId,
                        Answer = answer.Answer,
                        QuestionOptionId = answer.QuestionOptionId
                    };

                  
                    if (!answer.QuestionOptionId.HasValue)
                    {
                        _logger.LogInfo($"Fetching multiple option answers for RFQQuestionId: {answer.RFQQuestionId} from SupplierId: {supplierId}");
                        dto.QuestionOptionIds = await _repository.RFQQuestionAnswerOption
                            .FindByCondition(x =>
                                x.SupplierRFQQuestionAnswerId == answer.Id &&
                                x.IsActive)
                            .Select(x => x.RFQQuestionOptionId)
                            .Distinct()
                            .ToListAsync(cancellationToken);
                    }
                    else
                    {
                        _logger.LogInfo($"Single option answer found for RFQQuestionId: {answer.RFQQuestionId} from SupplierId: {supplierId}");
                        dto.QuestionOptionIds = new List<Guid>();
                    }

                    if (answer.AssetId.HasValue)
                    {
                        _logger.LogInfo($"Fetching attachment for RFQQuestionId: {answer.RFQQuestionId} from SupplierId: {supplierId}");
                        var asset = await _repository.Asset
                            .FindByCondition(x =>
                                x.Id == answer.AssetId.Value &&
                                x.IsActive)
                            .FirstOrDefaultAsync(cancellationToken);

                        if (asset != null)
                        {
                            _logger.LogInfo($"Attachment found for RFQQuestionId: {answer.RFQQuestionId} from SupplierId: {supplierId}, AssetId: {asset.Id}");
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

                    group.Answers.Add(dto);
                }

                response.Suppliers.Add(group);
            }
            _logger.LogInfo($"Completed fetching answers for BuyerRFQId: {request.BuyerRFQId}. Total suppliers with answers: {response.Suppliers.Count}");
            return response;
        }
    }
}